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
        #region Continue with Action
        
        [Fact]
        public async Task ContinueWithOnFaultedActionNullContinuation()
        {
            // Setup: Create a task that will definitely fault
            Task failureTask = new Task(() => { throw new Exception("It fail!"); });
            
            // If: I continue on fault and start the task
            Task continuationTask = failureTask.ContinueWithOnFaulted((Action<Task>)null);
            failureTask.Start();
            await continuationTask;

            // Then: The task should have completed without fault
            Assert.Equal(TaskStatus.RanToCompletion, continuationTask.Status);
        }

        [Fact]
        public async Task ContinueWithOnFaultedActionContinuatation()
        {
            // Setup: 
            // ... Create a new task that will definitely fault
            Task failureTask = new Task(() => { throw new Exception("It fail!"); });
            
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

        [Fact]
        public async Task ContinueWithOnFaultedActionExceptionInContinuation()
        {
            // Setup: 
            // ... Create a new task that will definitely fault
            Task failureTask = new Task(() => { throw new Exception("It fail!"); });
            
            // ... Create a quick continuation task that will signify if it's been called
            Task providedTask = null;
            
            // If: I continue on fault, with a continuation task that will fail
            Action<Task> failureContinuation = task =>
            {
                providedTask = task;
                throw new Exception("It fail!");
            };
            Task continuationTask = failureTask.ContinueWithOnFaulted(failureContinuation);
            failureTask.Start();
            await continuationTask;
            
            // Then:
            // ... The task should have completed without fault
            Assert.Equal(TaskStatus.RanToCompletion, continuationTask.Status);
            
            // ... The continuation action should have been called with the original failure task
            Assert.Equal(failureTask, providedTask);
        }
        
        #endregion
        
        #region Continue with Task
        
        [Fact]
        public async Task ContinueWithOnFaultedFuncNullContinuation()
        {
            // Setup: Create a task that will definitely fault
            Task failureTask = new Task(() => { throw new Exception("It fail!"); });
            
            // If: I continue on fault and start the task
            // ReSharper disable once RedundantCast -- Just to enforce we're running the right overload
            Task continuationTask = failureTask.ContinueWithOnFaulted((Func<Task, Task>)null);
            failureTask.Start();
            await continuationTask;

            // Then: The task should have completed without fault
            Assert.Equal(TaskStatus.RanToCompletion, continuationTask.Status);
        }

        [Fact]
        public async Task ContinueWithOnFaultedFuncContinuatation()
        {
            // Setup: 
            // ... Create a new task that will definitely fault
            Task failureTask = new Task(() => { throw new Exception("It fail!"); });
            
            // ... Create a quick continuation task that will signify if it's been called
            Task providedTask = null;

            // If: I continue on fault, with a continuation task
            Func<Task, Task> continuationFunc = task =>
            {
                providedTask = task;
                return Task.CompletedTask;
            };
            Task continuationTask = failureTask.ContinueWithOnFaulted(continuationFunc);
            failureTask.Start();
            await continuationTask;
            
            // Then:
            // ... The task should have completed without fault
            Assert.Equal(TaskStatus.RanToCompletion, continuationTask.Status);
            
            // ... The continuation action should have been called with the original failure task
            Assert.Equal(failureTask, providedTask);
        }

        [Fact]
        public async Task ContinueWithOnFaultedFuncExceptionInContinuation()
        {
            // Setup: 
            // ... Create a new task that will definitely fault
            Task failureTask = new Task(() => { throw new Exception("It fail!"); });
            
            // ... Create a quick continuation task that will signify if it's been called
            Task providedTask = null;
            
            // If: I continue on fault, with a continuation task that will fail
            Func<Task, Task> failureContinuation = task =>
            {
                providedTask = task;
                throw new Exception("It fail!");
            };
            Task continuationTask = failureTask.ContinueWithOnFaulted(failureContinuation);
            failureTask.Start();
            await continuationTask;
            
            // Then:
            // ... The task should have completed without fault
            Assert.Equal(TaskStatus.RanToCompletion, continuationTask.Status);
            
            // ... The continuation action should have been called with the original failure task
            Assert.Equal(failureTask, providedTask);
        }
        
        #endregion
    }
}