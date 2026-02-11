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
using Opc.Ua.Configuration;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thales.OPCUAClient.UAClient.Config;
using Thales.OPCUAClient.UAClient.Internal;
using Thales.OPCUAClient.UAClient.Model;

namespace Thales.OPCUAClient.UAClient
{
    /// <inheritdoc />
    public class Client : IClient
    {
        private static readonly ILogger _logger = Locator.Current.GetService<ILogger>();

        private readonly ConcurrentQueue<SendValue> concurrentQueue = new ConcurrentQueue<SendValue>();
        private readonly Action<IList, IList> validateResponseAction = ClientBase.ValidateResponse;

        private readonly Configuration configuration;

        /// <inheritdoc />
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Client(Configuration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Get all possible values from the queue for sending to OPC UA server
        /// </summary>
        /// <returns></returns>
        private List<SendValue> GetValues()
        {
            var values = new List<SendValue>();

            while (concurrentQueue.TryDequeue(out SendValue s))
            {
                values.Add(s);
            }

            return values;
        }

        /// <inheritdoc />
        public void AddValue(SendValue val)
        {
            concurrentQueue.Enqueue(val);
        }

        private WriteValueCollection GetWriteValueCollection()
        {
            List<SendValue> list = GetValues();

            if (!list.Any())
            {
                return null;
            }

            WriteValueCollection nodesToWrite = new WriteValueCollection();

            foreach (SendValue item in list)
            {
                if (item is ExtensionObjectSendValue)
                {
                    WriteValue structWriteVal = new WriteValue();
                    structWriteVal.NodeId = new NodeId(item.NodeId);
                    structWriteVal.Value = new DataValue((ExtensionObject)item.Value);
                    structWriteVal.AttributeId = Attributes.Value;
                    nodesToWrite.Add(structWriteVal);
                    continue;
                }
                var writeValue = new WriteValue();
                writeValue.NodeId = new NodeId(item.NodeId);
                writeValue.AttributeId = Attributes.Value;
                writeValue.Value = new DataValue();
                writeValue.Value.Value = item.Value;
                nodesToWrite.Add(writeValue);
            }

            return nodesToWrite;
        }

        /// <summary>
        /// Write a list of nodes to the Server.
        /// </summary>
        private void WriteNodes(ISession session, WriteValueCollection nodesToWrite)
        {
            if (session == null || !session.Connected)
            {
                _logger.Write("Session not connected!", LogLevel.Warn);
                return;
            }

            if (nodesToWrite?.Any() != true)
                return;

            try
            {
                StatusCodeCollection results = null;
                DiagnosticInfoCollection diagnosticInfos;

                session.Write(null, nodesToWrite, out results, out diagnosticInfos);
                validateResponseAction(results, nodesToWrite);

                bool allOk = true;
                List<int> errorIndexes = new List<int>();
                for (int i = 0; i < results.Count; i++)
                {
                    if (StatusCode.IsBad(results[i]))
                    {
                        allOk = false;
                        errorIndexes.Add(i); 
                    }
                }
                if (allOk)
                {
                    _logger.Write("Successfully wrote all nodes.", LogLevel.Debug);
                }
                else
                {
                    string errors = string.Join(", ", errorIndexes.Select(idx =>
                        $"Index {idx}, NodeId: {nodesToWrite[idx].NodeId}, Status: {results[idx]}"));
                    _logger.Write($"Write error on the following nodes: {errors}", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                _logger.Write($"Write Nodes Error. {ex.Message}", LogLevel.Error);
            }
        }

        /// <inheritdoc />
        public async Task StartAsync()
        {
            try
            {
                Uri serverUrl = new Uri(configuration.Url);
                var applicationName = "ConsoleReferenceClient";
                var configSectionName = "Quickstarts.ReferenceClient";

                // command line options
                bool autoAccept = false;
                bool noSecurity = false;
                string password = null;
                bool leakChannels = false;

                // Define the UA Client application
                ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
                CertificatePasswordProvider PasswordProvider = new CertificatePasswordProvider(password);
                ApplicationInstance application = new ApplicationInstance
                {
                    ApplicationName = applicationName,
                    ApplicationType = ApplicationType.Client,
                    ConfigSectionName = configSectionName,
                    CertificatePasswordProvider = PasswordProvider
                };

                // load the application configuration.
                ApplicationConfiguration config = await application.LoadApplicationConfiguration(filePath: configuration.FilePath, silent: false).ConfigureAwait(false);

                // keep only Basic256Sha256
                config.SecurityConfiguration.SupportedSecurityPolicies.RemoveAll(q => q != SecurityPolicies.Basic256Sha256);

                // wait for timeout or Ctrl-C
                var quitCTS = new CancellationTokenSource();
                var quitEvent = CtrlCHandler(quitCTS);

                // connect to a server until application stops
                bool quit = false;
                DateTime start = DateTime.UtcNow;
                int waitTime = int.MaxValue;
                do
                {
                    // create the UA Client object and connect to configured server.
                    using (var uaClient = new Internal.UAClient(application.ApplicationConfiguration, null)
                    {
                        AutoAccept = autoAccept,
                        SessionLifeTime = configuration.SessionLifeTime,
                    })
                    {
                        // set user identity of type username/pw
                        if (!string.IsNullOrEmpty(configuration.Username))
                        {
                            uaClient.UserIdentity = new UserIdentity(configuration.Username, configuration.Userpassword ?? string.Empty);
                        }

                        bool connected = await uaClient.ConnectAsync(serverUrl.ToString(), !noSecurity, quitCTS.Token).ConfigureAwait(false);

                        IsRunning = connected;

                        if (connected)
                        {
                            _logger.Write("Connected!", LogLevel.Info);

                            // enable subscription transfer
                            uaClient.ReconnectPeriod = configuration.ReconnectPeriod;
                            uaClient.ReconnectPeriodExponentialBackoff = configuration.ReconnectPeriodExponentialBackoff;
                            uaClient.Session.MinPublishRequestCount = configuration.MinPublishRequestCount;
                            uaClient.Session.TransferSubscriptionsOnReconnect = true;

                            _logger.Write("Waiting...", LogLevel.Debug);

                            // Wait for some DataChange notifications from MonitoredItems
                            int waitCounters = 0;

                            while (!quit && waitCounters < configuration.RecreateConnectionTimeout)
                            {
                                quit = quitEvent.WaitOne(configuration.SendDataPeriodWaitTime);

                                WriteNodes(uaClient.Session, GetWriteValueCollection());

                                waitCounters += configuration.SendDataPeriodWaitTime;
                            }

                            _logger.Write("Client disconnected.", LogLevel.Info);

                            uaClient.Disconnect(leakChannels);
                        }
                        else
                        {
                            _logger.Write($"Could not connect to server! Retry in {configuration.ReconnectPeriodExponentialBackoff / 1000.0} seconds.", LogLevel.Warn);
                            quit = quitEvent.WaitOne(Math.Min(configuration.ReconnectPeriodExponentialBackoff, waitTime));
                        }
                    }

                } while (!quit);

                _logger.Write("Client stopped.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                _logger.Write($"Error : {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Create an event which is set if a user
        /// enters the Ctrl-C key combination.
        /// </summary>
        private static ManualResetEvent CtrlCHandler(CancellationTokenSource cts)
        {
            var quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (_, eArgs) =>
                {
                    cts.Cancel();
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch
            {
                // intentionally left blank
            }
            return quitEvent;
        }
    }
}
