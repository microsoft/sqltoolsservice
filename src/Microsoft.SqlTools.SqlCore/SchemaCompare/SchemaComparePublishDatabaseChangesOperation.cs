//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;
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
        private bool progressSubscribed;

        public SchemaComparePublishDatabaseChangesParams Parameters { get; }

        public SchemaComparePublishResult PublishResult { get; set; }

        public SchemaComparePublishDatabaseChangesOperation(
            SchemaComparePublishDatabaseChangesParams parameters,
            SchemaComparisonResult comparisonResult) : base(comparisonResult)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Parameters = parameters;
            OperationId = !string.IsNullOrEmpty(parameters.OperationId) ? parameters.OperationId : Guid.NewGuid().ToString();
        }

        public override void Execute()
        {
            CancellationToken.ThrowIfCancellationRequested();

            try
            {
                SubscribeToProgressChanged();

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
                UnsubscribeFromProgressChanged();
            }
        }

        private void SubscribeToProgressChanged()
        {
            try
            {
                dynamic comparisonResult = ComparisonResult;
                // DacFx progress events are expected to provide sender and event-args objects to the callback.
                comparisonResult.ProgressChanged += (Action<object, object>)HandleProgressChanged;
                progressSubscribed = true;
            }
            catch (RuntimeBinderException)
            {
                progressSubscribed = false;
                Logger.Warning($"Unable to subscribe to schema compare publish progress on operation {OperationId} because the current DacFx version does not expose SchemaComparisonResult.ProgressChanged.");
            }
        }

        private void UnsubscribeFromProgressChanged()
        {
            if (!progressSubscribed)
            {
                return;
            }

            try
            {
                dynamic comparisonResult = ComparisonResult;
                comparisonResult.ProgressChanged -= (Action<object, object>)HandleProgressChanged;
            }
            catch (RuntimeBinderException)
            {
                // no-op
            }
            finally
            {
                progressSubscribed = false;
            }
        }

        private void HandleProgressChanged(object sender, object e)
        {
            if (e is EventArgs eventArgs)
            {
                OnProgressChanged(sender, eventArgs);
            }
        }
    }
}
