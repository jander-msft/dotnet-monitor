﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsOperationFactory : IExceptionsOperationFactory
    {
        private readonly IExceptionsStore _store;

        public ExceptionsOperationFactory(IExceptionsStore store)
        {
            _store = store;
        }

        public IArtifactOperation Create(ExceptionsFormat format)
        {
            return new ExceptionsOperation(_store, format);
        }
    }
}
