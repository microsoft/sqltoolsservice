//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Parameters to pass into IdentificationRequest
    /// </summary>
    public class IdentificationParams
    {
        /// <summary>
        /// URI identifying the file that Identification is for   
        /// </summary>
        public string DocumentUri { get; set; }     

        /// <summary>
        /// Position of the Identification request
        /// </summary>
        public Position Position { get; set;  }          

        /// <summary>
        /// Position of the Identification request
        /// </summary>
        public string word { get; set;  }         
    }

    public class IdentificationRequest
    {
        public static readonly
            // Takes in a position and document uri, returns the object identification as a list of strings: [server, database, schema, objectname]
            RequestType<IdentificationParams, string> Type =
            RequestType<IdentificationParams, string>.Create("textDocument/identification");
    }
}
