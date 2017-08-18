//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// A factory class to create restore option info
    /// </summary>
    public class RestoreOptionFactory
    {
        private static RestoreOptionFactory instance = new RestoreOptionFactory();

        Dictionary<string, OptionBuilder> optionBuilders = new Dictionary<string, OptionBuilder>();

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static RestoreOptionFactory Instance
        {
            get
            {
                return instance;
            }
        }

        public RestorePlanDetailInfo CreateAndValidate(string optionKey, IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            RestorePlanDetailInfo restorePlanDetailInfo = CreateOptionInfo(optionKey, restoreDataObject);
            UpdateOption(optionKey, restoreDataObject, restorePlanDetailInfo);
            return restorePlanDetailInfo;
        }

        /// <summary>
        /// Create option info using the current values
        /// </summary>
        /// <param name="optionKey">Option name</param>
        /// <param name="restoreDataObject">Restore task object</param>
        /// <returns></returns>
        public RestorePlanDetailInfo CreateOptionInfo(string optionKey, IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            if(optionBuilders.ContainsKey(optionKey))
            {
                return Create(optionKey, restoreDataObject, optionBuilders[optionKey]);
            }
            else
            {
                Logger.Write(LogLevel.Warning, $"cannot find restore option builder for {optionKey}");
                return null;
            }
        }

        /// <summary>
        /// Update the option info by validating the option 
        /// </summary>
        /// <param name="optionKey"></param>
        /// <param name="restoreDataObject"></param>
        /// <param name="optionInfo"></param>
        public void UpdateOption(string optionKey, IRestoreDatabaseTaskDataObject restoreDataObject, RestorePlanDetailInfo optionInfo)
        {
            if (optionBuilders.ContainsKey(optionKey))
            {
                var builder = optionBuilders[optionKey];
                var currentValue = builder.CurrentValueFunction(restoreDataObject);
                var defaultValue = builder.DefaultValueFunction(restoreDataObject);
                var validateResult = builder.ValidateFunction(restoreDataObject, currentValue, defaultValue);
                optionInfo.IsReadOnly = validateResult.IsReadOnly;
            }
            else
            {
                Logger.Write(LogLevel.Warning, $"cannot find restore option builder for {optionKey}");
            }
        }

        /// <summary>
        /// Set the option value in restore task object using the values in the restore request
        /// </summary>
        /// <param name="optionKey"></param>
        /// <param name="restoreDataObject"></param>
        public void SetAndValidate(string optionKey, IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            this.SetValue(optionKey, restoreDataObject);
            this.ValidateOption(optionKey, restoreDataObject);
        }

        /// <summary>
        /// Set the option value in restore task object using the values in the restore request
        /// </summary>
        /// <param name="optionKey"></param>
        /// <param name="restoreDataObject"></param>
        public void SetValue(string optionKey, IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            if (restoreDataObject != null)
            {
                if (optionBuilders.ContainsKey(optionKey))
                {
                    var builder = optionBuilders[optionKey];
                    if (restoreDataObject.RestoreParams != null && restoreDataObject.RestoreParams.Options.ContainsKey(optionKey))
                    {
                        try
                        {
                            var value = restoreDataObject.RestoreParams.GetOptionValue<object>(optionKey);
                            builder.SetValueFunction(restoreDataObject, value);
                        }
                        catch (Exception ex)
                        {
                            var defaultValue = builder.DefaultValueFunction(restoreDataObject);
                            builder.SetValueFunction(restoreDataObject, defaultValue);
                            Logger.Write(LogLevel.Warning, $"Failed tp set restore option {optionKey} error:{ex.Message}");

                        }
                    }
                    else
                    {
                        try
                        {
                            var defaultValue = builder.DefaultValueFunction(restoreDataObject);
                            builder.SetValueFunction(restoreDataObject, defaultValue);
                        }
                        catch (Exception)
                        {
                            Logger.Write(LogLevel.Warning, $"Failed to set restore option  {optionKey} to default value");
                        }
                    }
                }
                else
                {
                    Logger.Write(LogLevel.Warning, $"cannot find restore option builder for {optionKey}");
                }
            }
        }

        /// <summary>
        /// Validates the options, if option is not set correctly, set to default and return the error
        /// </summary>
        /// <param name="optionKey"></param>
        /// <param name="restoreDataObject"></param>
        /// <returns></returns>
        public string ValidateOption(string optionKey, IRestoreDatabaseTaskDataObject restoreDataObject)
        {
            string errorMessage = string.Empty;
            if (optionBuilders.ContainsKey(optionKey))
            {
                var builder = optionBuilders[optionKey];
                var currentValue = builder.CurrentValueFunction(restoreDataObject);
                var defaultValue = builder.DefaultValueFunction(restoreDataObject);
                OptionValidationResult result = optionBuilders[optionKey].ValidateFunction(restoreDataObject, currentValue, defaultValue);
                if (result.IsReadOnly)
                {
                    if(!ValueEqualsDefault(currentValue, defaultValue))
                    {
                        builder.SetValueFunction(restoreDataObject, defaultValue);
                        errorMessage = $"{optionKey} is ready only and cannot be modified";
                    }
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    errorMessage = result.ErrorMessage;
                    builder.SetValueFunction(restoreDataObject, defaultValue);
                }
            }
            else
            {
                errorMessage = $"cannot find restore option builder for {optionKey}";
                Logger.Write(LogLevel.Warning, errorMessage);
            }

            return errorMessage;
        }

        private bool ValueEqualsDefault(object currentValue, object defaultValue)
        {
            if(currentValue == null && defaultValue == null)
            {
                return true;
            }
            if(currentValue == null && defaultValue != null)
            {
                return false;
            }
            if (currentValue != null && defaultValue == null)
            {
                return false;
            }
            return currentValue.Equals(defaultValue);
        }


        private RestoreOptionFactory()
        {
            Register(RestoreOptionsHelper.RelocateDbFiles,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RelocateAllFiles;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult
                        {
                            IsReadOnly = restoreDataObject.DbFiles.Count == 0
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RelocateAllFiles = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.DataFileFolder,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultDataFileFolder;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DataFilesFolder;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult
                        {
                            IsReadOnly = !restoreDataObject.RelocateAllFiles
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.DataFilesFolder = GetValueAs<string>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.LogFileFolder,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultLogFileFolder;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.LogFilesFolder;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult
                        {
                            IsReadOnly = !restoreDataObject.RelocateAllFiles
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.LogFilesFolder = GetValueAs<string>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.ReplaceDatabase,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.ReplaceDatabase;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult();
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.ReplaceDatabase = GetValueAs<bool>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.KeepReplication,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.KeepReplication;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = restoreDataObject.RestoreOptions.RecoveryState == DatabaseRecoveryState.WithNoRecovery
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.KeepReplication = GetValueAs<bool>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.SetRestrictedUser,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.SetRestrictedUser;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.SetRestrictedUser = GetValueAs<bool>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.RecoveryState,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return DatabaseRecoveryState.WithRecovery.ToString();
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.RecoveryState.ToString();
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.RecoveryState = GetValueAs<DatabaseRecoveryState>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.StandbyFile,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultStandbyFile;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.StandByFile;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = restoreDataObject.RestoreOptions.RecoveryState != DatabaseRecoveryState.WithStandBy
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.StandByFile = GetValueAs<string>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.BackupTailLog,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.IsTailLogBackupPossible;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.BackupTailLog;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = !restoreDataObject.IsTailLogBackupPossible
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.BackupTailLog = GetValueAs<bool>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.TailLogBackupFile,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultTailLogbackupFile;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.TailLogBackupFile;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = !restoreDataObject.BackupTailLog | !restoreDataObject.IsTailLogBackupPossible
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.TailLogBackupFile = GetValueAs<string>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.TailLogWithNoRecovery,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.IsTailLogBackupWithNoRecoveryPossible;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.TailLogWithNoRecovery;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = !restoreDataObject.BackupTailLog | !restoreDataObject.IsTailLogBackupWithNoRecoveryPossible
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.TailLogWithNoRecovery = GetValueAs<bool>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.CloseExistingConnections,
                new OptionBuilder
                {
                    DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.CloseExistingConnections;
                    },
                    ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            //TODO: make the method public in SMO bool canDropExistingConnections = restoreDataObject.RestorePlan.CanDropExistingConnections(this.Data.RestorePlanner.DatabaseName);
                            IsReadOnly = false
                        };
                    },
                    SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.CloseExistingConnections = GetValueAs<bool>(value);
                        return true;
                    }
                });
            Register(RestoreOptionsHelper.SourceDatabaseName,
               new OptionBuilder
               {
                   DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.DefaultSourceDbName;
                   },
                   CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.SourceDatabaseName;
                   },
                   ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                   {
                       string errorMessage = string.Empty;
                       var sourceDbNames = restoreDataObject.SourceDbNames;
                       if (currentValue == null || (sourceDbNames != null && 
                            !sourceDbNames.Any(x => string.Compare(x, currentValue.ToString(), StringComparison.InvariantCultureIgnoreCase) == 0)))
                       {
                           errorMessage = "Source database name is not valid";
                       }
                       return new OptionValidationResult()
                       {
                          ErrorMessage = errorMessage
                       };
                   },
                   SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                   {

                       restoreDataObject.SourceDatabaseName = GetValueAs<string>(value);
                       return true;
                   }
               });
            Register(RestoreOptionsHelper.TargetDatabaseName,
               new OptionBuilder
               {
                   DefaultValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.CanChangeTargetDatabase ? restoreDataObject.DefaultSourceDbName : restoreDataObject.DefaultTargetDbName;
                   },
                   CurrentValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.TargetDatabaseName;
                   },
                   ValidateFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                   {
                       string errorMessage = string.Empty;
                       if (currentValue!= null && DatabaseUtils.IsSystemDatabaseConnection(currentValue.ToString()))
                       {
                           errorMessage = "Cannot restore to system database";
                       }
                       return new OptionValidationResult()
                       {
                           IsReadOnly = !restoreDataObject.CanChangeTargetDatabase,
                           ErrorMessage = errorMessage
                       };
                   },
                   SetValueFunction = (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                   {

                       restoreDataObject.TargetDatabaseName = GetValueAs<string>(value);
                       return true;
                   }
               });
        }

        internal T GetValueAs<T>(object value)
        {
            return GeneralRequestDetails.GetValueAs<T>(value);
        }

        private void Register(string optionKey, OptionBuilder optionBuilder)
        {
            optionBuilders.Add(optionKey, optionBuilder);
        }

        private RestorePlanDetailInfo Create(
            string optionKey, 
            IRestoreDatabaseTaskDataObject restoreDataObject,
            OptionBuilder optionBuilder)
        {
            object currnetValue = optionBuilder.CurrentValueFunction(restoreDataObject);
            object defaultValue = optionBuilder.DefaultValueFunction(restoreDataObject);
            OptionValidationResult validationResult = optionBuilder.ValidateFunction(restoreDataObject, currnetValue, defaultValue);
            return new RestorePlanDetailInfo
            {
                Name = optionKey,
                CurrentValue = currnetValue,
                DefaultValue = defaultValue,
                IsReadOnly = validationResult.IsReadOnly,
                IsVisiable = validationResult.IsVisible,
                ErrorMessage = validationResult.ErrorMessage
            };
        }
    }

    internal class OptionBuilder
    {
        public Func<IRestoreDatabaseTaskDataObject, object> DefaultValueFunction { get; set; }
        public Func<IRestoreDatabaseTaskDataObject, object, object, OptionValidationResult> ValidateFunction { get; set; }
        public Func<IRestoreDatabaseTaskDataObject, object> CurrentValueFunction { get; set; }
        public Func<IRestoreDatabaseTaskDataObject, object, bool> SetValueFunction { get; set; }
    }

    internal class OptionValidationResult
    {
        public OptionValidationResult()
        {
            IsVisible = true;
            IsReadOnly = false;
            ErrorMessage = string.Empty;
        }
        public bool IsReadOnly { get; set; }
        public bool IsVisible { get; set; }

        public string ErrorMessage { get; set; }
    }
}
