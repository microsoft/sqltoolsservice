//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Intellisense
{
    public class ShowDatabasesResult
    {
        public string DatabaseName;
        public string PersistentStorage;
        public string Version;
        public bool IsCurrent;
        public string DatabaseAccessMode;
        public string PrettyName;
        public bool CurrentUserIsUnrestrictedViewer;
        public string DatabaseId;
    }
}