//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Parameters to be sent back with a non TSQL file event
    /// </summary>
    public class NonTSqlParams
    {
        /// <summary>
        /// URI identifying the file that was detected as non Tsql   
        /// </summary>
        public string OwnerUri { get; set;  }     

        /// <summary>
        /// Indicates whether the file was flagged as 
        /// due to containing wrong keywords, or due to 
        /// hitting the error limit
        /// </summary>
        public Boolean ContainsNonTSqlKeywords { get; set;  }  
    }

    /// <summary>
    /// NonTSqlNotification notification mapping entry 
    /// </summary>
    public class NonTSqlNotification
    {
        public static readonly 
            EventType<NonTSqlParams> Type =
            EventType<NonTSqlParams>.Create("textDocument/nonTSqlFileDetected");
    }

    /// <summary>
    /// The error threshold for when a file is considered not TSQL
    /// </summary>
    public static class NonTSqlErrorLimit
    {
        public const int SqlFileErrorLimit = 50;
    }
}
