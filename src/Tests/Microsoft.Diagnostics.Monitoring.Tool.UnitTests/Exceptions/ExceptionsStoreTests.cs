// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Exceptions
{
    public sealed class ExceptionsStoreTests
    {
        private const ulong DefaultExceptionGroupId = 1;

        /// <summary>
        /// Validates that a <see cref="ExceptionsStore"/> can be instantiated.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_Creation()
        {
            await using ExceptionsStore store = new ExceptionsStore(Array.Empty<IExceptionsStoreCallbackFactory>());
        }

        /// <summary>
        /// Validates that a <see cref="ExceptionsStore"/> will throw with null factories.
        /// </summary>
        [Fact]
        public void ExceptionsStore_NullFactories_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new ExceptionsStore(null));
        }

        /// <summary>
        /// Validates adding a single exception will invoke the callback
        /// and the store will contain that exception.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddInstance_ContainsOne()
        {
            int ExpectedCount = 1;
            ulong ExpectedId = 1;

            // Arrange
            ThresholdExceptionsStoreCallback callback = new(ExpectedCount);
            await using ExceptionsStore store = new(PassThroughExceptionsStoreCallbackFactory.CreateFactories(callback));

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, ExpectedId);

            await callback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            IExceptionInstance instance = Assert.Single(instances);
            Assert.Equal(ExpectedId, instance.Id);
        }

        /// <summary>
        /// Validates adding multiple exceptions will invoke the callback
        /// and the store will contain those exceptions.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddMultipleInstance_ContainsAll()
        {
            int ExpectedCount = 3;
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;
            ulong ExpectedId3 = 3;

            // Arrange
            ThresholdExceptionsStoreCallback callback = new(ExpectedCount);
            await using ExceptionsStore store = new(PassThroughExceptionsStoreCallbackFactory.CreateFactories(callback));

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, ExpectedId1);
            AddExceptionInstance(store, cache, ExpectedId2);
            AddExceptionInstance(store, cache, ExpectedId3);

            await callback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            Assert.Equal(ExpectedCount, instances.Count);

            IExceptionInstance instance1 = instances[0];
            Assert.NotNull(instance1);
            Assert.Equal(ExpectedId1, instance1.Id);

            IExceptionInstance instance2 = instances[1];
            Assert.NotNull(instance2);
            Assert.Equal(ExpectedId2, instance2.Id);

            IExceptionInstance instance3 = instances[2];
            Assert.NotNull(instance3);
            Assert.Equal(ExpectedId3, instance3.Id);
        }

        /// <summary>
        /// Validates adding and removing exceptions will invoke the callback
        /// and the store will contain the remaining exceptions.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddTwoRemoveOne_ContainsOne()
        {
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;

            // Arrange
            AggregateCallback aggregateCallback = new();
            await using ExceptionsStore store = new(PassThroughExceptionsStoreCallbackFactory.CreateFactories(aggregateCallback));

            IExceptionsNameCache cache = CreateCache();

            // Act
            ThresholdExceptionsStoreCallback thresholdCallback1 = new(2, 0);
            using (aggregateCallback.RegisterCallback(thresholdCallback1))
            {
                AddExceptionInstance(store, cache, ExpectedId1);
                AddExceptionInstance(store, cache, ExpectedId2);

                await thresholdCallback1.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdExceptionsStoreCallback thresholdCallback2 = new(0, 1);
            using (aggregateCallback.RegisterCallback(thresholdCallback2))
            {
                RemoveExceptionInstance(store, ExpectedId2);

                await thresholdCallback2.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            // Assert
            IExceptionInstance instance = Assert.Single(store.GetSnapshot());
            Assert.NotNull(instance);
            Assert.Equal(ExpectedId1, instance.Id);
        }

        /// <summary>
        /// Validates adding exceptions and removing all exceptions
        /// will invoke the callback and the store will empty.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddThreeRemoveThree_Empty()
        {
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;
            ulong ExpectedId3 = 3;

            // Arrange
            AggregateCallback aggregateCallback = new();
            await using ExceptionsStore store = new(PassThroughExceptionsStoreCallbackFactory.CreateFactories(aggregateCallback));

            IExceptionsNameCache cache = CreateCache();

            // Act
            ThresholdExceptionsStoreCallback addThreeCallback = new(3, 0);
            using (aggregateCallback.RegisterCallback(addThreeCallback))
            {
                AddExceptionInstance(store, cache, ExpectedId1);
                AddExceptionInstance(store, cache, ExpectedId2);
                AddExceptionInstance(store, cache, ExpectedId3);

                await addThreeCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdExceptionsStoreCallback removeThreeCallback = new(0, 3);
            using (aggregateCallback.RegisterCallback(removeThreeCallback))
            {
                RemoveExceptionInstance(store, ExpectedId1);
                RemoveExceptionInstance(store, ExpectedId2);
                RemoveExceptionInstance(store, ExpectedId3);

                await removeThreeCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            // Assert
            Assert.Empty(store.GetSnapshot());
        }

        /// <summary>
        /// Validates adding exceptions and removing all exceptions in an unordered manner
        /// will invoke the callback and the store will be empty.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddThreeRemoveThreeOutOfOrder_Empty()
        {
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;
            ulong ExpectedId3 = 3;

            // Arrange
            AggregateCallback aggregateCallback = new();
            await using ExceptionsStore store = new(PassThroughExceptionsStoreCallbackFactory.CreateFactories(aggregateCallback));

            IExceptionsNameCache cache = CreateCache();

            // Act
            ThresholdExceptionsStoreCallback addTwoCallback = new(2, 0);
            using (aggregateCallback.RegisterCallback(addTwoCallback))
            {
                AddExceptionInstance(store, cache, ExpectedId1);
                AddExceptionInstance(store, cache, ExpectedId2);

                await addTwoCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdExceptionsStoreCallback removeOneCallback = new(0, 1);
            using (aggregateCallback.RegisterCallback(removeOneCallback))
            {
                RemoveExceptionInstance(store, ExpectedId1);

                await removeOneCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdExceptionsStoreCallback addOneCallback = new(1, 0);
            using (aggregateCallback.RegisterCallback(addOneCallback))
            {
                AddExceptionInstance(store, cache, ExpectedId3);

                await addOneCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdExceptionsStoreCallback removeTwoCallback = new(0, 2);
            using (aggregateCallback.RegisterCallback(removeTwoCallback))
            {
                RemoveExceptionInstance(store, ExpectedId2);
                RemoveExceptionInstance(store, ExpectedId3);

                await removeTwoCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            // Assert
            Assert.Empty(store.GetSnapshot());
        }

        private static IExceptionsNameCache CreateCache()
        {
            EventExceptionsPipelineNameCache cache = new();
            cache.AddExceptionGroup(DefaultExceptionGroupId, 1, 1, 0);
            return cache;
        }

        private static void AddExceptionInstance(ExceptionsStore store, IExceptionsNameCache cache, ulong exceptionId)
        {
            store.AddExceptionInstance(
                cache,
                exceptionId,
                DefaultExceptionGroupId,
                null,
                DateTime.UtcNow,
                Array.Empty<ulong>(),
                0,
                Array.Empty<ulong>(),
                null,
                ActivityIdFormat.Unknown);
        }

        private static void RemoveExceptionInstance(ExceptionsStore store, ulong exceptionId)
        {
            store.RemoveExceptionInstance(exceptionId);
        }

        private sealed class AggregateCallback : ExceptionsStoreCallbackBase
        {
            private readonly List<IExceptionsStoreCallback> _callbacks = new();

            public override void AfterAdd(IExceptionInstance instance)
            {
                foreach (IExceptionsStoreCallback callback in _callbacks)
                    callback.AfterAdd(instance);
            }

            public override void AfterRemove(IExceptionInstance instance)
            {
                foreach (IExceptionsStoreCallback callback in _callbacks)
                    callback.AfterRemove(instance);
            }

            public override void BeforeAdd(IExceptionInstance instance)
            {
                foreach (IExceptionsStoreCallback callback in _callbacks)
                    callback.BeforeAdd(instance);
            }

            public IDisposable RegisterCallback(IExceptionsStoreCallback callback)
            {
                _callbacks.Add(callback);

                return new Registration(this, callback);
            }

            private sealed class Registration : IDisposable
            {
                private readonly AggregateCallback _aggregateCallback;
                private readonly IExceptionsStoreCallback _callback;

                public Registration(AggregateCallback aggregateCallback, IExceptionsStoreCallback callback)
                {
                    _aggregateCallback = aggregateCallback ?? throw new ArgumentNullException(nameof(aggregateCallback));
                    _callback = callback ?? throw new ArgumentNullException(nameof(callback));
                }

                public void Dispose()
                {
                    _aggregateCallback._callbacks.Remove(_callback);
                }
            }
        }
    }
}
