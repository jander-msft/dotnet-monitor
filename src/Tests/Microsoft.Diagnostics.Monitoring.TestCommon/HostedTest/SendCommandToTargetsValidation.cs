// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class SendCommandToTargetsValidation : ITestValidation
    {
        private readonly MonitorTargetHostedService _monitorTargetService;
        private readonly string _name;

        public SendCommandToTargetsValidation(
            MonitorTargetHostedService monitorTargetService,
            string name)
        {
            _monitorTargetService = monitorTargetService;
            _name = name;
        }

        public Task ValidateAsync(CancellationToken cancellationToken)
        {
            return _monitorTargetService.SendCommandAsync(_name, cancellationToken);
        }
    }
}
