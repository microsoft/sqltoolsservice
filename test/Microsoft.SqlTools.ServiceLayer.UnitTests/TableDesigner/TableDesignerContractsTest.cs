//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TableDesigner
{
    public class TableDesignerContractsTest
    {
        [Test]
        public void InitializeTableDesignerRequestUsesSessionRequestParams()
        {
            RequestType<InitializeTableDesignerRequestParams, TableDesignerInfo> requestType =
                InitializeTableDesignerRequest.Type;

            Assert.IsNotNull(requestType);
        }

        [Test]
        public void TableDesignerNotificationsAreDefined()
        {
            EventType<TableDesignerProgressNotificationParams> progressType =
                TableDesignerProgressNotification.Type;
            EventType<TableDesignerMessageNotificationParams> messageType =
                TableDesignerMessageNotification.Type;

            Assert.IsNotNull(progressType);
            Assert.IsNotNull(messageType);
        }

        [Test]
        public void TableDesignerMessageNotificationIncludesDacFxMessageDetails()
        {
            var message = new TableDesignerMessageNotificationParams()
            {
                SessionId = "session",
                Operation = "Publish",
                MessageType = "Message",
                Message = "message",
                Number = 123,
                Prefix = "SQL",
                Progress = 0.5,
                SchemaName = "dbo",
                TableName = "Table"
            };

            Assert.AreEqual(123, message.Number);
            Assert.AreEqual("SQL", message.Prefix);
            Assert.AreEqual(0.5, message.Progress);
            Assert.AreEqual("dbo", message.SchemaName);
            Assert.AreEqual("Table", message.TableName);
        }
    }
}
