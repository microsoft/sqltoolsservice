//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.DataStorage
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

    }
}
