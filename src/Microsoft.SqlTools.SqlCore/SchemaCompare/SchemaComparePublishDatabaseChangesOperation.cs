//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Host-agnostic schema compare publish database changes operation
    /// </summary>
    public class SchemaComparePublishDatabaseChangesOperation : SchemaComparePublishChangesOperation
    {
        private readonly SchemaComparison comparison;
        private EventInfo progressChangedEvent;
        private object progressChangedEventSource;
        private Delegate progressChangedEventHandler;

        public SchemaComparePublishDatabaseChangesParams Parameters { get; }

        public SchemaComparePublishResult PublishResult { get; set; }

        public SchemaComparePublishDatabaseChangesOperation(
            SchemaComparePublishDatabaseChangesParams parameters,
            SchemaComparisonResult comparisonResult,
            SchemaComparison comparison = null) : base(comparisonResult)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Parameters = parameters;
            OperationId = !string.IsNullOrEmpty(parameters.OperationId) ? parameters.OperationId : Guid.NewGuid().ToString();
            this.comparison = comparison;
        }

        public override void Execute()
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                SubscribeToProgressEvents();

                PublishResult = ComparisonResult.PublishChangesToDatabase(CancellationToken);

                if (!PublishResult.Success)
                {
                    // Sending only errors and warnings - because overall message might be too big for task view
                    ErrorMessage = String.Join(Environment.NewLine, this.PublishResult.Errors.Where(x => x.MessageType == DacMessageType.Error || x.MessageType == DacMessageType.Warning));
                    throw new DacServicesException(ErrorMessage);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Error(string.Format("Schema compare publish database changes operation {0} failed with exception {1}", this.OperationId, e.Message));
                throw;
            }
            finally
            {
                UnsubscribeFromProgressEvents();
            }
        }

        private void SubscribeToProgressEvents()
        {
            if (comparison == null)
            {
                return;
            }

            progressChangedEventSource = comparison;
            progressChangedEvent = comparison.GetType().GetEvent("ProgressChanged", BindingFlags.Instance | BindingFlags.Public);

            // Current DacFx surfaces schema compare progress on internal DataModel.ProgressChanged
            if (progressChangedEvent == null)
            {
                PropertyInfo dataModelProperty = comparison.GetType().GetProperty("DataModel", BindingFlags.Instance | BindingFlags.NonPublic);
                object dataModel = dataModelProperty?.GetValue(comparison);
                if (dataModel != null)
                {
                    progressChangedEventSource = dataModel;
                    progressChangedEvent = dataModel.GetType().GetEvent("ProgressChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
            }

            if (progressChangedEvent == null || progressChangedEventSource == null)
            {
                return;
            }

            MethodInfo handlerMethod = GetType().GetMethod(nameof(HandleProgressChanged), BindingFlags.Instance | BindingFlags.NonPublic);
            progressChangedEventHandler = Delegate.CreateDelegate(progressChangedEvent.EventHandlerType, this, handlerMethod);
            progressChangedEvent.AddEventHandler(progressChangedEventSource, progressChangedEventHandler);
        }

        private void UnsubscribeFromProgressEvents()
        {
            if (progressChangedEvent != null && progressChangedEventHandler != null && progressChangedEventSource != null)
            {
                progressChangedEvent.RemoveEventHandler(progressChangedEventSource, progressChangedEventHandler);
            }

            progressChangedEvent = null;
            progressChangedEventSource = null;
            progressChangedEventHandler = null;
        }

        private void HandleProgressChanged(object sender, object progressEventArgs)
        {
            string status = null;
            if (progressEventArgs != null)
            {
                PropertyInfo statusProperty = progressEventArgs.GetType().GetProperty("Status");
                status = statusProperty?.GetValue(progressEventArgs)?.ToString();
                if (status == null)
                {
                    PropertyInfo progressCodeProperty = progressEventArgs.GetType().GetProperty("ProgressCode");
                    status = progressCodeProperty?.GetValue(progressEventArgs)?.ToString();
                }
            }

            OnProgressChanged(sender, new SchemaCompareProgressChangedEventArgs(status, progressEventArgs));
        }
    }
}
