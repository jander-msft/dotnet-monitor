// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsStreamOperation :
        ExceptionsOperationBase
    {
        private readonly IExceptionsEvents _exceptionEvents;

        public ExceptionsStreamOperation(IEndpointInfo endpointInfo, ExceptionFormat format, ExceptionsConfigurationSettings configuration, IExceptionsEvents exceptionEvents)
            : base(endpointInfo, format, configuration)
        {
            _exceptionEvents = exceptionEvents;
        }

        protected override Task ExecuteCoreAsync(Stream outputStream, CancellationToken token)
        {
            _exceptionEvents.Added += (sender, instance) =>
            {
                WriteInstance(outputStream, instance);
            };
        }
    }
}
