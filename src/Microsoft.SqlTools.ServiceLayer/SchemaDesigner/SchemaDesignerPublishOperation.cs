//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    internal sealed class SchemaDesignerPublishOperation : ITaskOperation
    {
        private readonly SchemaDesignerSession session;

        public SchemaDesignerPublishOperation(SchemaDesignerSession session)
        {
            Validate.IsNotNull(nameof(session), session);
            this.session = session;
        }

        public string ErrorMessage { get; private set; }

        public SqlTask SqlTask { get; set; }

        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                session.PublishSchema();
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