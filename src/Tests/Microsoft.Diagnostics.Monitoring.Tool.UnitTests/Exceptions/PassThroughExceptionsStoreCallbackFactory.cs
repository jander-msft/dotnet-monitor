// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Exceptions
{
    internal sealed class PassThroughExceptionsStoreCallbackFactory :
        IExceptionsStoreCallbackFactory
    {
        private readonly IExceptionsStoreCallback _callback;

        public PassThroughExceptionsStoreCallbackFactory(IExceptionsStoreCallback callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public IExceptionsStoreCallback Create(IExceptionsStore store)
        {
            return _callback;
        }

        public static IEnumerable<IExceptionsStoreCallbackFactory> CreateFactories(params IExceptionsStoreCallback[] callbacks)
        {
            foreach (IExceptionsStoreCallback callback in callbacks)
            {
                yield return new PassThroughExceptionsStoreCallbackFactory(callback);
            }
        }
    }
}
