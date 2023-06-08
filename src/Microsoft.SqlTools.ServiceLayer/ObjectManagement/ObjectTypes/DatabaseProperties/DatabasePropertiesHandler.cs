//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using System;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabasePropertiesHandler : ObjectTypeHandler<DatabasePropertiesInfo, DatabasePropertiesViewContext>
    {
        private const string serverNotExistsError = "Server was not created for data container";
        public DatabasePropertiesHandler(ConnectionService connectionService) : base(connectionService){ }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.DatabaseProperties;
        }

        /// <summary>
        /// Initilaize the database properties object view with the real values from SMO
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams parameters)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            if (dataContainer.Server == null)
            {
                throw new InvalidOperationException(serverNotExistsError);
            }

            // Get the database properties from SMO using the objectUrn
            var smoDatabaseProperties = dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as Database;

            // Get the General tab database properties
            DatabasePropertiesInfo databasePropertiesInfo = new DatabasePropertiesInfo()
            {
                Name = smoDatabaseProperties.Name,
                CollationName = smoDatabaseProperties.Collation,
                DateCreated = smoDatabaseProperties.CreateDate.ToString(),
                LastDatabaseBackup = smoDatabaseProperties.LastBackupDate == DateTime.MinValue ? "None" : smoDatabaseProperties.LastBackupDate.ToString(),
                LastDatabaseLogBackup = smoDatabaseProperties.LastLogBackupDate == DateTime.MinValue ? "None" : smoDatabaseProperties.LastLogBackupDate.ToString(),
                MemoryAllocatedToMemoryOptimizedObjectsInMb = ConvertKbtoMbString(smoDatabaseProperties.MemoryAllocatedToMemoryOptimizedObjectsInKB),
                MemoryUsedByMemoryOptimizedObjectsInMb = ConvertKbtoMbString(smoDatabaseProperties.MemoryUsedByMemoryOptimizedObjectsInKB),
                NumberOfUsers = smoDatabaseProperties.Users.Count.ToString(),
                Owner = smoDatabaseProperties.Owner.ToString(),
                SizeInMb= smoDatabaseProperties.Size.ToString("0.00") + " MB",
                SpaceAvailableInMb = ConvertKbtoMbString(smoDatabaseProperties.SpaceAvailable),
                Status = smoDatabaseProperties.Status.ToString(),
            };

            return Task.FromResult(new InitializeViewResult()
            {
                ViewInfo = new DatabasePropertiesViewInfo(){ ObjectInfo = databasePropertiesInfo },
                Context = new DatabasePropertiesViewContext(parameters)
            });
        }

        public override Task Save(DatabasePropertiesViewContext context, DatabasePropertiesInfo obj)
        {
            throw new NotImplementedException();
        }

        public override Task<string> Script(DatabasePropertiesViewContext context, DatabasePropertiesInfo obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts value in KBs to MBs with two decimal places
        /// </summary>
        /// <param name="valueInKb"></param>
        /// <returns>Returns as String</returns>
        private string ConvertKbtoMbString(double valueInKb)
        {
            return  (Math.Round(valueInKb / 1000, 2)).ToString("0.00") + " MB";
        }
    }
}
