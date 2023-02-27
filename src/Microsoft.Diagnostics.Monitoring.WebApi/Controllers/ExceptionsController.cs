// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    [Route("")]
    [ApiController]
    [ProducesErrorResponseType(typeof(ValidationProblemDetails))]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public sealed class ExceptionsController : ControllerBase
    {
        private readonly IExceptionsStore _exceptionsStore;
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly IExceptionsOperationFactory _operationFactory;

        public ExceptionsController(IServiceProvider serviceProvider)
        {
            _exceptionsStore = serviceProvider.GetRequiredService<IExceptionsStore>();
            _inProcessFeatures = serviceProvider.GetRequiredService<IInProcessFeatures>();
            _operationFactory = serviceProvider.GetRequiredService<IExceptionsOperationFactory>();
        }

        [HttpGet("exceptions", Name = nameof(GetExceptions))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [EgressValidation]
        public ActionResult GetExceptions()
        {
            if (!_inProcessFeatures.IsExceptionsEnabled)
            {
                return NotFound();
            }

            return new OutputStreamResult(_operationFactory.Create(_exceptionsStore));
        }
    }
}
