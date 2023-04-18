//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public interface ISqlObjectViewContext : IDisposable
    {
        public SqlObjectType ObjectType { get; set; }
        public string ConnectionUri { get; set; }
    }
}