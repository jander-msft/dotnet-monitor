// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsSnapshotOperation :
        ExceptionsOperationBase
    {
        private readonly IExceptionsStore _store;

        public ExceptionsSnapshotOperation(IEndpointInfo endpointInfo, ExceptionFormat format, ExceptionsConfigurationSettings configuration, IExceptionsStore store)
            : base(endpointInfo, format, configuration)
        {
            _store = store;
        }

        protected override async Task ExecuteCoreAsync(Stream outputStream, CancellationToken token)
        {
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
            foreach (IExceptionInstance instance in FilterExceptions(instances))
            {
                await WriteJsonInstance(stream, instance, token);
            }
        }

        private async Task WriteText(Stream stream, IReadOnlyList<IExceptionInstance> instances, CancellationToken token)
        {
            var filteredInstances = FilterExceptions(instances);

            Dictionary<ulong, IExceptionInstance> priorInstances = new(filteredInstances.Count);

            foreach (IExceptionInstance currentInstance in filteredInstances)
            {
                // Skip writing the exception if it does not have a call stack, which
                // indicates that the exception was not thrown. It is likely to be referenced
                // as an inner exception of a thrown exception.
                if (currentInstance.CallStack?.Frames.Count != 0)
                {
                    await WriteTextInstance(stream, currentInstance, priorInstances, token);
                }
                priorInstances.Add(currentInstance.Id, currentInstance);
            }
        }

        private List<IExceptionInstance> FilterExceptions(IReadOnlyList<IExceptionInstance> instances)
        {
            List<IExceptionInstance> filteredInstances = new List<IExceptionInstance>();
            foreach (IExceptionInstance instance in instances)
            {
                if (FilterException(instance))
                {
                    filteredInstances.Add(instance);
                }
            }

            return filteredInstances;
        }
    }
}
