//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Intellisense
{
    public class ShowMaterializedViewsResult
    {
        public string Name;
        public string SourceTable;
        public string Query;
        public string MaterializedTo;
        public string LastRun;
        public string LastRunResult;
        public bool IsHealthy;
        public bool IsEnabled;
        public string Folder;
        public string DocString;
        public bool AutoUpdateSchema;
        public string EffectiveDateTime;
        public string LookBack;
    }
}
