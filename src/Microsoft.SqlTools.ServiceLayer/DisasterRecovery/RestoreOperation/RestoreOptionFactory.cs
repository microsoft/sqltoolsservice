//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
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
            if (optionBuilders.TryGetValue(optionKey, out OptionBuilder? value))
            {
                return Create(optionKey, restoreDataObject, value);
            }
            else
            {
                Logger.Write(TraceEventType.Warning, $"cannot find restore option builder for {optionKey}");
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
            if (optionBuilders.TryGetValue(optionKey, out OptionBuilder? value))
            {
                var builder = value;
                var currentValue = builder.CurrentValueFunction(restoreDataObject);
                var defaultValue = builder.DefaultValueFunction(restoreDataObject);
                var validateResult = builder.ValidateFunction(restoreDataObject, currentValue, defaultValue);
                optionInfo.IsReadOnly = validateResult.IsReadOnly;
            }
            else
            {
                Logger.Write(TraceEventType.Warning, $"cannot find restore option builder for {optionKey}");
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
                if (optionBuilders.TryGetValue(optionKey, out OptionBuilder? builder))
                {
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
                            Logger.Write(TraceEventType.Warning, $"Failed tp set restore option {optionKey} error:{ex.Message}");

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
                            Logger.Write(TraceEventType.Warning, $"Failed to set restore option  {optionKey} to default value");
                        }
                    }
                }
                else
                {
                    Logger.Write(TraceEventType.Warning, $"cannot find restore option builder for {optionKey}");
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
            if (optionBuilders.TryGetValue(optionKey, out OptionBuilder? builder))
            {
                var currentValue = builder.CurrentValueFunction(restoreDataObject);
                var defaultValue = builder.DefaultValueFunction(restoreDataObject);
                OptionValidationResult result = optionBuilders[optionKey].ValidateFunction(restoreDataObject, currentValue, defaultValue);
                if (result.IsReadOnly)
                {
                    if (!ValueEqualsDefault(currentValue, defaultValue))
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
                Logger.Write(TraceEventType.Warning, errorMessage);
            }

            return errorMessage;
        }

        private bool ValueEqualsDefault(object currentValue, object defaultValue)
        {
            if (currentValue == null)
            {
                return defaultValue == null;
            }
            else if (defaultValue == null)
            {
                return false;
            }
            return currentValue.Equals(defaultValue);
        }


        private RestoreOptionFactory()
        {
            Register(RestoreOptionsHelper.RelocateDbFiles,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RelocateAllFiles;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult
                        {
                            IsReadOnly = restoreDataObject.DbFiles.Count == 0
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RelocateAllFiles = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.DataFileFolder,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultDataFileFolder;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DataFilesFolder;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult
                        {
                            IsReadOnly = !restoreDataObject.RelocateAllFiles
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.DataFilesFolder = GetValueAs<string>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.LogFileFolder,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultLogFileFolder;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.LogFilesFolder;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult
                        {
                            IsReadOnly = !restoreDataObject.RelocateAllFiles
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.LogFilesFolder = GetValueAs<string>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.ReplaceDatabase,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.ReplaceDatabase;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult();
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.ReplaceDatabase = GetValueAs<bool>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.KeepReplication,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.KeepReplication;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = restoreDataObject.RestoreOptions.RecoveryState == DatabaseRecoveryState.WithNoRecovery
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.KeepReplication = GetValueAs<bool>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.SetRestrictedUser,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.SetRestrictedUser;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.SetRestrictedUser = GetValueAs<bool>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.RecoveryState,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return DatabaseRecoveryState.WithRecovery.ToString();
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.RecoveryState.ToString();
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.RecoveryState = GetValueAs<DatabaseRecoveryState>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.StandbyFile,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultStandbyFile;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.RestoreOptions.StandByFile;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = restoreDataObject.RestoreOptions.RecoveryState != DatabaseRecoveryState.WithStandBy
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.RestoreOptions.StandByFile = GetValueAs<string>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.BackupTailLog,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.IsTailLogBackupPossible;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.BackupTailLog;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = !restoreDataObject.IsTailLogBackupPossible
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.BackupTailLog = GetValueAs<bool>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.TailLogBackupFile,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.DefaultTailLogbackupFile;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.TailLogBackupFile;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = !restoreDataObject.BackupTailLog | !restoreDataObject.IsTailLogBackupPossible
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.TailLogBackupFile = GetValueAs<string>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.TailLogWithNoRecovery,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.IsTailLogBackupWithNoRecoveryPossible;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.TailLogWithNoRecovery;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = !restoreDataObject.BackupTailLog | !restoreDataObject.IsTailLogBackupWithNoRecoveryPossible
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.TailLogWithNoRecovery = GetValueAs<bool>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.CloseExistingConnections,
                new OptionBuilder
                (
                    defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return false;
                    },
                    currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                    {
                        return restoreDataObject.CloseExistingConnections;
                    },
                    validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                    {
                        return new OptionValidationResult()
                        {
                            IsReadOnly = !restoreDataObject.CanDropExistingConnections
                        };
                    },
                    setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                    {

                        restoreDataObject.CloseExistingConnections = GetValueAs<bool>(value);
                        return true;
                    }
                ));
            Register(RestoreOptionsHelper.SourceDatabaseName,
               new OptionBuilder
               (
                   defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.DefaultSourceDbName;
                   },
                   currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.SourceDatabaseName;
                   },
                   validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                   {
                       string errorMessage = string.Empty;
                       var sourceDbNames = restoreDataObject.SourceDbNames;
                       if (currentValue == null)
                       {
                           errorMessage = "Source database name is not valid";
                       }
                       return new OptionValidationResult()
                       {
                           ErrorMessage = errorMessage
                       };
                   },
                   setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                   {

                       restoreDataObject.SourceDatabaseName = GetValueAs<string>(value);
                       return true;
                   }
               ));
            Register(RestoreOptionsHelper.TargetDatabaseName,
               new OptionBuilder
               (
                   defaultValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.OverwriteTargetDatabase ? restoreDataObject.DefaultSourceDbName : restoreDataObject.DefaultTargetDbName;
                   },
                   currentValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject) =>
                   {
                       return restoreDataObject.TargetDatabaseName;
                   },
                   validateFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object currentValue, object defaultValue) =>
                   {
                       return new OptionValidationResult()
                       {
                           IsReadOnly = false
                       };
                   },
                   setValueFunction: (IRestoreDatabaseTaskDataObject restoreDataObject, object value) =>
                   {

                       restoreDataObject.TargetDatabaseName = GetValueAs<string>(value);
                       return true;
                   }
               ));
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

        public OptionBuilder(Func<IRestoreDatabaseTaskDataObject, object> defaultValueFunction, Func<IRestoreDatabaseTaskDataObject, object, object, OptionValidationResult> validateFunction, Func<IRestoreDatabaseTaskDataObject, object> currentValueFunction, Func<IRestoreDatabaseTaskDataObject, object, bool> setValueFunction)
        {
            this.DefaultValueFunction = defaultValueFunction;
            this.ValidateFunction = validateFunction;
            this.CurrentValueFunction = currentValueFunction;
            this.SetValueFunction = setValueFunction;
        }
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
