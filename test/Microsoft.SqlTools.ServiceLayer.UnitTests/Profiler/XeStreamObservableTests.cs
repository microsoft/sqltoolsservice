//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Reflection;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using System.Threading;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    public class XeStreamObservableTests
    { 

        /// <summary>
        /// this might technically be an integration test but putting it here because it doesn't require any connectivity.
        /// </summary>
        [Test]
        public void XeStreamObservable_reads_entire_xel_file()
        {
            var observer = InitializeFileObserver();
            observer.Observable.Start();
            var retries = 100;
            while (!observer.Completed && retries-- > 0)
            {
                Thread.Sleep(100);
            }
            Assert.That(observer.Completed, Is.True, $"Reading the file didn't complete in 10 seconds. Events read: {observer.ProfilerEvents.Count}");
            Assert.Multiple(() =>
            {
                Assert.That(observer.ProfilerEvents.Count, Is.EqualTo(149), "Number of events read");
                Assert.That(observer.ProfilerEvents[0].Name, Is.EqualTo("rpc_completed"), "First event in the file");
                Assert.That(observer.ProfilerEvents.Last().Name, Is.EqualTo("sql_batch_completed"), "Last event in the file");

            });
        }

        [Test]
        public void XeStreamObservable_calls_OnError_when_the_fetcher_fails()
        {
            var observer = InitializeFileObserver("thispathdoesnotexist.xel");
            Assert.Throws<FileNotFoundException>(observer.Observable.Start);    // exception handled in StartLocalFileSession task (ProfilerServic.cs)
            // exception not thrown unless the file is been accessed
        }

        private XeStreamObserver InitializeFileObserver(string filePath = null)
        {
            filePath ??= Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Profiler", "TestXel_0.xel");
            var fetcher = new XEventFetcher(filePath);
            var observable = new XeStreamObservable(fetcher);
            var observer = new XeStreamObserver() { Observable = observable };
            observable.Subscribe(observer);
            return observer;
        }
    }

    sealed class XeStreamObserver : IObserver<ProfilerEvent>
    {
        public XeStreamObservable Observable { get; set; }
        public readonly List<ProfilerEvent> ProfilerEvents = new List<ProfilerEvent>();

        public bool Completed { get; private set; }

        public Exception Error { get; private set; }

        public void OnCompleted()
        {
            Completed = true;
        }

        public void OnError(Exception error)
        {
            Error = error;
        }

        public void OnNext(ProfilerEvent value)
        {
            ProfilerEvents.Add(value);
            OnEventAdded?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnEventAdded;
    }
}
