//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    public class DatabaseLockConnectionStub : IDatabaseLockConnection
    {
       
        public bool CanTemporaryClose
        {
            get
            {
                return true;
            }
        }

        public SqlConnection Connection { get; set; }

        public bool IsConnctionOpen
        {
            get
            {
                return Connection.State == System.Data.ConnectionState.Open;
            }
        }

        public void Connect()
        {
            Connection.Open();
        }

        public void Disconnect()
        {
            Connection.Close();
        }
    }
}
