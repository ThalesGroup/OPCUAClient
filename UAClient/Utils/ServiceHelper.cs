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
using System.IO;
using Thales.OPCUAClient.UAClient.Config;
using Thales.OPCUAClient.UAClient.Extensions;

namespace Thales.OPCUAClient.UAClient.Utils
{
    public class ServiceHelper
    {
        private static readonly ILogger Log = Locator.Current.GetService<ILogger>();

        protected ServiceHelper()
        {
            // Empty because of a static class
        }

        public static string ReadJsonConfigFile(string rootPath, string fileName)
        {
            string path = !string.IsNullOrEmpty(rootPath)
                ? Path.Combine(rootPath, fileName)
                : fileName;

            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(fileInfo.FullName);
            }

            string json = File.ReadAllText(fileInfo.FullName);

            return json;
        }

        public static void EncryptSensitiveData(string rootPath, string fileName)
        {
            Log.Write($"{nameof(EncryptSensitiveData)} starts", LogLevel.Debug);

            string path = !string.IsNullOrEmpty(rootPath)
                ? Path.Combine(rootPath, fileName)
                : fileName;

            JsonEncryptDecrypt.SecureFile(path);

            Log.Write($"{nameof(EncryptSensitiveData)} ends", LogLevel.Debug);
        }

        public static ParamsMappingHelper CreateParamsMappingHelper(string rootPath, string configFileName)
        {
            string json = ReadJsonConfigFile(rootPath, configFileName);

            var paramsMappingHelper = new ParamsMappingHelper(json);
            return paramsMappingHelper;
        }

        public static Configuration GetConfig(string rootPath, string configFileName, string opcuaConfigFileName)
        {
            string json = ReadJsonConfigFile(rootPath, configFileName);

            Configuration config = json.CreateConfiguration();

            if (JsonEncryptDecrypt.IsEncrypted(config.Username))
            {
                config.Username = JsonEncryptDecrypt.Decrypt(config.Username, config.Entropy);
            }

            if (JsonEncryptDecrypt.IsEncrypted(config.Userpassword))
            {
                config.Userpassword = JsonEncryptDecrypt.Decrypt(config.Userpassword, config.Entropy);
            }

            config.FilePath = !string.IsNullOrEmpty(rootPath)
                ? Path.Combine(rootPath, opcuaConfigFileName)
                : opcuaConfigFileName;

            return config;
        }

        public static IClient CreateClient(string rootPath, string configFileName, string opcuaConfigFileName)
        {
            Configuration config = GetConfig(rootPath, configFileName, opcuaConfigFileName);

            IClient client = new Client(config);
            return client;
        }
    }
}
