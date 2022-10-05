// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.TestBuilder
{
    internal interface ITestBuilderContext
    {
        Assembly TestAssembly { get; }
    }
}
