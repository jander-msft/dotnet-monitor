// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions.Operations
{
    internal sealed class LiveExceptionsOperation :
        BaseExceptionsOperation
    {
        private readonly ArtifactOperationService _artifactOperationService;
        private readonly ConfiguredExceptionsStoreCallback _callback;
        private readonly TaskCompletionSource _completedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ChannelReader<IExceptionInstance> _reader;
        private readonly ChannelWriter<IExceptionInstance> _writer;

        public LiveExceptionsOperation(ArtifactOperationService artifactOperationService, IEndpointInfo endpointInfo, ConfiguredExceptionsStoreCallback callback, ExceptionFormat format)
            : base(endpointInfo, format)
        {
            _artifactOperationService = artifactOperationService;
            _callback = callback;

            Channel<IExceptionInstance> channel = Channel.CreateBounded<IExceptionInstance>(1000);
            _reader = channel.Reader;
            _writer = channel.Writer;
        }

        public override async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            await using IAsyncDisposable _ = await _artifactOperationService.RegisterAsync(this, token);

            EventHandler<IExceptionInstance> addHandler = (s, e) =>
            {
                _writer.TryWrite(e);
            };

            ulong? unhandledId = null;
            EventHandler<ulong> unhandledHandler = (s, e) =>
            {
                unhandledId = e;
            };

            _callback.ExceptionAdded += addHandler;
            _callback.UnhandledException += unhandledHandler;

            startCompletionSource?.TrySetResult(null);

            try
            {
                Dictionary<ulong, IExceptionInstance> priorInstances = new();
                while (await _reader.WaitToReadAsync(token))
                {
                    IExceptionInstance instance = await _reader.ReadAsync(token);

                    switch (Format)
                    {
                        case ExceptionFormat.JsonSequence:
                        case ExceptionFormat.NewlineDelimitedJson:
                            await WriteJsonInstance(outputStream, instance, false, token);
                            break;
                        case ExceptionFormat.PlainText:
                            await WriteTextInstance(outputStream, instance, priorInstances, false, token);
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    priorInstances.Add(instance.Id, instance);
                }

                if (unhandledId.HasValue && priorInstances.TryGetValue(unhandledId.Value, out IExceptionInstance unhandledInstance))
                {
                    switch (Format)
                    {
                        case ExceptionFormat.JsonSequence:
                        case ExceptionFormat.NewlineDelimitedJson:
                            await WriteJsonInstance(outputStream, unhandledInstance, true, token);
                            break;
                        case ExceptionFormat.PlainText:
                            await WriteTextInstance(outputStream, unhandledInstance, priorInstances, true, token);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
            catch (Exception ex) when (SignalFaultNoCatch(ex, _completedSource))
            {
            }
            finally
            {
                _completedSource.TrySetResult();

                _callback.UnhandledException -= unhandledHandler;
                _callback.ExceptionAdded -= addHandler;
            }
        }

        public override async Task StopAsync(CancellationToken token)
        {
            _writer.TryComplete();

            await _completedSource.Task.SafeAwait();
        }

        private static bool SignalFaultNoCatch(Exception exception, TaskCompletionSource source)
        {
            source.TrySetException(exception);
            return false;
        }
    }
}
