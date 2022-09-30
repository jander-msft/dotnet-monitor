// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class TestBuilderContext
    {
        private int _nextTargetId = 1;

        public TestBuilderContext(Assembly testAssembly)
        {
            TestAssembly = testAssembly;
        }

        public string CreateNextTargetId()
        {
            return FormattableString.Invariant($"Target{_nextTargetId++}");
        }

        public Assembly TestAssembly { get; }
    }
}
