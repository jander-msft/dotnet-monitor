// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class EventExceptionsPipeline : EventSourcePipeline<EventExceptionsPipelineSettings>
    {
        private readonly EventExceptionsPipelineNameCache _cache = new();
        private readonly IExceptionsStore _store;

        public EventExceptionsPipeline(IpcEndpoint endpoint, EventExceptionsPipelineSettings settings, IExceptionsStore store)
            : this(new DiagnosticsClient(endpoint), settings, store)
        {
        }

        public EventExceptionsPipeline(DiagnosticsClient client, EventExceptionsPipelineSettings settings, IExceptionsStore store)
            : base(client, settings)
        {
            ArgumentNullException.ThrowIfNull(store, nameof(store));

            _store = store;
        }

        protected override MonitoringSourceConfiguration CreateConfiguration()
        {
            return new EventPipeProviderSourceConfiguration(rundownKeyword: 0, bufferSizeInMB: 64, new[]
            {
                new EventPipeProvider(ExceptionsEvents.SourceName, EventLevel.Informational, (long)EventKeywords.All)
            });
        }

        protected override Task OnEventSourceAvailable(EventPipeEventSource eventSource, Func<Task> stopSessionAsync, CancellationToken token)
        {
            eventSource.Dynamic.AddCallbackForProviderEvent(
                ExceptionsEvents.SourceName,
                null,
                Callback);

            return Task.CompletedTask;
        }

        private void Callback(TraceEvent traceEvent)
        {
            // Using event name instead of event ID because event ID seem to be dynamically assigned
            // in the order in which they are used.
            switch (traceEvent.EventName)
            {
                case "ClassDescription":
                    _cache.AddClass(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ClassId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.ClassDescPayloads.Token),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ModuleId),
                        traceEvent.GetPayload<ClassFlags>(NameIdentificationEvents.ClassDescPayloads.Flags),
                        traceEvent.GetPayload<ulong[]>(NameIdentificationEvents.ClassDescPayloads.TypeArgs)
                        );
                    break;
                case "ExceptionGroup":
                    _cache.AddExceptionGroup(
                        traceEvent.GetPayload<ulong>(ExceptionsEvents.ExceptionGroupPayloads.ExceptionGroupId),
                        traceEvent.GetPayload<ulong>(ExceptionsEvents.ExceptionGroupPayloads.ExceptionClassId),
                        traceEvent.GetPayload<ulong>(ExceptionsEvents.ExceptionGroupPayloads.ThrowingMethodId),
                        traceEvent.GetPayload<int>(ExceptionsEvents.ExceptionGroupPayloads.ILOffset)
                        );
                    break;
                case "ExceptionInstance":
                    _store.AddExceptionInstance(
                        _cache,
                        traceEvent.GetPayload<ulong>(ExceptionsEvents.ExceptionInstancePayloads.ExceptionId),
                        traceEvent.GetPayload<ulong>(ExceptionsEvents.ExceptionInstancePayloads.ExceptionGroupId),
                        traceEvent.GetPayload<string>(ExceptionsEvents.ExceptionInstancePayloads.ExceptionMessage),
                        traceEvent.GetPayload<DateTime>(ExceptionsEvents.ExceptionInstancePayloads.Timestamp).ToUniversalTime(),
                        traceEvent.GetPayload<ulong[]>(ExceptionsEvents.ExceptionInstancePayloads.StackFrameIds),
                        traceEvent.ThreadID,
                        traceEvent.GetPayload<ulong[]>(ExceptionsEvents.ExceptionInstancePayloads.InnerExceptionIds),
                        traceEvent.GetPayload<string>(ExceptionsEvents.ExceptionInstancePayloads.ActivityId),
                        traceEvent.GetPayload<ActivityIdFormat>(ExceptionsEvents.ExceptionInstancePayloads.ActivityIdFormat));
                    break;
                case "ExceptionInstanceUnhandled":
                    // TODO: Advertise unhandled exception as necessary
                    ulong exceptionId = traceEvent.GetPayload<ulong>(ExceptionsEvents.ExceptionInstanceUnhandledPayloads.ExceptionId);
                    break;
                case "FunctionDescription":
                    _cache.AddFunction(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.FunctionId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.FunctionDescPayloads.MethodToken),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ClassId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.FunctionDescPayloads.ClassToken),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ModuleId),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.FunctionDescPayloads.Name),
                        traceEvent.GetPayload<ulong[]>(NameIdentificationEvents.FunctionDescPayloads.TypeArgs),
                        traceEvent.GetPayload<ulong[]>(NameIdentificationEvents.FunctionDescPayloads.ParameterTypes)
                        );
                    break;
                case "ModuleDescription":
                    _cache.AddModule(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ModuleDescPayloads.ModuleId),
                        traceEvent.GetPayload<Guid>(NameIdentificationEvents.ModuleDescPayloads.ModuleVersionId),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.ModuleDescPayloads.Name)
                        );
                    break;
                case "StackFrameDescription":
                    _cache.AddStackFrame(
                        traceEvent.GetPayload<ulong>(ExceptionsEvents.StackFrameIdentifierPayloads.StackFrameId),
                        traceEvent.GetPayload<ulong>(ExceptionsEvents.StackFrameIdentifierPayloads.FunctionId),
                        traceEvent.GetPayload<int>(ExceptionsEvents.StackFrameIdentifierPayloads.ILOffset)
                        );
                    break;
                case "TokenDescription":
                    _cache.AddToken(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.TokenDescPayloads.ModuleId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.Token),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.OuterToken),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.TokenDescPayloads.Name),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.TokenDescPayloads.Namespace)
                        );
                    break;
                case "Flush":
                    break;
#if DEBUG
                default:
                    throw new NotSupportedException("Unhandled event: " + traceEvent.EventName);
#endif
            }
        }

        public new Task StartAsync(CancellationToken token)
        {
            return base.StartAsync(token);
        }
    }

    internal sealed class EventExceptionsPipelineSettings : EventSourcePipelineSettings
    {
        public EventExceptionsPipelineSettings()
        {
            Duration = Timeout.InfiniteTimeSpan;
        }
    }
}
