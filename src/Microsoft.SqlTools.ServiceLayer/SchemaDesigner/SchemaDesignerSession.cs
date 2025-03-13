//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerSession : IDisposable
    {
        private SchemaDesignerModel InitialSchema;
        private string SessionId;

        public SchemaDesignerSession(string sessionId, SchemaDesignerModel initialSchema)
        {

            InitialSchema = initialSchema;
            SessionId = sessionId;
        }

        public void Dispose()
        {
        }
    }
}