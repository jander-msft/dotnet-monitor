// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Pipes;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class NamedPipeConnectionContext : ConnectionContext
    {
        public NamedPipeConnectionContext(PipeStream stream)
        {
            ConnectionId = Guid.NewGuid().ToString();
            Features = new FeatureCollection();
            Items = new Dictionary<object, object>();
            Transport = new PipeStreamDuplexPipe(stream);
        }

        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features { get; }

        public override IDictionary<object, object> Items { get; set; }

        public override IDuplexPipe Transport { get; set; }
    }
}
