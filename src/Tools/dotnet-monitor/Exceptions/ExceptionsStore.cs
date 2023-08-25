// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CallStackModel = Microsoft.Diagnostics.Monitoring.WebApi.Models.CallStack;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsStore : IExceptionsStore, IAsyncDisposable
    {
        private const int ChannelCapacity = 1000;

        private readonly IReadOnlyList<IExceptionsStoreCallback> _callbacks;
        private readonly Channel<ExceptionEntry> _channel;
        private readonly CancellationTokenSource _disposalSource = new();
        private readonly KeyedCollection<ulong, ExceptionInstance> _instances = new ExceptionInstanceCollection();
        private readonly Task _processingTask;

        private readonly StringBuilder _exceptionTypeNameBuilder = new();
        private readonly Dictionary<ulong, string> _exceptionTypeNameMap = new();

        private long _disposalState;

        public ExceptionsStore(IEnumerable<IExceptionsStoreCallbackFactory> factories)
        {
            ArgumentNullException.ThrowIfNull(factories);

            _channel = CreateChannel();
            _processingTask = ProcessEntriesAsync(_disposalSource.Token);

            List<IExceptionsStoreCallback> callbacks = new(factories.Count());
            foreach (IExceptionsStoreCallbackFactory factory in factories)
            {
                callbacks.Add(factory.Create(this));
            }
            _callbacks = callbacks;
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposalState))
                return;

            _channel.Writer.TryComplete();

            await _processingTask.SafeAwait();

            _disposalSource.SafeCancel();

            _disposalSource.Dispose();
        }

        public void AddExceptionInstance(
            IExceptionsNameCache cache,
            ulong exceptionId,
            ulong groupId,
            string message,
            DateTime timestamp,
            ulong[] stackFrameIds,
            int threadId,
            ulong[] innerExceptionIds,
            string activityId,
            ActivityIdFormat activityIdFormat)
        {
            ExceptionInstanceEntry entry = new(cache, exceptionId, groupId, message, timestamp, stackFrameIds, threadId, innerExceptionIds, activityId, activityIdFormat);
            // This should never fail to write because the behavior is to drop the oldest.
            _channel.Writer.TryWrite(entry);
        }

        public void RemoveExceptionInstance(ulong exceptionId)
        {
            ExceptionInstance removedInstance = null;

            lock (_instances)
            {
                if (_instances.TryGetValue(exceptionId, out removedInstance))
                {
                    _instances.Remove(exceptionId);
                }
            }

            if (null != removedInstance)
            {
                for (int i = 0; i < _callbacks.Count; i++)
                {
                    _callbacks[i].AfterRemove(removedInstance);
                }
            }
        }

        public void UnhandledException(ulong exceptionId)
        {

        }

        public IReadOnlyList<IExceptionInstance> GetSnapshot()
        {
            lock (_instances)
            {
                return new List<ExceptionInstance>(_instances).AsReadOnly();
            }
        }

        private static Channel<ExceptionEntry> CreateChannel()
        {
            // TODO: Hook callback for when items are dropped and report appropriately.
            return Channel.CreateBounded<ExceptionEntry>(
                new BoundedChannelOptions(ChannelCapacity)
                {
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = true
                });
        }

        private async Task ProcessEntriesAsync(CancellationToken token)
        {
            bool shouldReadEntry = await _channel.Reader.WaitToReadAsync(token);
            while (shouldReadEntry)
            {
                ExceptionEntry entry = await _channel.Reader.ReadAsync(token);

                switch (entry)
                {
                    case ExceptionInstanceEntry instanceEntry:
                        HandleExceptionInstance(instanceEntry);
                        break;
                    case ExceptionUnhandledEntry unhandledEntry:
                        HandleExceptionUnhandled(unhandledEntry);
                        break;
                    default:
                        throw new NotSupportedException();
                };

                shouldReadEntry = await _channel.Reader.WaitToReadAsync(token);
            }
        }

        private void HandleExceptionInstance(ExceptionInstanceEntry entry)
        {
            // CONSIDER: If the group ID could not be found, either the identification information was not sent
            // by the EventSource in the target application OR it hasn't been sent yet due to multithreaded collision
            // in the target application where the same exception information is being logged by two or more threads
            // at the same time; one will return sooner and report the correct IDs potentially before those IDs are
            // produced by the EventSource. May need to cache this incomplete information and attempt to reconstruct
            // it in the future, with either periodic retry OR registering a callback system for the missing IDs.
            if (entry.Cache.TryGetExceptionGroup(entry.GroupId, out ulong exceptionClassId, out _, out _))
            {
                string exceptionTypeName;
                if (!_exceptionTypeNameMap.TryGetValue(exceptionClassId, out exceptionTypeName))
                {
                    _exceptionTypeNameBuilder.Clear();
                    NameFormatter.BuildClassName(_exceptionTypeNameBuilder, entry.Cache.NameCache, exceptionClassId);
                    exceptionTypeName = _exceptionTypeNameBuilder.ToString();
                }

                        entry.ActivityId,
                        entry.ActivityIdFormat);

                    for (int i = 0; i < _callbacks.Count; i++)
                    {
                        _callbacks[i].BeforeAdd(instance);
                    }

                    lock (_instances)
                ExceptionInstance instance = new(
                    entry.ExceptionId,
                    exceptionTypeName,
                    moduleName,
                    entry.Message,
                    entry.Timestamp,
                    callStack,
                    entry.InnerExceptionIds,
                    entry.ActivityId,
                    entry.ActivityIdFormat);

                    for (int i = 0; i < _callbacks.Count; i++)
                    {
                        _callbacks[i].AfterAdd(instance);
                    }
                }

                _callback?.AfterAdd(instance);
            }
        }

        private void HandleExceptionUnhandled(ExceptionUnhandledEntry entry)
        {
            // It is highly unlikely that the unhandled exception will not be in the store
            // since unhandled exceptions are detected very shortly after their first chance encounter.
            // Guard against missing it in the list of instances.
            if (_instances.TryGetValue(entry.ExceptionId, out ExceptionInstance instance))
            {
                _callback?.Unhandled(instance);
            }
        }

        internal static CallStackModel GenerateCallStack(ulong[] stackFrameIds, IExceptionsNameCache cache, int threadId)
        {
            CallStack callStack = new();
            callStack.ThreadId = (uint)threadId;

            foreach (var stackFrameId in stackFrameIds)
            {
                if (cache.TryGetStackFrameIds(stackFrameId, out ulong methodId, out int ilOffset))
                {
                    CallStackFrame frame = new()
                    {
                        FunctionId = methodId,
                        Offset = (ulong)ilOffset
                    };

                    callStack.Frames.Add(frame);
                }
            }

            return StackUtilities.TranslateCallStackToModel(callStack, cache.NameCache);
        }

        private abstract class ExceptionEntry
        {
            protected ExceptionEntry(ulong exceptionId)
            {
                ExceptionId = exceptionId;
            }

            public ulong ExceptionId { get; }
        }

        private sealed class ExceptionInstanceEntry :
            ExceptionEntry
        {
            public ExceptionInstanceEntry(
                IExceptionsNameCache cache,
                ulong exceptionId,
                ulong groupId,
                string message,
                DateTime timestamp,
                ulong[] stackFrameIds,
                int threadId,
                ulong[] innerExceptionIds,
                string activityId,
                ActivityIdFormat activityIdFormat)
                : base(exceptionId)
            {
                Cache = cache;
                GroupId = groupId;
                Message = message;
                Timestamp = timestamp;
                StackFrameIds = stackFrameIds;
                ThreadId = threadId;
                InnerExceptionIds = innerExceptionIds;
                ActivityId = activityId;
                ActivityIdFormat = activityIdFormat;
            }

            public IExceptionsNameCache Cache { get; }

            public ulong GroupId { get; }

            public string Message { get; }

            public DateTime Timestamp { get; }

            public ulong[] StackFrameIds { get; }

            public int ThreadId { get; }

            public ulong[] InnerExceptionIds { get; }

            public string ActivityId { get; }

            public ActivityIdFormat ActivityIdFormat { get; }
        }

        private sealed class ExceptionUnhandledEntry :
            ExceptionEntry
        {
            public ExceptionUnhandledEntry(ulong exceptionId)
                : base(exceptionId)
            {
            }
        }

        private sealed class ExceptionInstanceCollection :
            KeyedCollection<ulong, ExceptionInstance>
        {
            protected override ulong GetKeyForItem(ExceptionInstance item)
            {
                return item.Id;
            }
        }
    }
}
