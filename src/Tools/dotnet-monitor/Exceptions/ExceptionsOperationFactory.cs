// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    public sealed class ExceptionsOperationFactory : IExceptionsOperationFactory
    {
        IArtifactOperation IExceptionsOperationFactory.Create(IExceptionsStore store)
        {
            return new ExceptionsOperation(store);
        }

        private sealed class ExceptionsOperation : IArtifactOperation
        {
            private readonly IExceptionsStore _store;

            public ExceptionsOperation(IExceptionsStore store)
            {
                _store = store;
            }

            public string ContentType => ContentTypes.ApplicationJson;

            public bool IsStoppable => false;

            public async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
            {
                startCompletionSource?.TrySetResult(null);

                IEnumerable<IExceptionInstance> exceptions = _store.GetSnapshot();

                await using Utf8JsonWriter writer = new(outputStream);
                writer.WriteStartArray();
                foreach (IExceptionInstance instance in exceptions)
                {
                    writer.WriteStartObject();
                    writer.WriteString("typeName", instance.TypeName);
                    writer.WriteString("message", instance.Message);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }

            public string GenerateFileName()
            {
                throw new NotSupportedException();
            }

            public Task StopAsync(CancellationToken token)
            {
                throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
            }
        }
    }
}
