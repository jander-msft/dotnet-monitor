﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter<DumpType>))]
    public enum DumpType
    {
        Full = 1,
        Mini,
        WithHeap,
        Triage
    }
}
