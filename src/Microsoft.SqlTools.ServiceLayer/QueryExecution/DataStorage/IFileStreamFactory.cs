//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Interface for a factory that creates filesystem readers/writers
    /// </summary>
    public interface IFileStreamFactory
    {
        string CreateFile();

        IFileStreamReader GetReader(string fileName);

        IFileStreamWriter GetWriter(string fileName, IReadOnlyList<DbColumnWrapper> columns = null);

        void DisposeFile(string fileName);

        SqlContext.QueryExecutionSettings QueryExecutionSettings { get; set; }

    }
}
