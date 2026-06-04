//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests
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

        protected async Task RunAndVerify<TResult>(Func<Task<TResult>> test, Action<TResult> verify)
        {
            TResult actualResult = await test();
            verify(actualResult);
        }

        protected void RunAndVerifyError<T>(Func<Task<T>> test)
        {
            Assert.ThrowsAsync<Exception>(async () => await test());
        }

        protected void VerifyResult<TResult>(Action<TResult> verify, TResult actual)
        {
            verify(actual);
        }

    }
}
