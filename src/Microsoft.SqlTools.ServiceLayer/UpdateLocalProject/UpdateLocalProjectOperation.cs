//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.UpdateLocalProject.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.UpdateLocalProject
{
    /// <summary>
    /// Class to represent an in-progress update local project operation
    /// </summary>
    class UpdateLocalProjectOperation
    {
        private LocalProjectUpdater Updater { get; }

        public UpdateLocalProjectOperation(UpdateLocalProjectParams parameters, ConnectionInfo connInfo, string connString = null)
        {
            Validate.IsNotNull("parameters", parameters);

            Updater = new LocalProjectUpdater(connString ?? ConnectionService.BuildConnectionString(connInfo.ConnectionDetails),
                                              parameters.ProjectPath, parameters.TargetScripts, parameters.Version, parameters.FolderStructure);
        }

        public UpdateLocalProjectResult UpdateLocalProject()
        {
            Dictionary<string, string[]> result;

            try
            {
                result = Updater.UpdateLocalProject();

                return new UpdateLocalProjectResult()
                {
                    AddedFiles = result["addedFiles"],
                    DeletedFiles = result["deletedFiles"],
                    ChangedFiles = result["changedFiles"],
                    Success = true,
                    ErrorMessage = ""
                };
            }
            catch (Exception e)
            {
                return new UpdateLocalProjectResult()
                {
                    AddedFiles = Array.Empty<string>(),
                    DeletedFiles = Array.Empty<string>(),
                    ChangedFiles = Array.Empty<string>(),
                    Success = false,
                    ErrorMessage = e.Message
                };
            }
        }

        public void UpdateTargetScripts(string[] targetScripts)
        {
            Updater.UpdateTargetScripts(targetScripts);
        }
    }
}
