# DiagApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**captureDump**](DiagApi.md#captureDump) | **GET** /dump/{processKey} | Capture a dump of a process.
[**captureGcDump**](DiagApi.md#captureGcDump) | **GET** /gcdump/{processKey} | Capture a GC dump of a process.
[**captureLogs**](DiagApi.md#captureLogs) | **GET** /logs/{processKey} | Capture a stream of logs from a process.
[**captureTrace**](DiagApi.md#captureTrace) | **GET** /trace/{processKey} | Capture a trace of a process.
[**captureTraceCustom**](DiagApi.md#captureTraceCustom) | **POST** /trace/{processKey} | Capture a trace of a process.
[**getProcessEnvironment**](DiagApi.md#getProcessEnvironment) | **GET** /processes/{processKey}/env | Get the environment block of the specified process.
[**getProcessInfo**](DiagApi.md#getProcessInfo) | **GET** /processes/{processKey} | Get information about the specified process.
[**getProcesses**](DiagApi.md#getProcesses) | **GET** /processes | Get the list of accessible processes.


<a name="captureDump"></a>
# **captureDump**
> File captureDump(processKey, type, egressProvider)

Capture a dump of a process.

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **processKey** | [**oneOf&lt;integer,UUID&gt;**](../Models/.md)| Value used to identify the target process, either the process ID or the runtime instance cookie. | [default to null]
 **type** | [**DumpType**](../Models/.md)| The type of dump to capture. | [optional] [default to null] [enum: Full, Mini, WithHeap, Triage]
 **egressProvider** | **String**| The egress provider to which the dump is saved. | [optional] [default to null]

### Return type

[**File**](../Models/file.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, application/octet-stream

<a name="captureGcDump"></a>
# **captureGcDump**
> File captureGcDump(processKey, egressProvider)

Capture a GC dump of a process.

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **processKey** | [**oneOf&lt;integer,UUID&gt;**](../Models/.md)| Value used to identify the target process, either the process ID or the runtime instance cookie. | [default to null]
 **egressProvider** | **String**| The egress provider to which the GC dump is saved. | [optional] [default to null]

### Return type

[**File**](../Models/file.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, application/octet-stream

<a name="captureLogs"></a>
# **captureLogs**
> String captureLogs(processKey, durationSeconds, level, egressProvider)

Capture a stream of logs from a process.

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **processKey** | [**oneOf&lt;integer,UUID&gt;**](../Models/.md)| Value used to identify the target process, either the process ID or the runtime instance cookie. | [default to null]
 **durationSeconds** | **Integer**| The duration of the trace session (in seconds). | [optional] [default to 30]
 **level** | [**LogLevel**](../Models/.md)| The level of the logs to capture. | [optional] [default to null] [enum: Trace, Debug, Information, Warning, Error, Critical, None]
 **egressProvider** | **String**| The egress provider to which the trace is saved. | [optional] [default to null]

### Return type

[**String**](../Models/string.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, application/x-ndjson, text/event-stream

<a name="captureTrace"></a>
# **captureTrace**
> File captureTrace(processKey, profile, durationSeconds, metricsIntervalSeconds, egressProvider)

Capture a trace of a process.

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **processKey** | [**oneOf&lt;integer,UUID&gt;**](../Models/.md)| Value used to identify the target process, either the process ID or the runtime instance cookie. | [default to null]
 **profile** | [**TraceProfile**](../Models/.md)| The profiles enabled for the trace session. | [optional] [default to null] [enum: Cpu, Http, Logs, Metrics]
 **durationSeconds** | **Integer**| The duration of the trace session (in seconds). | [optional] [default to 30]
 **metricsIntervalSeconds** | **Integer**| The reporting interval (in seconds) for event counters. | [optional] [default to 1]
 **egressProvider** | **String**| The egress provider to which the trace is saved. | [optional] [default to null]

### Return type

[**File**](../Models/file.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, application/octet-stream

<a name="captureTraceCustom"></a>
# **captureTraceCustom**
> File captureTraceCustom(processKey, EventPipeConfiguration, durationSeconds, egressProvider)

Capture a trace of a process.

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **processKey** | [**oneOf&lt;integer,UUID&gt;**](../Models/.md)| Value used to identify the target process, either the process ID or the runtime instance cookie. | [default to null]
 **EventPipeConfiguration** | [**EventPipeConfiguration**](../Models/EventPipeConfiguration.md)| The trace configuration describing which events to capture. |
 **durationSeconds** | **Integer**| The duration of the trace session (in seconds). | [optional] [default to 30]
 **egressProvider** | **String**| The egress provider to which the trace is saved. | [optional] [default to null]

### Return type

[**File**](../Models/file.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: application/json, text/json, application/*+json
- **Accept**: application/problem+json, application/octet-stream

<a name="getProcessEnvironment"></a>
# **getProcessEnvironment**
> Map getProcessEnvironment(processKey)

Get the environment block of the specified process.

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **processKey** | [**oneOf&lt;integer,UUID&gt;**](../Models/.md)| Value used to identify the target process, either the process ID or the runtime instance cookie. | [default to null]

### Return type

[**Map**](../Models/string.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, application/json

<a name="getProcessInfo"></a>
# **getProcessInfo**
> ProcessInfo getProcessInfo(processKey)

Get information about the specified process.

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **processKey** | [**oneOf&lt;integer,UUID&gt;**](../Models/.md)| Value used to identify the target process, either the process ID or the runtime instance cookie. | [default to null]

### Return type

[**ProcessInfo**](../Models/ProcessInfo.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, application/json

<a name="getProcesses"></a>
# **getProcesses**
> List getProcesses()

Get the list of accessible processes.

### Parameters
This endpoint does not need any parameter.

### Return type

[**List**](../Models/ProcessIdentifier.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, application/json

