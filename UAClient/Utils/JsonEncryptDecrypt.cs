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
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Thales.OPCUAClient.UAClient.Utils
{
    public class JsonEncryptDecrypt
    {
        private static readonly ILogger Log = Locator.Current.GetService<ILogger>();

        public static readonly string CIPHERED = "ciphered:";

        protected JsonEncryptDecrypt()
        {
            // Empty because of a static class
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueToEncrypt"></param>
        /// <param name="entropy"></param>
        /// <returns></returns>
        public static string Encrypt(string valueToEncrypt, string entropy)
        {
            if (IsEncrypted(valueToEncrypt))
            {
                return valueToEncrypt;  // simple way
            }
            byte[] value = Encoding.UTF8.GetBytes(valueToEncrypt);
            byte[] entropyBytes = Encoding.UTF8.GetBytes(entropy ?? string.Empty);
            return string.Concat(CIPHERED, Convert.ToBase64String(ProtectedData.Protect(value, entropyBytes, DataProtectionScope.CurrentUser)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueToDecrypt"></param>
        /// <param name="entropy"></param>
        /// <returns></returns>
        public static string Decrypt(string valueToDecrypt, string entropy)
        {
            if (!IsEncrypted(valueToDecrypt))
            {
                return valueToDecrypt;  // simple way
            }

            byte[] entropyBytes = Encoding.UTF8.GetBytes(entropy ?? string.Empty);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(valueToDecrypt.Substring(CIPHERED.Length)), entropyBytes, DataProtectionScope.CurrentUser));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsEncrypted(string value) => !string.IsNullOrEmpty(value) && value.StartsWith(CIPHERED);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public static void SecureFile(string filePath)
        {
            Log.Write($"{nameof(SecureFile)} starts",LogLevel.Debug);
            if (!File.Exists(filePath))
            {
                Log.Write($"{nameof(SecureFile)} file '{filePath}' does not exist.", LogLevel.Warn);
                return;
            }

            try
            {
                string jsonFile = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonFile))
                {
                    Log.Write($"{nameof(SecureFile)} file '{filePath}' is empty.", LogLevel.Warn);
                    return;
                }

                // Look for the section
                dynamic jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonFile);
                if (jsonObj == null)
                {
                    Log.Write($"{nameof(SecureFile)} '{filePath}' has not been converted to a json object (null).", LogLevel.Warn);
                    return;
                }

                // Encrypt the sensitive data if they are not.
                bool userName = SecureCredentials(jsonObj, "Username", "Entropy");
                bool userPassword = SecureCredentials(jsonObj, "Userpassword", "Entropy");
                if (userName || userPassword)
                {
                    // Save
                    Log.Write($"{nameof(SecureFile)} Save ciphered information in file '{filePath}'.", LogLevel.Info);
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(jsonObj, Formatting.Indented));
                }
                else
                {
                    Log.Write($"{nameof(SecureFile)} No ciphered information to save in '{filePath}'.", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Log.Write($"{nameof(SecureFile)} Error while securing file '{filePath}'. {ex.Message}",LogLevel.Error);
            }

            Log.Write($"{nameof(SecureFile)} ends", LogLevel.Debug);
        }

        private static bool SecureCredentials(dynamic jsonObj, string passwordKey, string entropyKey)
        {
            string pwd = jsonObj[passwordKey] ?? string.Empty;
            string entropy = jsonObj[entropyKey] ?? string.Empty;
            if (!string.IsNullOrEmpty(pwd) && !pwd.StartsWith(CIPHERED))
            {
                jsonObj[passwordKey] = Encrypt(pwd, entropy);
                return true;
            }

            return false;
        }
    }
}
