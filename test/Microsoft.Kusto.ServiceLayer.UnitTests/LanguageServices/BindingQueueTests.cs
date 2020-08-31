using System;
using System.Threading;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.LanguageServices
{
    public class BindingQueueTests
    {
        [Test]
        public void QueueBindingOperation_Returns_Null_For_NullBindOperation()
        {
            var bindingQueue = new BindingQueue<ConnectedBindingContext>();
            var queueItem = bindingQueue.QueueBindingOperation("", null);
            Assert.IsNull(queueItem);
        }

        [Test]
        public void QueueBindingOperation_Returns_QueueItem()
        {
            var key = "key";
            var bindOperation = new Func<IBindingContext, CancellationToken, object>((context, token) => new Hover());
            Func<IBindingContext, object> timeoutOperation = (context) => LanguageService.HoverTimeout;
            Func<Exception, object> errorHandler = exception => new Exception();
            var bindingTimeout = 30;
            var waitForLockTimeout = 45;
            
            var bindingQueue = new BindingQueue<ConnectedBindingContext>();
            var queueItem = bindingQueue.QueueBindingOperation(key, 
                bindOperation,
                timeoutOperation,
                errorHandler, 
                bindingTimeout,
                waitForLockTimeout);
            
            Assert.AreEqual(key, queueItem.Key);
            Assert.AreEqual(bindOperation, queueItem.BindOperation);
            Assert.AreEqual(timeoutOperation, queueItem.TimeoutOperation);
            Assert.AreEqual(errorHandler, queueItem.ErrorHandler);
            Assert.AreEqual(bindingTimeout, queueItem.BindingTimeout);
            Assert.AreEqual(waitForLockTimeout, queueItem.WaitForLockTimeout);
        }
    }
}