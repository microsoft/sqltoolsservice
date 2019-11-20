//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Completion.Extension
{

    [Serializable]
    public class CompletionExtensionParams
    {
        /// <summary>
        /// Absolute path for the assembly containing the completion extension
        /// </summary>
        public string AssemblyPath { get; set; }

        /// <summary>
        /// The type name for the completion extension
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Property bag for initializing the completion extension
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }
    }

}
