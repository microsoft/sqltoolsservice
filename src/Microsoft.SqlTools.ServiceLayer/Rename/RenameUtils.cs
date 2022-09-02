using System;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;

namespace Microsoft.SqlTools.ServiceLayer.Rename
{
    public static class RenameUtils
    {
        public static void Validate(ProcessRenameEditRequestParams requestParams)
        {
            if (requestParams.TableInfo.IsNewTable == true)
            {
                throw new InvalidOperationException(SR.TableDoesNotExist);
            }
            if (requestParams.ChangeInfo.Type == ChangeType.Column)
            {
                throw new NotImplementedException(SR.FeatureNotYetImplemented);
            }
            if (String.IsNullOrEmpty(requestParams.TableInfo.Schema) || String.IsNullOrEmpty(requestParams.TableInfo.TableName) || String.IsNullOrEmpty(requestParams.TableInfo.ConnectionString) || String.IsNullOrEmpty(requestParams.TableInfo.Server) || String.IsNullOrEmpty(requestParams.TableInfo.Id))
            {
                throw new ArgumentException(SR.RenameRequestParametersNotNullOrEmpty);
            }
        }
    }
}