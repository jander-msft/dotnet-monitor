// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    [Route("")]
    [ApiController]
    [HostRestriction]
    [Authorize(Policy = AuthConstants.PolicyName)]
    [ProducesErrorResponseType(typeof(ValidationProblemDetails))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public sealed class ExceptionsController :
        DiagnosticsControllerBase
    {
        private readonly IOptions<ExceptionsOptions> _options;

        public ExceptionsController(
            IServiceProvider serviceProvider,
            ILogger<ExceptionsController> logger)
            : base(serviceProvider, logger)
        {
            _options = serviceProvider.GetRequiredService<IOptions<ExceptionsOptions>>();
        }

        /// <summary>
        /// Gets the exceptions from the target process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the exceptions are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        [HttpGet("exceptions", Name = nameof(GetExceptions))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> GetExceptions(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
        {
            return GetExceptionsCore(pid, uid, name, egressProvider, tags, durationSeconds: null);
        }

        /// <summary>
        /// Gets the exceptions from the target process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the exceptions are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        /// <param name="configuration">The exceptions configuration describing which exceptions to include in the response.</param>
        [HttpPost("exceptions", Name = nameof(CaptureExceptionsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [EgressValidation]
        public Task<ActionResult> CaptureExceptionsCustom(
            [FromBody]
            ExceptionsConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
        {
            return CaptureExceptionsCustomCore(configuration, pid, uid, name, egressProvider, tags, durationSeconds: null);
        }

        /// <summary>
        /// Gets the exceptions from the target process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the exceptions are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        /// <param name="durationSeconds">The duration of the exceptions session (in seconds).</param>
        [HttpGet("liveexceptions", Name = nameof(CaptureLiveExceptions))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureLiveExceptions(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int? durationSeconds = 30)
        {
            return GetExceptionsCore(pid, uid, name, egressProvider, tags, durationSeconds);
        }

        /// <summary>
        /// Gets the exceptions from the target process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the exceptions are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        /// <param name="configuration">The exceptions configuration describing which exceptions to include in the response.</param>
        /// <param name="durationSeconds">The duration of the exceptions session (in seconds).</param>
        [HttpPost("liveexceptions", Name = nameof(CaptureLiveExceptionsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [EgressValidation]
        public Task<ActionResult> CaptureLiveExceptionsCustom(
            [FromBody]
            ExceptionsConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int? durationSeconds = 30)
        {
            return CaptureExceptionsCustomCore(configuration, pid, uid, name, egressProvider, tags, durationSeconds);
        }

        private Task<ActionResult> GetExceptionsCore(
            int? pid,
            Guid? uid,
            string? name,
            string? egressProvider,
            string? tags,
            int? durationSeconds)
        {
            if (!_options.Value.GetEnabled())
            {
                return Task.FromResult(this.FeatureNotEnabled(Strings.FeatureName_Exceptions));
            }

            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                ExceptionFormat format = ComputeFormat(Request.GetTypedHeaders().Accept) ?? ExceptionFormat.PlainText;
                TimeSpan? duration = durationSeconds.HasValue ? Utilities.ConvertSecondsToTimeSpan(durationSeconds.Value) : null;

                IArtifactOperation operation = processInfo.EndpointInfo.ServiceProvider
                    .GetRequiredService<IExceptionsOperationFactory>()
                    .Create(format, new ExceptionsConfigurationSettings(), duration);

                return Result(
                    Utilities.ArtifactType_Exceptions,
                    egressProvider,
                    operation,
                    processInfo,
                    tags,
                    format != ExceptionFormat.PlainText);
            }, processKey, Utilities.ArtifactType_Exceptions);
        }

        public Task<ActionResult> CaptureExceptionsCustomCore(
            ExceptionsConfiguration configuration,
            int? pid,
            Guid? uid,
            string? name,
            string? egressProvider,
            string? tags,
            int? durationSeconds)
        {
            if (!_options.Value.GetEnabled())
            {
                return Task.FromResult<ActionResult>(NotFound());
            }
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                ExceptionFormat format = ComputeFormat(Request.GetTypedHeaders().Accept) ?? ExceptionFormat.PlainText;
                TimeSpan? duration = durationSeconds.HasValue ? Utilities.ConvertSecondsToTimeSpan(durationSeconds.Value) : null;

                IArtifactOperation operation = processInfo.EndpointInfo.ServiceProvider
                    .GetRequiredService<IExceptionsOperationFactory>()
                    .Create(format, ExceptionsSettingsFactory.ConvertExceptionsConfiguration(configuration), duration);

                return Result(
                    Utilities.ArtifactType_Exceptions,
                    egressProvider,
                    operation,
                    processInfo,
                    tags,
                    format != ExceptionFormat.PlainText);
            }, processKey, Utilities.ArtifactType_Exceptions);
        }

        private static ExceptionFormat? ComputeFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            if (acceptedHeaders == null || acceptedHeaders.Count == 0)
            {
                return null;
            }

            if (acceptedHeaders.Contains(ContentTypeUtilities.TextPlainHeader))
            {
                return ExceptionFormat.PlainText;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.NdJsonHeader))
            {
                return ExceptionFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.JsonSequenceHeader))
            {
                return ExceptionFormat.JsonSequence;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.TextPlainHeader.IsSubsetOf))
            {
                return ExceptionFormat.PlainText;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.NdJsonHeader.IsSubsetOf))
            {
                return ExceptionFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.JsonSequenceHeader.IsSubsetOf))
            {
                return ExceptionFormat.JsonSequence;
            }
            return null;
        }
    }
}
