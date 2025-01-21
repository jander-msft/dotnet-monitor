// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit.Sdk;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    [TraitDiscoverer("Microsoft.Diagnostics.Monitoring.TestCommon.TargetFrameworkTraitDiscoverer", "Microsoft.Diagnostics.Monitoring.TestCommon")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class TargetFrameworkTraitAttribute :
        Attribute,
        ITraitAttribute
    {
        public TargetFrameworkTraitAttribute(TargetFramework targetFramework)
        {
        }
    }
}
