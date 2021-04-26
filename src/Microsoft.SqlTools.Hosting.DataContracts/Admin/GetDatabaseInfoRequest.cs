using Microsoft.SqlTools.Hosting.DataContracts.Admin.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Admin
{
    /// <summary>
    /// Get database info request mapping
    /// </summary>
    public class GetDatabaseInfoRequest
    {
        public static readonly
            RequestType<GetDatabaseInfoParams, GetDatabaseInfoResponse> Type =
                RequestType<GetDatabaseInfoParams, GetDatabaseInfoResponse>.Create("admin/getdatabaseinfo");
    }
}