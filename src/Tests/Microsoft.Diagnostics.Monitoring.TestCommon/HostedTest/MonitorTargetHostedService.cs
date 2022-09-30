// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.HostedTest;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class MonitorTargetHostedService :
        IHostedService
    {
        private readonly ExecutionStrategy _strategy;

        public MonitorTargetHostedService(IEnumerable<IMonitorTargetRunner> runners)
        {
            if (runners.Count() > 1)
            {
                _strategy = new MultipleTargetStrategy(runners);
            }
            else
            {
                _strategy = new SingleTargetStrategy(runners.Single());
            }
        }

        public Task ExecuteAsync(Func<IMonitorTarget, CancellationToken, Task> callback, CancellationToken cancellationToken)
        {
            return _strategy.ExecuteAsync(callback, cancellationToken);
        }

        public Task SendCommandAsync(string name, CancellationToken cancellationToken)
        {
            return _strategy.SendCommandAsync(name, cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _strategy.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _strategy.StopAsync(cancellationToken);
        }

        private abstract class ExecutionStrategy
        {
            public abstract Task ExecuteAsync(Func<IMonitorTarget, CancellationToken, Task> callback, CancellationToken cancellationToken);

            public abstract Task SendCommandAsync(string name, CancellationToken cancellationToken);

            public abstract Task StartAsync(CancellationToken cancellationToken);

            public abstract Task StopAsync(CancellationToken cancellationToken);
        }

        private sealed class SingleTargetStrategy : ExecutionStrategy
        {
            private readonly IMonitorTargetRunner _runner;

            private IMonitorTarget _target;

            public SingleTargetStrategy(IMonitorTargetRunner runner)
            {
                _runner = runner;
            }

            public override Task ExecuteAsync(Func<IMonitorTarget, CancellationToken, Task> callback, CancellationToken cancellationToken)
            {
                return callback(_target, cancellationToken);
            }

            public override Task SendCommandAsync(string name, CancellationToken cancellationToken)
            {
                return _target.SendCommandAsync(name, cancellationToken);
            }

            public override async Task StartAsync(CancellationToken cancellationToken)
            {
                _target = await _runner.StartAsync(cancellationToken);

                await SendCommandAsync(TestAppScenarios.Commands.StartScenario, cancellationToken);
            }

            public override async Task StopAsync(CancellationToken cancellationToken)
            {
                await SendCommandAsync(TestAppScenarios.Commands.EndScenario, cancellationToken);

                await _runner.StopAsync(cancellationToken);
            }
        }

        private sealed class MultipleTargetStrategy : ExecutionStrategy
        {
            private readonly IEnumerable<IMonitorTargetRunner> _allRunners;

            private List<IMonitorTarget> _targets = new();

            public MultipleTargetStrategy(IEnumerable<IMonitorTargetRunner> runners)
            {
                _allRunners = runners;
            }

            public override async Task ExecuteAsync(Func<IMonitorTarget, CancellationToken, Task> callback, CancellationToken cancellationToken)
            {
                foreach (IMonitorTarget target in _targets)
                {
                    await callback(target, cancellationToken);
                }
            }

            public override Task SendCommandAsync(string name, CancellationToken cancellationToken)
            {
                return ExecuteParallelAsync(
                    _targets,
                    (target, token) => target.SendCommandAsync(name, token),
                    cancellationToken);
            }

            public override async Task StartAsync(CancellationToken cancellationToken)
            {
                _targets.AddRange(await ExecuteParallelAsync(
                    _allRunners,
                    async (runner, token) =>
                    {
                        IMonitorTarget target = await runner.StartAsync(token);

                        await target.SendCommandAsync(TestAppScenarios.Commands.StartScenario, token);

                        return target;
                    },
                    cancellationToken));
            }

            public override async Task StopAsync(CancellationToken cancellationToken)
            {
                await SendCommandAsync(TestAppScenarios.Commands.EndScenario, cancellationToken);

                await ExecuteParallelAsync(
                    _allRunners,
                    (runner, token) => runner.StopAsync(token),
                    cancellationToken);
            }

            private static async Task<IEnumerable<TResult>> ExecuteParallelAsync<T, TResult>(IEnumerable<T> items, Func<T, CancellationToken, Task<TResult>> callback, CancellationToken cancellationToken)
            {
                List<Task<TResult>> tasks = new();

                foreach (T item in items)
                {
                    tasks.Add(Task.Run(() => callback(item, cancellationToken), cancellationToken));
                }

                await Task.WhenAll(tasks);

                List<TResult> results = new();
                foreach (Task<TResult> task in tasks)
                {
                    results.Add(task.Result);
                }

                return results.AsReadOnly();
            }

            private static async Task ExecuteParallelAsync<T>(IEnumerable<T> items, Func<T, CancellationToken, Task> callback, CancellationToken cancellationToken)
            {
                List<Task> tasks = new();

                foreach (T item in items)
                {
                    tasks.Add(Task.Run(() => callback(item, cancellationToken), cancellationToken));
                }

                await Task.WhenAll(tasks);
            }
        }
    }
}
