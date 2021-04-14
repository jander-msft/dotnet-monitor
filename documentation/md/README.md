# Documentation for dotnet-monitor

<a name="documentation-for-api-endpoints"></a>
## Documentation for API Endpoints

All URIs are relative to *http://localhost*

Class | Method | HTTP request | Description
------------ | ------------- | ------------- | -------------
*DiagApi* | [**captureDump**](Apis/DiagApi.md#capturedump) | **GET** /dump/{processKey} | Capture a dump of a process.
*DiagApi* | [**captureGcDump**](Apis/DiagApi.md#capturegcdump) | **GET** /gcdump/{processKey} | Capture a GC dump of a process.
*DiagApi* | [**captureLogs**](Apis/DiagApi.md#capturelogs) | **GET** /logs/{processKey} | Capture a stream of logs from a process.
*DiagApi* | [**captureTrace**](Apis/DiagApi.md#capturetrace) | **GET** /trace/{processKey} | Capture a trace of a process.
*DiagApi* | [**captureTraceCustom**](Apis/DiagApi.md#capturetracecustom) | **POST** /trace/{processKey} | Capture a trace of a process.
*DiagApi* | [**getProcessEnvironment**](Apis/DiagApi.md#getprocessenvironment) | **GET** /processes/{processKey}/env | Get the environment block of the specified process.
*DiagApi* | [**getProcessInfo**](Apis/DiagApi.md#getprocessinfo) | **GET** /processes/{processKey} | Get information about the specified process.
*DiagApi* | [**getProcesses**](Apis/DiagApi.md#getprocesses) | **GET** /processes | Get the list of accessible processes.
*MetricsApi* | [**getMetrics**](Apis/MetricsApi.md#getmetrics) | **GET** /metrics | Get a list of the current backlog of metrics for a process in the Prometheus exposition format.


<a name="documentation-for-models"></a>
## Documentation for Models

 - [DumpType](./Models/DumpType.md)
 - [EventLevel](./Models/EventLevel.md)
 - [EventPipeConfiguration](./Models/EventPipeConfiguration.md)
 - [EventPipeProvider](./Models/EventPipeProvider.md)
 - [LogLevel](./Models/LogLevel.md)
 - [ProcessIdentifier](./Models/ProcessIdentifier.md)
 - [ProcessInfo](./Models/ProcessInfo.md)
 - [TraceProfile](./Models/TraceProfile.md)
 - [ValidationProblemDetails](./Models/ValidationProblemDetails.md)


<a name="documentation-for-authorization"></a>
## Documentation for Authorization

All endpoints do not require authorization.
