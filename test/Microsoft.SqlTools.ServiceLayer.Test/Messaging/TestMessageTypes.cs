//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.SqlTools.ServiceLayer.Test.Messaging
{
    #region Request Types

    internal class TestRequest 
    {
        public Task ProcessMessage(MessageWriter messageWriter)
        {
            return Task.FromResult(false);
        }
    }

    internal class TestRequestArguments
    {
        public string SomeString { get; set; }
    }

    #endregion

    #region Response Types

    internal class TestResponse
    {
    }

    internal class TestResponseBody
    {
        public string SomeString { get; set; }
    }

    #endregion

    #region Event Types

    internal class TestEvent
    {
    }

    internal class TestEventBody
    {
        public string SomeString { get; set; }
    }

    #endregion
}
