//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    /// <summary>
    /// This is a util class which will store starting and ending cordinates of queryText under parent object
    /// </summary>
    public class SelectedTextCoordinates
    {
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }
}
