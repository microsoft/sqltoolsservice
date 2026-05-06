//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    internal sealed class ObjectExplorerTaskManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ObjectExplorerTaskHandle> tasks = new();

        public ObjectExplorerTaskHandle Register(
            string taskId,
            CancellationTokenSource cancellationTokenSource,
            Func<Task>? onCancel = null)
        {
            ObjectExplorerTaskHandle handle = new(taskId, cancellationTokenSource, onCancel);
            tasks.AddOrUpdate(taskId, handle, (_, oldHandle) =>
            {
                oldHandle.Dispose();
                return handle;
            });
            return handle;
        }

        public bool Cancel(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId) || !tasks.TryGetValue(taskId, out ObjectExplorerTaskHandle? handle))
            {
                return false;
            }

            handle.Cancel();
            return true;
        }

        public void Complete(string taskId)
        {
            if (tasks.TryRemove(taskId, out ObjectExplorerTaskHandle? handle))
            {
                handle.Dispose();
            }
        }

        public void Dispose()
        {
            foreach (ObjectExplorerTaskHandle handle in tasks.Values)
            {
                handle.Dispose();
            }

            tasks.Clear();
        }
    }

    internal sealed class ObjectExplorerTaskHandle : IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Func<Task>? onCancel;

        public ObjectExplorerTaskHandle(
            string taskId,
            CancellationTokenSource cancellationTokenSource,
            Func<Task>? onCancel)
        {
            TaskId = taskId;
            this.cancellationTokenSource = cancellationTokenSource;
            this.onCancel = onCancel;
        }

        public string TaskId { get; }

        public CancellationToken Token => cancellationTokenSource.Token;

        public void Cancel()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }

            if (onCancel != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await onCancel();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to cancel Object Explorer task {TaskId}: {ex}");
                    }
                });
            }
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }
    }
}
