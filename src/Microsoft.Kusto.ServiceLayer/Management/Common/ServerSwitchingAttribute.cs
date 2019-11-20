//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.Kusto.ServiceLayer.Management
{
    /// <summary>
    /// Custom attribute that can be applied on particular DB commander to
    /// indicate whether the base class should switch SMO servers before 
    /// execution or not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServerSwitchingAttribute : Attribute 
    {
        private bool needToSwitch = false;

        private ServerSwitchingAttribute() {}

        public ServerSwitchingAttribute(bool needToSwitchServer) 
        {
            this.needToSwitch = needToSwitchServer;
        }

        public bool NeedToSwtichServerObject 
        {
            get 
            {
                return this.needToSwitch;
            }
        }
    }
}
