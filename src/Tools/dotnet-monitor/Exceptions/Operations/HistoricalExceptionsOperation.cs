// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions.Operations
{
    internal sealed class HistoricalExceptionsOperation :
        BaseExceptionsOperation
    {
        private readonly IExceptionsStore _store;

        public HistoricalExceptionsOperation(IEndpointInfo endpointInfo, IExceptionsStore store, ExceptionFormat format)
            : base(endpointInfo, format)
        {
            _store = store;
        }

        public override async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            startCompletionSource?.TrySetResult(null);

            IReadOnlyList<IExceptionInstance> exceptions = _store.GetSnapshot();

            switch (Format)
            {
                case ExceptionFormat.JsonSequence:
                case ExceptionFormat.NewlineDelimitedJson:
                    await WriteJson(outputStream, exceptions, token);
                    break;
                case ExceptionFormat.PlainText:
                    await WriteText(outputStream, exceptions, token);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private async Task WriteJson(Stream stream, IReadOnlyList<IExceptionInstance> instances, CancellationToken token)
        {
            foreach (IExceptionInstance instance in instances)
            {
                await WriteJsonInstance(stream, instance, false, token);
            }
        }

        private static async Task WriteText(Stream stream, IReadOnlyList<IExceptionInstance> instances, CancellationToken token)
        {
            Dictionary<ulong, IExceptionInstance> priorInstances = new(instances.Count);
            foreach (IExceptionInstance currentInstance in instances)
            {
                // Skip writing the exception if it does not have a call stack, which
                // indicates that the exception was not thrown. It is likely to be referenced
                // as an inner exception of a thrown exception.
                if (currentInstance.CallStack?.Frames.Count != 0)
                {
                    await WriteTextInstance(stream, currentInstance, priorInstances, false, token);
                }
                priorInstances.Add(currentInstance.Id, currentInstance);
            }
        }
    }
}
