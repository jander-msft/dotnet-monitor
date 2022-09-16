// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipelines;
using System.IO.Pipes;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class PipeStreamDuplexPipe : IDuplexPipe
    {
        public PipeStreamDuplexPipe(PipeStream stream)
        {
            Input = PipeReader.Create(stream);
            Output = PipeWriter.Create(stream);
        }

        public PipeReader Input { get; init; }

        public PipeWriter Output { get; init; }
    }
}
