

using System;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class SmoEditMetadataFactory : IEditMetadataFactory
    {
        public IEditTableMetadata GetObjectMetadata(DbConnection connection, DbColumnWrapper[] columns, string objectName, string objectType)
        {
            // Get a connection to the database for SMO purposes
            SqlConnection sqlConn = connection as SqlConnection;
            if (sqlConn == null)
            {
                // It's not actually a SqlConnection, so let's try a reliable SQL connection
                ReliableSqlConnection reliableConn = connection as ReliableSqlConnection;
                if (reliableConn == null)
                {
                    // If we don't have connection we can use with SMO, just give up on using SMO
                    return null;
                }

                // We have a reliable connection, use the underlying connection
                sqlConn = reliableConn.GetUnderlyingConnection();
            }

            Server server = new Server(new ServerConnection(sqlConn));
            TableViewTableTypeBase result;
            switch (objectType.ToLowerInvariant())
            {
                case "table":
                    result = server.Databases[sqlConn.Database].Tables[objectName];
                    break;
                case "view":
                    result = server.Databases[sqlConn.Database].Views[objectName];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(objectType), SR.EditDataUnsupportedObjectType(objectType));
            }
            if (result == null)
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), SR.EditDataObjectMetadataNotFound);
            }

            return new EditTableMetadata(columns, result);
        }
    }
}