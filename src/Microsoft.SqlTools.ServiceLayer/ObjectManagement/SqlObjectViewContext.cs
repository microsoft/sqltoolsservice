//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable
using System;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public abstract class SqlObjectViewContext : IDisposable
    {
        public SqlObjectViewContext(InitializeViewRequestParams parameters)
        {
            this.Parameters = parameters;
        }

        public InitializeViewRequestParams Parameters { get; }

        public abstract void Dispose();
    }

    public class InitializeViewResult
    {
        public SqlObjectViewContext Context { get; set; }
        public SqlObjectViewInfo ViewInfo { get; set; }
    }
}