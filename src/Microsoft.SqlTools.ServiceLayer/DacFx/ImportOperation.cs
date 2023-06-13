//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress import operation
    /// </summary>
    class ImportOperation : DacFxOperation
    {
        public ImportParams Parameters { get; }

        public ImportOperation(ImportParams parameters, ConnectionInfo connInfo) : base(connInfo)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public override void Execute()
        {
            BacPackage bacpac = BacPackage.Load(this.Parameters.PackageFilePath);
            this.DacServices.ImportBacpac(bacpac, this.Parameters.DatabaseName, this.CancellationToken);
        }
    }
}
