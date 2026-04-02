//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.SqlCore.TableDesigner;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner
{
    internal sealed class TableDesignerPublishOperation : ITaskOperation
    {
        private readonly TableDesignerManager tableDesignerManager;
        private readonly TableInfo tableInfo;

        public TableDesignerPublishOperation(TableDesignerManager tableDesignerManager, TableInfo tableInfo)
        {
            Validate.IsNotNull(nameof(tableDesignerManager), tableDesignerManager);
            Validate.IsNotNull(nameof(tableInfo), tableInfo);

            this.tableDesignerManager = tableDesignerManager;
            this.tableInfo = tableInfo;
        }

        public PublishTableChangesResponse PublishResponse { get; private set; }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                Action<string, int, int, string> progressCallback = null;
                if (this.SqlTask != null)
                {
                    this.SqlTask.InitializeProgress(0, 0, "Initializing");
                    progressCallback = (phase, current, total, message) =>
                    {
                        if (this.SqlTask != null)
                        {
                            if (total > 0)
                            {
                                // Real batch-level progress from deployment
                                if (this.SqlTask.ProgressGoal != total)
                                {
                                    this.SqlTask.InitializeProgress(current, total, phase);
                                }
                                else
                                {
                                    int delta = current - this.SqlTask.ProgressCurrent;
                                    this.SqlTask.IncrementProgress(delta > 0 ? delta : 0, phase);
                                }
                            }
                            else
                            {
                                // Phase-only progress (indeterminate)
                                this.SqlTask.IncrementProgress(0, phase);
                            }

                            if (message != null)
                            {
                                this.SqlTask.AddMessage(message, SqlTaskStatus.InProgress);
                            }
                        }
                    };
                }

                PublishResponse = tableDesignerManager.PublishTableChanges(tableInfo, progressCallback);

                if (this.SqlTask != null && this.SqlTask.ProgressGoal > 0)
                {
                    int remaining = this.SqlTask.ProgressGoal - this.SqlTask.ProgressCurrent;
                    if (remaining > 0)
                    {
                        this.SqlTask.IncrementProgress(remaining, "Complete");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        public void Cancel()
        {
        }
    }
}