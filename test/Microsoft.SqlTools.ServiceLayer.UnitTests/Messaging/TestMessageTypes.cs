//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    #region Request Types

    internal sealed class TestRequest 
    {
        public Task ProcessMessage(MessageWriter messageWriter)
        {
            return Task.FromResult(false);
        }
    }

    internal sealed class TestRequestArguments
    {
        public string SomeString { get; set; }
    }

    #endregion

    #region Response Types

    internal sealed class TestResponse
    {
    }

    internal sealed class TestResponseBody
    {
        public string SomeString { get; set; }
    }

    #endregion

    #region Event Types

    internal sealed class TestEvent
    {
    }

    internal sealed class TestEventBody
    {
        public string SomeString { get; set; }
    }

    #endregion
}
