//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.DataProtocol.Hosting.UnitTests
{   
    public static class TaskExtensions 
    {
        public static async Task<Task> WithTimeout(this Task task, TimeSpan timeout)
        {
            Task delayTask = Task.Delay(TimeSpan.FromSeconds(1));
            Task firstCompleted = await Task.WhenAny(task, delayTask);
            if (firstCompleted == delayTask)
            {
                throw new Exception("Task timed out");
            }

            return task;
        }
    } 
}