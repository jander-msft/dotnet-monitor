// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Exceptions
{
    internal sealed class ThresholdExceptionsStoreCallback
        : ExceptionsStoreCallbackBase
    {
        private readonly TaskCompletionSource _addCompletionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly int _addThreshold;

        private readonly TaskCompletionSource _removeCompletionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly int _removeThreshold;

        private int _addCount;
        private int _removeCount;

        public ThresholdExceptionsStoreCallback(int addThreshold, int removeThreshold = 0)
        {
            _addThreshold = addThreshold;
            _removeThreshold = removeThreshold;

            SetIfAtTarget(_addCompletionSource, 0, addThreshold);
            SetIfAtTarget(_removeCompletionSource, 0, removeThreshold);
        }

        public override void AfterAdd(IExceptionInstance instance)
        {
            SetIfAtTarget(_addCompletionSource, ++_addCount, _addThreshold);
        }

        public override void AfterRemove(IExceptionInstance instance)
        {
            SetIfAtTarget(_removeCompletionSource, ++_removeCount, _removeThreshold);
        }

        public Task WaitForThresholdsAsync(CancellationToken cancellationToken) =>
            Task.WhenAll(
                _addCompletionSource.Task,
                _removeCompletionSource.Task)
            .WaitAsync(cancellationToken);

        public Task WaitForThresholdsAsync(TimeSpan timeout) =>
            Task.WhenAll(
                _addCompletionSource.Task,
                _removeCompletionSource.Task)
            .WaitAsync(timeout);

        private static void SetIfAtTarget(TaskCompletionSource source, int value, int target)
        {
            if (value == target)
            {
                source.SetResult();
            }
        }
    }
}
