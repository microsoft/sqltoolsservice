//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension
{

    [Serializable]
    public class CompletionExtensionParams
    {
        public string AssemblyPath { get; set; }
        public string TypeName { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

}
