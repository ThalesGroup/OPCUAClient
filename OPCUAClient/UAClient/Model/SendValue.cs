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

using Opc.Ua;

namespace Thales.OPCUAClient.UAClient.Model
{
    public abstract class SendValue
    {
        public string NodeId { get; }

        public object Value { get; }

        protected SendValue(object val, string nodeId)
        {
            Value = val;
            NodeId = nodeId;
        }
    }

    public class SendValue<T> : SendValue
    {
        public SendValue(T val, string nodeId) : base(val, nodeId)
        {
        }
    }

    public class IntSendValue : SendValue<int>
    {
        public IntSendValue(int val, string nodeId) : base(val, nodeId)
        {
        }
    }

    public class BoolSendValue : SendValue<bool>
    {
        public BoolSendValue(bool val, string nodeId) : base(val, nodeId)
        {
        }
    }

    public class FloatSendValue : SendValue<float>
    {
        public FloatSendValue(float val, string nodeId) : base(val, nodeId)
        {
        }
    }

    public class StringSendValue : SendValue<string>
    {
        public StringSendValue(string val, string nodeId) : base(val, nodeId)
        {
        }
    }

    public class ExtensionObjectSendValue : SendValue<ExtensionObject>
    {
        public ExtensionObjectSendValue(ExtensionObject val, string nodeId) : base(val, nodeId)
        {
        }
    }
}
