//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime;
using NUnit.Framework;
using System.Reflection;
using Microsoft.SqlServer.XEvent.XELite;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using System.Threading;
using System.Linq;
using Moq;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Azure.Management.Sql.Models;
using System.ComponentModel.DataAnnotations;

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
            observer.Observable.Start();
            var retries = 10;
            while (!observer.Completed && retries-- > 0)
            {
                Thread.Sleep(100);
            }
            Assert.Multiple(() =>
            {
                Assert.That(observer.Completed, Is.True, $"Reading the missing file didn't complete in 1 second.");
                Assert.That(observer.Error?.GetBaseException(), Is.InstanceOf<FileNotFoundException>(), $"Expected Error from missing file. Error:{observer.Error}");
            });
        }

        [Test]
        public void XeStreamObservable_can_Start_after_Close()
        {
            var observer1 = InitializeFileObserver();
            var observable = observer1.Observable;
            observer1.OnEventAdded += (o, e) => { observable.Close(); };
            observable.Start();
            var retries = 10;
            while (!observer1.Completed && retries-- > 0)
            {
                Thread.Sleep(100);
            }
            Assert.Multiple(() =>
            {
                Assert.That(observer1.ProfilerEvents.Select(p => p.Name), Is.EqualTo(new[] { "rpc_completed" }), "Only 1 event expected before Close() for observer1");
            });
            var firstEvent = observer1.ProfilerEvents[0];
            var observer2 = new XeStreamObserver() { Observable = observable };
            observable.Subscribe(observer2);
            Console.WriteLine("Starting the xevent observable for 1 second to process some events");
            observable.Start();
            retries = 100;
            while (!observer2.Completed && retries-- > 0)
            {
                Thread.Sleep(100);
            }
            Assert.Multiple(() =>
            {
                Assert.That(observer2.Completed, Is.True, "observer2 should have read to completion");
                Assert.That(observer1.ProfilerEvents.Select(p => (p.Name, p.Timestamp)), Is.EqualTo(new[] { (firstEvent.Name, firstEvent.Timestamp) }), "Only 1 event expected for observer1 after restarting the observer");
                Assert.That(observer2.ProfilerEvents.Count, Is.EqualTo(149), "observer2 should have all the events from the file");
            });
        }

        private static ProfilerEvent GetOneEvent()
        {
            return new ProfilerEvent("profilerEvent", "timeStamp");
        }

        private XeStreamObserver InitializeFileObserver(string filePath = null)
        {
            filePath ??= Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Profiler", "TestXel_0.xel");
            var fetcher = new XEFileEventStreamer(filePath);
            var observable = new XeStreamObservable(fetcher);
            var observer = new XeStreamObserver() { Observable = observable };
            observable.Subscribe(observer);
            return observer;
        }
    }

    class XeStreamObserver : IObserver<ProfilerEvent>
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
