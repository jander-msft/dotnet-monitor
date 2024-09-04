// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;
using NameFormatter = Microsoft.Diagnostics.Monitoring.WebApi.Stacks.NameFormatter;
using System.Text.Json;
using System.Text;
using System.Globalization;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal abstract class ExceptionsOperationBase :
        IArtifactOperation
    {
        private static byte[] JsonRecordDelimiter = new byte[] { (byte)'\n' };

        private static byte[] JsonSequenceRecordSeparator = new byte[] { 0x1E };

        private const char GenericSeparator = ',';
        private const char GenericStart = '[';
        private const char GenericEnd = ']';

        private readonly ExceptionsConfigurationSettings _configuration;
        private readonly IEndpointInfo _endpointInfo;
        private readonly ExceptionFormat _format;

        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected ExceptionsOperationBase(IEndpointInfo endpointInfo, ExceptionFormat format, ExceptionsConfigurationSettings configuration)
        {
            _endpointInfo = endpointInfo;
            _format = format;
            _configuration = configuration;
        }

        protected ExceptionsConfigurationSettings Configuration => _configuration;

        public string ContentType => _format switch
        {
            ExceptionFormat.PlainText => ContentTypes.TextPlain,
            ExceptionFormat.NewlineDelimitedJson => ContentTypes.ApplicationNdJson,
            ExceptionFormat.JsonSequence => ContentTypes.ApplicationJsonSequence,
            _ => ContentTypes.TextPlain
        };

        protected ExceptionFormat Format => _format;

        public virtual bool IsStoppable => false;

        public Task Started => _startCompletionSource.Task;

        public async Task ExecuteAsync(Stream outputStream, CancellationToken token)
        {
            _startCompletionSource.TrySetResult();

            await ExecuteCoreAsync(outputStream, token);
        }

        public virtual Task StopAsync(CancellationToken token)
        {
            throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
        }

        protected abstract Task ExecuteCoreAsync(Stream outputStream, CancellationToken token);

        public string GenerateFileName()
        {
            string extension = _format == ExceptionFormat.PlainText ? "txt" : "json";
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{_endpointInfo.ProcessId}.exceptions.{extension}");
        }

        protected bool FilterException(IExceptionInstance instance)
        {
            if (_configuration.Exclude.Count > 0)
            {
                // filter out exceptions that match the filter
                if (_configuration.ShouldExclude(instance))
                {
                    return false;
                }
            }

            if (_configuration.Include.Count > 0)
            {
                // filter out exceptions that don't match the filter
                if (_configuration.ShouldInclude(instance))
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        protected async Task WriteJsonInstance(Stream stream, IExceptionInstance instance, CancellationToken token)
        {
            if (_format == ExceptionFormat.JsonSequence)
            {
                await stream.WriteAsync(JsonSequenceRecordSeparator, token);
            }

            // Make sure dotnet-monitor is self-consistent with other features that print type and stack information.
            // For example, the stacks and exceptions features should print structured stack traces exactly the same way.
            // CONSIDER: Investigate if other tools have "standard" formats for printing structured stacks and exceptions.
            await using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", instance.Id);
                // Writes the timestamp in ISO 8601 format
                writer.WriteString("timestamp", instance.Timestamp);
                writer.WriteString("typeName", instance.TypeName);
                writer.WriteString("moduleName", instance.ModuleName);
                writer.WriteString("message", instance.Message);

                if (IncludeActivityId(instance))
                {
                    writer.WriteStartObject("activity");
                    writer.WriteString("id", instance.ActivityId);
                    writer.WriteString("idFormat", instance.ActivityIdFormat.ToString("G"));
                    writer.WriteEndObject();
                }

                writer.WriteStartArray("innerExceptions");
                foreach (ulong innerExceptionId in instance.InnerExceptionIds)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("id", innerExceptionId);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                if (null != instance.CallStack)
                {
                    writer.WriteStartObject("stack");
                    writer.WriteNumber("threadId", instance.CallStack.ThreadId);
                    writer.WriteString("threadName", instance.CallStack.ThreadName);

                    writer.WriteStartArray("frames");

                    StringBuilder builder = new StringBuilder();

                    foreach (var frame in instance.CallStack.Frames)
                    {
                        writer.WriteStartObject();

                        string assembledMethodName = frame.MethodName;
                        if (frame.FullGenericArgTypes.Count > 0)
                        {
                            builder.Clear();
                            builder.Append(GenericStart);
                            builder.Append(string.Join(GenericSeparator, frame.FullGenericArgTypes));
                            builder.Append(GenericEnd);
                            assembledMethodName += builder.ToString();
                        }
                        writer.WriteString("methodName", assembledMethodName);
                        writer.WriteNumber("methodToken", frame.MethodToken);
                        writer.WriteStartArray("parameterTypes");
                        foreach (string parameterType in frame.FullParameterTypes)
                        {
                            writer.WriteStringValue(parameterType);
                        }
                        writer.WriteEndArray(); // end parameterTypes
                        writer.WriteString("typeName", frame.TypeName);
                        writer.WriteString("moduleName", frame.ModuleName);
                        writer.WriteString("moduleVersionId", frame.ModuleVersionId.ToString("D"));

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray(); // end frames
                    writer.WriteEndObject(); // end callStack
                }

                writer.WriteEndObject(); // end.
            }

            await stream.WriteAsync(JsonRecordDelimiter, token);
        }

        protected static async Task WriteTextInstance(
            Stream stream,
            IExceptionInstance currentInstance,
            IDictionary<ulong, IExceptionInstance> priorInstances,
            CancellationToken token)
        {
            // This format is similar of that which is written to the console when an unhandled exception occurs. Each
            // exception will appear as:

            // First chance exception at <TimeStamp>
            // <TypeName>: <Message>
            //  ---> <InnerExceptionTypeName>: <InnerExceptionMessage>
            //   at <StackFrameClass>.<StackFrameMethod>(<ParameterType1>, <ParameterType2>, ...)
            //   --- End of inner exception stack trace ---
            //   at <StackFrameClass>.<StackFrameMethod>(<ParameterType1>, <ParameterType2>, ...)

            await using StreamWriter writer = new(stream, leaveOpen: true);

            await writer.WriteAsync("First chance exception at ");
            await writer.WriteAsync(currentInstance.Timestamp.ToString("O", CultureInfo.InvariantCulture));

            await writer.WriteLineAsync();
            await WriteTextExceptionFormat(writer, currentInstance);
            await WriteTextInnerExceptionsAndStackFrames(writer, currentInstance, priorInstances);

            if (IncludeActivityId(currentInstance))
            {
                // ActivityIdFormat is intentionally being omitted
                await writer.WriteLineAsync();
                await writer.WriteAsync($"Activity ID: {currentInstance.ActivityId}");
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync();

#if NET8_0_OR_GREATER
            await writer.FlushAsync(token);
#else
            await writer.FlushAsync();
#endif
        }

        // Writes the inner exceptions and stack frames of the current exception:
        // - The primary inner exception is written with a separator message.
        // - The call stack frames are written for the current exception
        // - The remaining inner exceptions are written with their inner exception index
        // The above fits the format of inner exception and call stack information reported
        // by AggregateException instances.
        private static async Task WriteTextInnerExceptionsAndStackFrames(TextWriter writer, IExceptionInstance currentInstance, IDictionary<ulong, IExceptionInstance> priorInstances)
        {
            if (currentInstance.InnerExceptionIds?.Length > 0)
            {
                if (priorInstances.TryGetValue(
                        currentInstance.InnerExceptionIds[0],
                        out IExceptionInstance? primaryInnerInstance))
                {
                    await WriteTextInnerException(writer, primaryInnerInstance, 0, priorInstances);

                    await writer.WriteLineAsync();
                    await writer.WriteAsync("   --- End of inner exception stack trace ---");
                }
                else
                {
                    await writer.WriteLineAsync();
                    await writer.WriteAsync("   --- The inner exception was not included in the filter ---");
                }
            }

            StringBuilder builder = new();
            if (null != currentInstance.CallStack)
            {
                foreach (CallStackFrame frame in currentInstance.CallStack.Frames)
                {
                    await writer.WriteLineAsync();
                    await writer.WriteAsync("   at ");
                    await writer.WriteAsync(frame.TypeName);
                    await writer.WriteAsync(".");
                    await writer.WriteAsync(frame.MethodName);

                    NameFormatter.BuildGenericArgTypes(builder, frame.SimpleGenericArgTypes);
                    await writer.WriteAsync(builder);
                    builder.Clear();

                    NameFormatter.BuildMethodParameterTypes(builder, frame.SimpleParameterTypes);
                    await writer.WriteAsync(builder);
                    builder.Clear();
                }
            }

            if (currentInstance.InnerExceptionIds?.Length > 1)
            {
                for (int index = 1; index < currentInstance.InnerExceptionIds.Length; index++)
                {
                    if (priorInstances.TryGetValue(
                        currentInstance.InnerExceptionIds[index],
                        out IExceptionInstance? secondaryInnerInstance))
                    {
                        await WriteTextInnerException(writer, secondaryInnerInstance, index, priorInstances);
                    }
                }
            }
        }

        // Writes the specified exception as an inner exception with the appropriate delimiters.
        private static async Task WriteTextInnerException(TextWriter writer, IExceptionInstance currentInstance, int currentIndex, IDictionary<ulong, IExceptionInstance> priorInstances)
        {
            await writer.WriteLineAsync();
            await writer.WriteAsync(" ---> ");

            if (0 < currentIndex)
            {
                await writer.WriteAsync("(Inner Exception #");
                await writer.WriteAsync(currentIndex.ToString("D", CultureInfo.InvariantCulture));
                await writer.WriteAsync(") ");
            }

            await WriteTextExceptionFormat(writer, currentInstance);

            await WriteTextInnerExceptionsAndStackFrames(writer, currentInstance, priorInstances);

            if (0 < currentIndex)
            {
                await writer.WriteAsync("<---");
            }
        }

        // Writes the basic exception information, namely the type and message
        private static async Task WriteTextExceptionFormat(TextWriter writer, IExceptionInstance instance)
        {
            await writer.WriteAsync(instance.TypeName);
            if (!string.IsNullOrEmpty(instance.Message))
            {
                await writer.WriteAsync(": ");
                await writer.WriteAsync(instance.Message);
            }
        }

        private static bool IncludeActivityId(IExceptionInstance instance)
        {
            return !string.IsNullOrEmpty(instance.ActivityId);
        }
    }
}
