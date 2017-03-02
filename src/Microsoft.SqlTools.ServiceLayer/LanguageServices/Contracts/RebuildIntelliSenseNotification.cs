//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Parameters to be sent back with a rebuild IntelliSense event
    /// </summary>
    public class RebuildIntelliSenseParams
    {
        /// <summary>
        /// URI identifying the file that should have its IntelliSense cache rebuilt    
        /// </summary>
        public string OwnerUri { get; set;  }        
    }

    /// <summary>
    /// RebuildIntelliSenseNotification notification mapping entry 
    /// </summary>
    public class RebuildIntelliSenseNotification
    {
        public static readonly 
            EventType<RebuildIntelliSenseParams> Type =
            EventType<RebuildIntelliSenseParams>.Create("textDocument/rebuildIntelliSense");
    }
}
