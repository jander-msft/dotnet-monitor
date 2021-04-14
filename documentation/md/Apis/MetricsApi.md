# MetricsApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**getMetrics**](MetricsApi.md#getMetrics) | **GET** /metrics | Get a list of the current backlog of metrics for a process in the Prometheus exposition format.


<a name="getMetrics"></a>
# **getMetrics**
> String getMetrics()

Get a list of the current backlog of metrics for a process in the Prometheus exposition format.

### Parameters
This endpoint does not need any parameter.

### Return type

[**String**](../Models/string.md)

### Authorization

No authorization required

### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/problem+json, text/plain; version=0.0.4

