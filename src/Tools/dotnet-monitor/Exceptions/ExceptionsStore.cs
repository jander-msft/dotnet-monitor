// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    public sealed class ExceptionsStore : IExceptionsStore, IAsyncDisposable
    {
        private readonly Channel<ExceptionInstanceEntry> _channel =
            Channel.CreateBounded<ExceptionInstanceEntry>(1000);
        private readonly CancellationTokenSource _disposalSource = new();
        private readonly List<ExceptionInstance> _instances = new();
        private readonly Task _processingTask;

        private long _disposalState;

        public ExceptionsStore()
        {
            _processingTask = ProcessEntriesAsync(_disposalSource.Token);
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposalState))
                return;

            _disposalSource.SafeCancel();

            await _processingTask.SafeAwait();

            _disposalSource.Dispose();
        }

        void IExceptionsStore.AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, string message)
        {
            ExceptionInstanceEntry entry = new(cache, exceptionId, message);
            _channel.Writer.TryWrite(entry);
        }

        public IEnumerable<IExceptionInstance> GetSnapshot()
        {
            lock (_instances)
            {
                return new List<ExceptionInstance>(_instances).AsReadOnly();
            }
        }

        private async Task ProcessEntriesAsync(CancellationToken token)
        {
            StringBuilder _builder = new();
            Dictionary<ulong, string> _exceptionTypeNameMap = new();

            bool shouldReadEntry = await _channel.Reader.WaitToReadAsync(token);
            while (shouldReadEntry)
            {
                ExceptionInstanceEntry entry = await _channel.Reader.ReadAsync(token);

                if (entry.Cache.TryGetExceptionId(entry.ExceptionId, out ulong exceptionClassId, out _, out _))
                {
                    string exceptionTypeName;
                    if (!_exceptionTypeNameMap.TryGetValue(exceptionClassId, out exceptionTypeName))
                    {
                        _builder.Clear();
                        NameFormatter.BuildClassName(_builder, entry.Cache.NameCache, exceptionClassId);
                        exceptionTypeName = _builder.ToString();
                    }

                    lock (_instances)
                    {
                        _instances.Add(new ExceptionInstance(exceptionTypeName, entry.Message));
                    }
                }

                shouldReadEntry = await _channel.Reader.WaitToReadAsync(token);
            }
        }

        private sealed class ExceptionInstanceEntry
        {
            public ExceptionInstanceEntry(IExceptionsNameCache cache, ulong exceptionId, string message)
            {
                Cache = cache;
                ExceptionId = exceptionId;
                Message = message;
            }

            public IExceptionsNameCache Cache { get; }

            public ulong ExceptionId { get; }

            public string Message { get; }
        }
    }
}
