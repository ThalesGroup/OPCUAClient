// -----------------------------------------------------------------------
// Copyright (C) 2025 THALES. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//
// -----------------------------------------------------------------------
// THE SOFTWARE IS PROVIDED “AS IS” AND THALES MAKES NO REPRESENTATIONS OR 
// WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE, EITHER EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT. THALES SHALL NOT BE 
// LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE AS A RESULT OF USING, 
// MODIFYING OR DISTRIBUTING THIS SOFTWARE OR ITS DERIVATIVES TO THE
// EXTENT PERMITTED BY LAW.
//
// THIS SOFTWARE IS NOT DESIGNED OR INTENDED FOR USE OR RESALE AS ON-LINE
// CONTROL EQUIPMENT IN HAZARDOUS ENVIRONMENTS REQUIRING FAIL-SAFE
// PERFORMANCE, SUCH AS IN THE OPERATION OF NUCLEAR FACILITIES, AIRCRAFT
// NAVIGATION OR COMMUNICATION SYSTEMS, AIR TRAFFIC CONTROL, DIRECT LIFE
// SUPPORT MACHINES, OR WEAPONS SYSTEMS, IN WHICH THE FAILURE OF THE
// SOFTWARE COULD LEAD DIRECTLY TO DEATH, PERSONAL INJURY, OR SEVERE
// PHYSICAL OR ENVIRONMENTAL DAMAGE ("HIGH RISK ACTIVITIES"). THALES
// SPECIFICALLY DISCLAIMS ANY EXPRESS OR IMPLIED WARRANTY OF FITNESS AND 
// ANY LIABILITIES TO THE EXTENT PERMITTED BY LAW FOR HIGH RISK ACTIVITIES.
// -----------------------------------------------------------------------

using Splat;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Thales.OPCUAClient.UAClient.Internal
{
    /// <summary>
    /// A client interface which holds an active session.
    /// The client handler may reconnect and the Session
    /// property may be updated during operation.
    /// </summary>
    internal interface IUAClient
    {
        /// <summary>
        /// The session to use.
        /// </summary>
        ISession Session { get; }
    };

    /// <summary>
    /// OPC UA Client with examples of basic functionality.
    /// </summary>
    internal class UAClient : IUAClient, IDisposable
    {
        private static readonly ILogger _logger = Locator.Current.GetService<ILogger>();

        private readonly object m_lock = new object();
        private ReverseConnectManager m_reverseConnectManager;
        private ApplicationConfiguration m_configuration;
        private SessionReconnectHandler m_reconnectHandler;
        private ISession m_session;
        private bool m_disposed = false;

        /// <summary>
        /// Initializes a new instance of the UAClient class.
        /// </summary>
        public UAClient(ApplicationConfiguration configuration)
        {
            m_configuration = configuration;
            m_configuration.CertificateValidator.CertificateValidation += CertificateValidation;
            m_reverseConnectManager = null;
        }

        /// <summary>
        /// Initializes a new instance of the UAClient class for reverse connections.
        /// </summary>
        public UAClient(ApplicationConfiguration configuration, ReverseConnectManager reverseConnectManager)
        {
            m_configuration = configuration;
            m_configuration.CertificateValidator.CertificateValidation += CertificateValidation;
            m_reverseConnectManager = reverseConnectManager;
        }

        /// <summary>
        /// Dispose objects.
        /// </summary>
        public void Dispose()
        {
            m_disposed = true;
            Opc.Ua.Utils.SilentDispose(m_session);
            m_configuration.CertificateValidator.CertificateValidation -= CertificateValidation;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the client session.
        /// </summary>
        public ISession Session => m_session;

        /// <summary>
        /// The session keepalive interval to be used in ms.
        /// </summary>
        public int KeepAliveInterval { get; set; } = 5000;

        /// <summary>
        /// The reconnect period to be used in ms.
        /// </summary>
        public int ReconnectPeriod { get; set; } = 1000;

        /// <summary>
        /// The reconnect period exponential backoff to be used in ms.
        /// </summary>
        public int ReconnectPeriodExponentialBackoff { get; set; } = 15000;

        /// <summary>
        /// The session lifetime.
        /// </summary>
        public uint SessionLifeTime { get; set; } = 60 * 1000;

        /// <summary>
        /// The user identity to use to connect to the server.
        /// </summary>
        public IUserIdentity UserIdentity { get; set; } = new UserIdentity();

        /// <summary>
        /// Auto accept untrusted certificates.
        /// </summary>
        public bool AutoAccept { get; set; } = false;

        /// <summary>
        /// The file to use for log output.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// Do a Durable Subscription Transfer
        /// </summary>
        public async Task<bool> DurableSubscriptionTransfer(string serverUrl, bool useSecurity = true, CancellationToken ct = default)
        {
            bool success = false;
            SubscriptionCollection subscriptions = new SubscriptionCollection(m_session.Subscriptions);
            m_session = null;
            if (await ConnectAsync(serverUrl, useSecurity, ct))
            {
                if (subscriptions != null && m_session != null)
                {
                    _logger.Write("Transferring " + subscriptions.Count.ToString() +
                        " subscriptions from old session to new session...",LogLevel.Debug);
                    success = m_session.TransferSubscriptions(subscriptions, true);
                    if (success)
                    {
                        _logger.Write("Subscriptions transferred.", LogLevel.Debug);
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Creates a session with the UA server
        /// </summary>
        public async Task<bool> ConnectAsync(string serverUrl, bool useSecurity = true, CancellationToken ct = default)
        {
            if (m_disposed) throw new ObjectDisposedException(nameof(UAClient));
            if (serverUrl == null) throw new ArgumentNullException(nameof(serverUrl));

            try
            {
                if (m_session != null && m_session.Connected == true)
                {
                    _logger.Write($"Session already connected!", LogLevel.Info);
                }
                else
                {
                    ITransportWaitingConnection connection = null;
                    EndpointDescription endpointDescription = null;
                    if (m_reverseConnectManager != null)
                    {
                        _logger.Write($"Waiting for reverse connection to.... {serverUrl}", LogLevel.Debug);
                        do
                        {
                            using (var cts = new CancellationTokenSource(30_000))
                            using (var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token))
                            {
                                connection = await m_reverseConnectManager.WaitForConnection(new Uri(serverUrl), null, linkedCTS.Token).ConfigureAwait(false);
                                if (connection == null)
                                {
                                    throw new ServiceResultException(StatusCodes.BadTimeout, "Waiting for a reverse connection timed out.");
                                }
                                if (endpointDescription == null)
                                {
                                    _logger.Write($"Discover reverse connection endpoints....", LogLevel.Debug);
                                    endpointDescription = CoreClientUtils.SelectEndpoint(m_configuration, connection, useSecurity);
                                    connection = null;
                                }
                            }
                        } while (connection == null);
                    }
                    else
                    {
                        _logger.Write($"Connecting to... {serverUrl}", LogLevel.Debug);
                        endpointDescription = CoreClientUtils.SelectEndpoint(m_configuration, serverUrl, useSecurity);
                    }

                    // Get the endpoint by connecting to server's discovery endpoint.
                    // Try to find the first endopint with security.
                    EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(m_configuration);
                    ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                    var sessionFactory = TraceableSessionFactory.Instance;

                    // Create the session
                    var session = await sessionFactory.CreateAsync(
                        m_configuration,
                        connection,
                        endpoint,
                        connection == null,
                        false,
                        m_configuration.ApplicationName,
                        SessionLifeTime,
                        UserIdentity,
                        null,
                        ct
                    ).ConfigureAwait(false);

                    // Assign the created session
                    if (session != null && session.Connected)
                    {
                        m_session = session;

                        // override keep alive interval
                        m_session.KeepAliveInterval = KeepAliveInterval;

                        // support transfer
                        m_session.DeleteSubscriptionsOnClose = false;
                        m_session.TransferSubscriptionsOnReconnect = true;

                        // set up keep alive callback.
                        m_session.KeepAlive += Session_KeepAlive;

                        // prepare a reconnect handler
                        m_reconnectHandler = new SessionReconnectHandler(true, ReconnectPeriodExponentialBackoff);
                    }

                    // Session created successfully.
                    _logger.Write($"New Session Created with SessionName = {m_session.SessionName}", LogLevel.Debug);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log Error
                _logger.Write($"Create Session Error.  {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Disconnects the session.
        /// </summary>
        /// <param name="leaveChannelOpen">Leaves the channel open.</param>
        public void Disconnect(bool leaveChannelOpen = false)
        {
            try
            {
                if (m_session != null)
                {
                    _logger.Write("Disconnecting...", LogLevel.Info);

                    lock (m_lock)
                    {
                        m_session.KeepAlive -= Session_KeepAlive;
                        m_reconnectHandler?.Dispose();
                        m_reconnectHandler = null;
                    }

                    m_session.Close(!leaveChannelOpen);
                    if (leaveChannelOpen)
                    {
                        // detach the channel, so it doesn't get closed when the session is disposed.
                        m_session.DetachChannel();
                    }
                    m_session.Dispose();
                    m_session = null;

                    // Log Session Disconnected event
                    _logger.Write("Session Disconnected.", LogLevel.Info);
                }
                else
                {
                    _logger.Write("Session not created!", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                // Log Error
                _logger.Write($"Disconnect Error : {ex.Message}", LogLevel.Error);
            }
        }
        /// <summary>
        /// Handles a keep alive event from a session and triggers a reconnect if necessary.
        /// </summary>
        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            try
            {
                // check for events from discarded sessions.
                if (m_session == null || !m_session.Equals(session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status))
                {
                    if (ReconnectPeriod <= 0)
                    {
                        _logger.Write($"KeepAlive status {e.Status}, but reconnect is disabled.", LogLevel.Warn);
                        return;
                    }

                    var state = m_reconnectHandler.BeginReconnect(m_session, m_reverseConnectManager, ReconnectPeriod, Client_ReconnectComplete);
                    if (state == SessionReconnectHandler.ReconnectState.Triggered)
                    {
                        _logger.Write($"KeepAlive status {e.Status}, reconnect status {state}, reconnect period {ReconnectPeriod}ms.", LogLevel.Info);
                    }
                    else
                    {
                        _logger.Write($"KeepAlive status {e.Status}, reconnect status {state}.", LogLevel.Info);
                    }

                    // cancel sending a new keep alive request, because reconnect is triggered.
                    e.CancelKeepAlive = true;

                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Write($"Error in OnKeepAlive. {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Called when the reconnect attempt was successful.
        /// </summary>
        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!ReferenceEquals(sender, m_reconnectHandler))
            {
                return;
            }

            lock (m_lock)
            {
                // if session recovered, Session property is null
                if (m_reconnectHandler.Session != null)
                {
                    // ensure only a new instance is disposed
                    // after reactivate, the same session instance may be returned
                    if (!ReferenceEquals(m_session, m_reconnectHandler.Session))
                    {
                        _logger.Write($"--- RECONNECTED TO NEW SESSION --- {m_reconnectHandler.Session.SessionId}", LogLevel.Debug);
                        var session = m_session;
                        m_session = m_reconnectHandler.Session;
                        Opc.Ua.Utils.SilentDispose(session);
                    }
                    else
                    {
                        _logger.Write($"--- REACTIVATED SESSION --- {m_reconnectHandler.Session.SessionId}", LogLevel.Debug);
                    }
                }
                else
                {
                    _logger.Write("--- RECONNECT KeepAlive recovered ---", LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Handles the certificate validation event.
        /// This event is triggered every time an untrusted certificate is received from the server.
        /// </summary>
        protected virtual void CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            var cert = e.Certificate;
            var trustedPeerPath = m_configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath
                .Replace("%LocalApplicationData%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Replace('/', Path.DirectorySeparatorChar);
            var rejectedPath = m_configuration.SecurityConfiguration.RejectedCertificateStore.StorePath
                .Replace("%LocalApplicationData%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Replace('/', Path.DirectorySeparatorChar);

            bool certificateIsTrusted = false;

            // Vérifier si le certificat est dans le dossier trusted
            try
            {
                foreach (string file in Directory.GetFiles(trustedPeerPath, "*.der"))
                {
                    var c = new X509Certificate2(file);
                    if (c.Thumbprint == cert.Thumbprint)
                    {
                        certificateIsTrusted = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Write($"Error reading trusted certificates: {ex.Message}", LogLevel.Error);
            }

            if (certificateIsTrusted)
            {
                _logger.Write($"Certificate found in trusted store: {cert.Subject}", LogLevel.Info);
                e.Accept = true;
            }
            else
            {
                _logger.Write($"Certificate NOT trusted. Will be stored in rejected folder: {cert.Subject}", LogLevel.Warn);

                string newFilename = Path.Combine(rejectedPath, $"{SanitizeFileName(cert.Subject)}_{cert.Thumbprint}.der");
                if (!File.Exists(newFilename))
                {
                    try
                    {
                        Directory.CreateDirectory(rejectedPath);
                        File.WriteAllBytes(newFilename, cert.RawData);
                        _logger.Write($"Certificate written to rejected store: {newFilename}", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        _logger.Write($"Failed to write rejected certificate: {ex.Message}", LogLevel.Error);
                    }
                }
                e.Accept = false;
            }
        }
        private string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
