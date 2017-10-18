//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public class TaskExtensionTests
    {
        [Fact]
        public async Task ContinueWithOnFaultedNullContinuation()
        {
            // Setup: Create a task that will definitely fault
            Task failureTask = new Task(() => throw new Exception("It fail!"));
            
            // If: I continue on fault and start the task
            Task continuationTask = failureTask.ContinueWithOnFaulted(null);
            failureTask.Start();
            await continuationTask;

            // Then: The task should have completed without fault
            Assert.Equal(TaskStatus.RanToCompletion, continuationTask.Status);
        }

        [Fact]
        public async Task ContinueWithOnFaultedContinuatation()
        {
            // Setup: 
            // ... Create a new task that will definitely fault
            Task failureTask = new Task(() => throw new Exception("It fail!"));
            
            // ... Create a quick continuation task that will signify if it's been called
            Task providedTask = null;

            // If: I continue on fault, with a continuation task
            Task continuationTask = failureTask.ContinueWithOnFaulted(task => { providedTask = task; });
            failureTask.Start();
            await continuationTask;
            
            // Then:
            // ... The task should have completed without fault
            Assert.Equal(TaskStatus.RanToCompletion, continuationTask.Status);
            
            // ... The continuation action should have been called with the original failure task
            Assert.Equal(failureTask, providedTask);
        }
    }
}