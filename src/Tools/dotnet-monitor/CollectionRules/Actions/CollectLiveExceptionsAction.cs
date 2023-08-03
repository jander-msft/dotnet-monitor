// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectLiveExceptionsActionFactory : ICollectionRuleActionFactory<CollectLiveExceptionsOptions>
    {
        public ICollectionRuleAction Create(IProcessInfo processInfo, CollectLiveExceptionsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, processInfo.EndpointInfo.ServiceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectLiveExceptionsAction(processInfo, options);
        }
    }

    internal sealed class CollectLiveExceptionsAction :
        CollectionRuleEgressActionBase<CollectLiveExceptionsOptions>
    {
        private readonly IExceptionsOperationFactory _operationFactory;

        public CollectLiveExceptionsAction(IProcessInfo processInfo, CollectLiveExceptionsOptions options)
            : base(processInfo.EndpointInfo.ServiceProvider, processInfo, options)
        {
            _operationFactory = ServiceProvider.GetRequiredService<IExceptionsOperationFactory>();
        }

        protected override EgressOperation CreateArtifactOperation(TaskCompletionSource<object> startCompletionSource, CollectionRuleMetadata collectionRuleMetadata)
        {
            KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Exceptions, EndpointInfo);

            IArtifactOperation operation = _operationFactory.Create(Options.GetFormat(), ExceptionCollectionMode.Live);

            EgressOperation egressOperation = new EgressOperation(
                operation,
                startCompletionSource,
                Options.Egress,
                ProcessInfo,
                scope,
                tags: null,
                collectionRuleMetadata: collectionRuleMetadata);

            return egressOperation;
        }
    }
}
