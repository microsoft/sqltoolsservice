//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Reflection;
using Moq.Language;
using Moq.Language.Flow;

namespace Microsoft.SqlTools.ServiceLayer.Test.Utility
{
    public static class MoqExtensions
    {
        public delegate void OutAction<TOut>(out TOut outVal);

        public delegate void OutAction<in T1, TOut>(T1 arg1, out TOut outVal);

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, TOut>(
            this ICallback<TMock, TReturn> mock, OutAction<TOut> action) where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, TOut>(
            this ICallback<TMock, TReturn> mock, OutAction<T1, TOut> action) where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        private static IReturnsThrows<TMock, TReturn> OutCallbackInternal<TMock, TReturn>(
            ICallback<TMock, TReturn> mock, object action) where TMock : class
        {
            typeof(ICallback<TMock, TReturn>).GetTypeInfo()
                .Assembly.GetType("Moq.MethodCall")
                .GetMethod("SetCallbackWithArguments",
                    BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(mock, new[] { action });
            return mock as IReturnsThrows<TMock, TReturn>;

        }
    }
}
