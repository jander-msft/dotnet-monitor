// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsStoreEventsCallback :
        ExceptionsStoreCallbackBase,
        IExceptionsEvents,
        IExceptionsStoreCallback
    {
        public event EventHandler<IExceptionInstance>? Added;

        public override void AfterAdd(IExceptionInstance instance)
        {
            Added?.Invoke(this, instance);
        }
    }
}
