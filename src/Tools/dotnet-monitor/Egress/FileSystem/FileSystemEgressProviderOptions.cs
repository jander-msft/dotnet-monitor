// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem
#endif
{
    /// <summary>
    /// Egress provider options for file system egress.
    /// </summary>
    internal class FileSystemEgressProviderOptions
    {
        /// <summary>
        /// The directory path to which the stream data will be egressed.
        /// </summary>
        [Required]
        public string DirectoryPath { get; set; }

        /// <summary>
        /// The directory path to which the stream data will initially be written, if specified; the file will then
        /// be moved/renamed to the directory specified in <see cref="FileSystemEgressProviderOptions.DirectoryPath"/>.
        /// </summary>
        public string IntermediateDirectoryPath { get; set; }

        /// <summary>
        /// Buffer size used when copying data from an egress callback returning a stream
        /// to the egress callback that is provided a stream to which data is written.
        /// </summary>
        public int? CopyBufferSize { get; set; }
    }
}
