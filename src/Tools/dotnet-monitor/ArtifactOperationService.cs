// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ArtifactOperationService : IDiagnosticLifetimeService
    {
        private LinkedList<IArtifactOperation> _operations = new();
        private SemaphoreSlim _semaphore = new(1);

        public ValueTask StartAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                List<Task> _stopTasks = new(_operations.Count);
                foreach (IArtifactOperation operation in _operations)
                {
                    _stopTasks.Add(operation.StopAsync(cancellationToken).WaitAsync(cancellationToken).SafeAwait());
                }
                _operations.Clear();

                await Task.WhenAll(_stopTasks);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IAsyncDisposable> RegisterAsync(IArtifactOperation operation, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                return new Registration(_operations.AddLast(operation), _semaphore);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private sealed class Registration : IAsyncDisposable
        {
            private LinkedListNode<IArtifactOperation> _node;
            private SemaphoreSlim _semaphore;

            public Registration(LinkedListNode<IArtifactOperation> node, SemaphoreSlim semaphore)
            {
                _node = node;
                _semaphore = semaphore;
            }

            public async ValueTask DisposeAsync()
            {
                await _semaphore.WaitAsync();
                try
                {
                    _node.List?.Remove(_node);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
    }
}
