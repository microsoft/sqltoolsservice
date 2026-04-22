//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

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
