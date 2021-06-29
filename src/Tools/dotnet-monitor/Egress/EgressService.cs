// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Egress service implementation required by the REST server.
    /// </summary>
    internal class EgressService : IEgressService
    {
        private readonly IDictionary<string, string> _nameMap;
        private readonly IDictionary<string, Type> _optionsMap;
        private readonly IServiceProvider _serviceProvider;

        public EgressService(
            IServiceProvider serviceProvider,
            IEnumerable<IEgressProviderConfiguration> configurations,
            IEnumerable<IEgressProviderDescriptor> descriptors)
        {
            _serviceProvider = serviceProvider;

            _nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (IEgressProviderConfiguration providerConfiguration in configurations)
            {
                foreach (IConfigurationSection optionsSection in providerConfiguration.Configuration.GetChildren())
                {
                    _nameMap.Add(optionsSection.Key, providerConfiguration.ProviderName);
                }
            }

            _optionsMap = descriptors.ToDictionary(
                d => d.ProviderName,
                d => d.OptionsType,
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<CancellationToken, Task<Stream>> action, string fileName, string contentType, IEndpointInfo source, CancellationToken token)
        {
            string value = await GetProvider(providerName).EgressAsync(
                providerName,
                action,
                CreateSettings(source, fileName, contentType),
                token);

            return new EgressResult("name", value);
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<Stream, CancellationToken, Task> action, string fileName, string contentType, IEndpointInfo source, CancellationToken token)
        {
            string value = await GetProvider(providerName).EgressAsync(
                providerName,
                action,
                CreateSettings(source, fileName, contentType),
                token);

            return new EgressResult("name", value);
        }

        private IEgressProviderInternal GetProvider(string providerName)
        {
            if (!_nameMap.TryGetValue(providerName, out string providerType))
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }

            if (!_optionsMap.TryGetValue(providerType, out Type optionsType))
            {
                throw new EgressException($"Could not find {providerType} in options type map.");
            }

            return (IEgressProviderInternal)_serviceProvider.GetRequiredService(
                typeof(IEgressProviderInternal<>).MakeGenericType(optionsType));
        }

        private static EgressArtifactSettings CreateSettings(IEndpointInfo source, string fileName, string contentType)
        {
            EgressArtifactSettings settings = new();
            settings.Name = fileName;
            settings.ContentType = contentType;

            // Activity metadata
            Activity activity = Activity.Current;
            if (null != activity)
            {
                settings.Metadata.Add(
                    ActivityMetadataNames.ParentId,
                    activity.GetParentId());
                settings.Metadata.Add(
                    ActivityMetadataNames.SpanId,
                    activity.GetSpanId());
                settings.Metadata.Add(
                    ActivityMetadataNames.TraceId,
                    activity.GetTraceId());
            }

            // Artifact metadata
            settings.Metadata.Add(
                ArtifactMetadataNames.ArtifactSource.ProcessId,
                source.ProcessId.ToString(CultureInfo.InvariantCulture));
            settings.Metadata.Add(
                ArtifactMetadataNames.ArtifactSource.RuntimeInstanceCookie,
                source.RuntimeInstanceCookie.ToString("N"));

            return settings;
        }
    }
}
