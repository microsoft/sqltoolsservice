//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.TaskServices
{
    public class RequstParamStub : IScriptableRequestParams
    {
        public TaskExecutionMode TaskExecutionMode { get; set; }
        public string OwnerUri { get; set; }
        public string DatabaseName { get; set; }
    }
}
