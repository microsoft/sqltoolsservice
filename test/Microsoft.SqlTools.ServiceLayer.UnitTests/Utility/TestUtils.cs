using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public static class TestUtils
    {

        /// <summary>
        /// Wait for a condition to be true for a limited amount of time.
        /// </summary>
        /// <param name="condition">Function that returns a boolean on a condition</param>
        /// <param name="intervalMilliseconds">Number of milliseconds to wait between test intervals.</param>
        /// <param name="intervalCount">Number of test intervals to perform before giving up.</param>
        /// <returns>True if the condition was met before the test interval limit.</returns>
        public static bool WaitFor(Func<bool> condition, int intervalMilliseconds = 10, int intervalCount = 200)
        {
            int count = 0;
            while (count++ < intervalCount && !condition.Invoke())
            {
                Thread.Sleep(intervalMilliseconds);
            }

            return (count < intervalCount);
        }


        public static async Task RunAndVerify<T>(Func<RequestContext<T>, Task> test, Action<T> verify)
        {
            T result = default(T);
            var contextMock = RequestContextMocks.Create<T>(r => result = r).AddErrorHandling(null);
            await test(contextMock.Object);
            VerifyResult(contextMock, verify, result);
        }

        public static void VerifyErrorSent<T>(Mock<RequestContext<T>> contextMock)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Never);
            contextMock.Verify(c => c.SendError(It.IsAny<string>()), Times.Once);
        }

        public static void VerifyResult<T, U>(Mock<RequestContext<T>> contextMock, U expected, U actual)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            Assert.Equal(expected, actual);
            contextMock.Verify(c => c.SendError(It.IsAny<string>()), Times.Never);
        }

        public static void VerifyResult<T>(Mock<RequestContext<T>> contextMock, Action<T> verify, T actual)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            contextMock.Verify(c => c.SendError(It.IsAny<string>()), Times.Never);
            verify(actual);
        }

    }
}
