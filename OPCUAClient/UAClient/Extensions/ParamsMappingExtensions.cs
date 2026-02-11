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

using System;
using System.Linq;
using Thales.OPCUAClient.UAClient.Config;
using Thales.OPCUAClient.UAClient.Enums;
using Thales.OPCUAClient.UAClient.Exceptions;
using Thales.OPCUAClient.UAClient.Model;

namespace Thales.OPCUAClient.UAClient.Extensions
{
    public static class ParamsMappingExtensions
    {
        public const int MAX_STRING_LENGTH = 16;

        public static SendValue CreateSendValue(this ParamsMappingHelper paramsMappingHelper, string valueName, string valueStatus)
        {
            ValidateInput(paramsMappingHelper, valueName, valueStatus);

            if (!paramsMappingHelper.Mapping.ContainsKey(valueName))
            {
                string avParams = string.Join(", ", paramsMappingHelper.Mapping.Select(q => q.Key));
                throw new ParamNotFoundException($"Cannot find server parameter '{valueName}' in mapping. Available parameters: [{avParams}].");
            }

            string nodeId = paramsMappingHelper.GlobalNodeId + valueName;
            ParamType type = paramsMappingHelper.Mapping[valueName];

            return ConstructSendValue(nodeId, type, valueStatus);
        }

        private static void ValidateInput(ParamsMappingHelper paramsMappingHelper, string valueName, string valueStatus)
        {
            if (paramsMappingHelper == null)
            {
                throw new ArgumentNullException(nameof(paramsMappingHelper));
            }

            if (string.IsNullOrEmpty(valueName))
            {
                throw new ArgumentNullException(nameof(valueName));
            }

            if (string.IsNullOrEmpty(valueStatus))
            {
                throw new ArgumentNullException(nameof(valueStatus));
            }
        }

        private static SendValue ConstructSendValue(string nodeId, ParamType type, string valueStatus)
        {
            switch (type)
            {
                case ParamType.Bool:
                    return ConstructBoolSendValue(nodeId, valueStatus);

                case ParamType.Int:
                    return ConstructIntSendValue(nodeId, valueStatus);

                case ParamType.String:
                    return ConstructStringSendValue(nodeId, valueStatus);

                case ParamType.Float:
                    return ConstructFloatSendValue(nodeId, valueStatus);

                default:
                    throw new ArgumentOutOfRangeException($"Server parameter type '{type}' not supported.");
            }
        }

        private static SendValue ConstructStringSendValue(string nodeId, string valueStatus)
        {
            if (valueStatus.Length <= MAX_STRING_LENGTH)
            {
                return new StringSendValue(valueStatus, nodeId);
            }

            throw new ArgumentException($"Value '{valueStatus}' exceed Maxium String Length '{MAX_STRING_LENGTH}' symbols. nodeId={nodeId}.");
        }

        private static SendValue ConstructBoolSendValue(string nodeId, string valueStatus)
        {
            if (bool.TryParse(valueStatus, out bool val))
            {
                return new BoolSendValue(val, nodeId);
            }

            throw new ArgumentException($"Cannot parse '{valueStatus}' as Boolean value. nodeId={nodeId}.");
        }

        private static SendValue ConstructIntSendValue(string nodeId, string valueStatus)
        {
            if (int.TryParse(valueStatus, out int val))
            {
                return new IntSendValue(val, nodeId);
            }

            throw new ArgumentException($"Cannot parse '{valueStatus}' as Int value. nodeId={nodeId}.");
        }

        private static SendValue ConstructFloatSendValue(string nodeId, string valueStatus)
        {
            if (float.TryParse(valueStatus, out float val))
            {
                return new FloatSendValue(val, nodeId);
            }

            throw new ArgumentException($"Cannot parse '{valueStatus}' as Float value. nodeId={nodeId}.");
        }
    }
}
