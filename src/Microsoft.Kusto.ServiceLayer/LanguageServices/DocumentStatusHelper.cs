//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Helper class to send events to the client
    /// </summary>
    public class DocumentStatusHelper
    {
        public const string DefinitionRequested = "DefinitionRequested";
        public const string DefinitionRequestCompleted = "DefinitionRequestCompleted";

        /// <summary>
        /// Sends an event for specific document using the existing request context
        /// </summary>
        public static void SendStatusChange<T>(RequestContext<T> requestContext, TextDocumentPosition textDocumentPosition, string status)
        {
            Task.Factory.StartNew(async () =>
            {
                if (requestContext != null)
                {
                    string ownerUri = textDocumentPosition != null && textDocumentPosition.TextDocument != null ? textDocumentPosition.TextDocument.Uri : "";
                    await requestContext.SendEvent(StatusChangedNotification.Type, new StatusChangeParams()
                    {
                        OwnerUri = ownerUri,
                        Status = status
                    });
                }
            });
        }

        /// <summary>
        /// Sends a telemetry event for specific document using the existing request context
        /// </summary>
        public static void SendTelemetryEvent<T>(RequestContext<T> requestContext, string telemetryEvent)
        {
            Validate.IsNotNull(nameof(requestContext), requestContext);
            Validate.IsNotNullOrWhitespaceString(nameof(telemetryEvent), telemetryEvent);
            Task.Factory.StartNew(async () =>
            {
                await requestContext.SendEvent(TelemetryNotification.Type, new TelemetryParams()
                {
                    Params = new TelemetryProperties
                    {
                        EventName = telemetryEvent
                    }
                });
            });
        }

        /// <summary>
        /// Sends a telemetry event for specific document using the existing request context
        /// </summary>
        public static void SendTelemetryEvent<T>(RequestContext<T> requestContext, TelemetryProperties telemetryProps)
        {
            Validate.IsNotNull(nameof(requestContext), requestContext);
            Validate.IsNotNull(nameof(telemetryProps), telemetryProps);
            Validate.IsNotNullOrWhitespaceString("telemetryProps.EventName", telemetryProps.EventName);
            Task.Factory.StartNew(async () =>
            {
                await requestContext.SendEvent(TelemetryNotification.Type, new TelemetryParams()
                {
                    Params = telemetryProps
                });
            });
        }
    }
}
