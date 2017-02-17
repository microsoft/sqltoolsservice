
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public interface IEditMetadataFactory
    {
        IEditTableMetadata GetObjectMetadata(DbConnection connection, DbColumnWrapper[] columns, string objectName, string objectType);
    }
}