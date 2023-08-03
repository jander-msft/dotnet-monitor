// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal sealed class UnhandledExceptionEventsPipelineStep
    {
        private readonly ExceptionsEventSource _eventSource;
        private readonly ExceptionIdSource _idSource;
        private readonly ExceptionPipelineDelegate _next;

        public UnhandledExceptionEventsPipelineStep(ExceptionPipelineDelegate next, ExceptionsEventSource eventSource, ExceptionIdSource idSource)
        {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(eventSource);
            ArgumentNullException.ThrowIfNull(idSource);

            _eventSource = eventSource;
            _idSource = idSource;
            _next = next;
        }

        public void Invoke(Exception exception, ExceptionPipelineExceptionContext context)
        {
            ArgumentNullException.ThrowIfNull(exception);

            // Do not send via the EventSource unless an listener is active.
            if (_eventSource.IsEnabled())
            {
                _eventSource.ExceptionInstanceUnhandled(_idSource.GetId(exception));

                // Wait for signal to allow crash to be unblocked or for a short timeout to occur.
                string? value = null;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (string.IsNullOrEmpty(value) && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));

                    value = Environment.GetEnvironmentVariable("UnblockCrash");
                }
            }

            _next(exception, context);
        }
    }
}
