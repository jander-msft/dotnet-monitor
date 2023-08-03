// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions.Operations;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsOperationFactory :
        IExceptionsOperationFactory
    {
        private readonly ArtifactOperationService _artifactOperationService;
        private readonly ConfiguredExceptionsStoreCallback _callback;
        private IEndpointInfo _endpointInfo;
        private IExceptionsStore _store;

        public ExceptionsOperationFactory(ArtifactOperationService artifactOperationService, IEndpointInfo endpointInfo, IExceptionsStore store, ConfiguredExceptionsStoreCallback callback)
        {
            _artifactOperationService = artifactOperationService ?? throw new ArgumentNullException(nameof(artifactOperationService));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _endpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IArtifactOperation Create(ExceptionFormat format, ExceptionCollectionMode mode)
        {
            return mode switch
            {
                ExceptionCollectionMode.Historical => new HistoricalExceptionsOperation(_endpointInfo, _store, format),
                ExceptionCollectionMode.Live => new LiveExceptionsOperation(_artifactOperationService, _endpointInfo, _callback, format),
                _ => throw new NotSupportedException()
            };
        }
    }
}
