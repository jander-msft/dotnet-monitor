// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsStoreEventsCallbackFactory :
        IExceptionsStoreCallbackFactory
    {
        public IExceptionsStoreCallback Create(IExceptionsStore store)
        {
            return new ExceptionsStoreEventsCallback();
        }
    }
}
