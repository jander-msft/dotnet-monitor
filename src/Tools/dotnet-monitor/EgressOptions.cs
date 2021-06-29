// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !UNITTEST
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
#endif
using System.Collections.Generic;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal sealed class EgressOptions
    {
        public IDictionary<string, AzureBlobEgressProviderOptions> AzureBlobStorage { get; set; }

        public IDictionary<string, FileSystemEgressProviderOptions> FileSystem { get; set; }

        public IDictionary<string, string> Properties { get; set; }
    }
}
