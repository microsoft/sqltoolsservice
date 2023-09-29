//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    public class ProcessTableDesignerEditResponse
    {
        public TableViewModel ViewModel { get; set; }

        public TableDesignerView View { get; set; }

        public bool IsValid { get; set; }

        public TableDesignerIssue[] Issues { get; set; }

        public Dictionary<string, string> Metadata { get; set; }

        public string InputValidationError { get; set; }
    }
}
