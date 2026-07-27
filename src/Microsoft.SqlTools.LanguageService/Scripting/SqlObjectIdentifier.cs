//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.LanguageService.Scripting
{
    internal sealed class Sql4PartIdentifier : Sql3PartIdentifier
    {
        public string? ServerName { get; set; }
    }

    internal class Sql3PartIdentifier
    {
        public string ObjectName { get; set; } = string.Empty;
        public string? SchemaName { get; set; }
        public string? DatabaseName { get; set; }
    }
}