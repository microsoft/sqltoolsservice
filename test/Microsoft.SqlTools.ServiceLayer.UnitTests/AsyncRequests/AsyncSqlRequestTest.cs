//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.AsyncRequest;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using System.Threading;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.AsyncRequests
{
    public class AsyncSqlRequestTest
    {
        [Fact]
        public void AsyncTaskShouldSendTheResultWhenCompleted()
        {
            string ownerUri = "test uri";
            AsyncRequestParams requestParams = new AsyncRequestParams
            {
                OwnerUri = ownerUri
            };
            SqlServiceStub serviceStub = new SqlServiceStub();
            AsyncSqlResponse result;
            var contextMock = RequestContextMocks.Create<AsyncSqlResponse>(r => result = r).AddErrorHandling(null);

            AsyncRequestHandler<AsyncSqlResponse>.HandleRequestAsync(contextMock.Object,
                serviceStub.FunctionToRun, requestParams, 0);

            Thread.Sleep(1000);
            serviceStub.Stop();
            Thread.Sleep(1000);

            contextMock.Verify(x => x.SendResult(It.Is<AsyncSqlResponse>(r => r.OwnerUri == ownerUri)));
        }

        [Fact]
        public void AsyncTaskShouldSendErrorIfTimeout()
        {
            string ownerUri = "test uri";
            AsyncRequestParams requestParams = new AsyncRequestParams
            {
                OwnerUri = ownerUri
            };
            SqlServiceStub serviceStub = new SqlServiceStub();
            AsyncSqlResponse result;
            var contextMock = RequestContextMocks.Create<AsyncSqlResponse>(r => result = r).AddErrorHandling(null);

            AsyncRequestHandler<AsyncSqlResponse>.HandleRequestAsync(contextMock.Object,
                serviceStub.FunctionToRun, requestParams, 2);

            Thread.Sleep(3000);
            serviceStub.Stop();
            Thread.Sleep(1000);

            contextMock.Verify(x => x.SendResult(It.Is<AsyncSqlResponse>(r => r.OwnerUri == ownerUri && !string.IsNullOrEmpty(r.ErrorMessage))));
        }
    }
}
