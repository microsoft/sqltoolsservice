//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.UnitTests.RequestContextMocking;
using Moq;

namespace Microsoft.Kusto.ServiceLayer.UnitTests
{
    public abstract class ServiceTestBase
    {
        protected RegisteredServiceProvider ServiceProvider
        {
            get;
            set;
        }

        protected RegisteredServiceProvider CreateProvider()
        {
            ServiceProvider = new RegisteredServiceProvider();
            return ServiceProvider;
        }

        protected abstract RegisteredServiceProvider CreateServiceProviderWithMinServices();

        protected async Task RunAndVerify<T, TResult>(Func<RequestContext<T>, Task<TResult>> test, Action<TResult> verify)
        {
            T result = default(T);
            var contextMock = RequestContextMocks.Create<T>(r => result = r).AddErrorHandling(null);
            TResult actualResult = await test(contextMock.Object);
            if (actualResult == null && typeof(TResult) == typeof(T))
            {
                actualResult = (TResult)Convert.ChangeType(result, typeof(TResult));
            }
            VerifyResult<T, TResult>(contextMock, verify, actualResult);
        }

        protected async Task RunAndVerify<T>(Func<RequestContext<T>, Task> test, Action<T> verify)
        {
            T result = default(T);
            var contextMock = RequestContextMocks.Create<T>(r => result = r).AddErrorHandling(null);
            await test(contextMock.Object);
            VerifyResult<T>(contextMock, verify, result);
        }

        protected void VerifyResult<T, TResult>(Mock<RequestContext<T>> contextMock, Action<TResult> verify, TResult actual)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            contextMock.Verify(c => c.SendError(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            verify(actual);
        }

        protected void VerifyResult<T>(Mock<RequestContext<T>> contextMock, Action<T> verify, T actual)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            contextMock.Verify(c => c.SendError(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            verify(actual);
        }

        protected void VerifyErrorSent<T>(Mock<RequestContext<T>> contextMock)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Never);
            contextMock.Verify(c => c.SendError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }
    }
}
