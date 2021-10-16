//------------------------------------------------------------------------------
// <copyright file="SqlModelUpdaterException.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using Microsoft.Data.Tools.Schema.SchemaModel;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    [Serializable]
    internal sealed class SqlModelUpdaterException : DataSchemaModelException
    {
        public SqlModelUpdaterException()
            : this(null, null)
        {
        }

        public SqlModelUpdaterException(string message)
            : this(message, null)
        {
        }

        public SqlModelUpdaterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private SqlModelUpdaterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}