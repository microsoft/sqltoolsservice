//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts
{

    [DebuggerDisplay("NewText = {NewText}, Range = {Range.Start.Line}:{Range.Start.Character} - {Range.End.Line}:{Range.End.Character}")]
    public class TextEdit
    {
        public Range Range { get; set; }

        public string NewText { get; set; }
    }

}
