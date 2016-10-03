//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// The state of a binding request
    /// </summary>
    public interface IBindingContext
    {
        bool IsConnected { get; set; }

        ServerConnection ServerConnection { get; set; }

        MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        SmoMetadataProvider SmoMetadataProvider { get; set; }

        IBinder Binder { get; set; }

        ManualResetEvent BindingLocked { get; set; }

        int BindingTimeout { get; set; }

        ParseOptions ParseOptions { get; }

        ServerVersion ServerVersion { get; }

        DatabaseEngineType DatabaseEngineType {  get; }

        TransactSqlVersion TransactSqlVersion { get; }

        DatabaseCompatibilityLevel DatabaseCompatibilityLevel { get; }        
    }
}
