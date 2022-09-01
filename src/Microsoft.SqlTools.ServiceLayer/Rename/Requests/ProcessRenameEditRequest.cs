using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Rename.Requests
{
    public class ProcessRenameEditRequestParams : GeneralRequestDetails
    {
        public RenameTableInfo TableInfo { get; set; }
        public RenameTableChangeInfo ChangeInfo { get; set; }
    }
    public class ProcessRenameEditRequest
    {
        public static readonly RequestType<ProcessRenameEditRequestParams, bool> Type = RequestType<ProcessRenameEditRequestParams, bool>.Create("rename/processedit");
    }
}