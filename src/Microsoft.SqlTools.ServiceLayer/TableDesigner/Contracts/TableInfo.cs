//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The information of the table being designed.
    /// </summary>
    public class TableInfo
    {
        public string Title { get; set; }

        public string Tooltip { get; set; }

        public string Server { get; set; }

        public string Database { get; set; }

        public string Schema { get; set; }

        public string Name { get; set; }

        public bool IsNewTable { get; set; }

        public string ConnectionString { get; set; }

        public string Id { get; set; }

        public string AccessToken { get; set; }

        public string TableScriptPath { get; set; }

        public string ProjectFilePath { get; set; }

        public List<string> AllScripts { get; set; }

        public string TargetVersion { get; set; }
    }
}