// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ThreadDataManager.h"
#include "macros.h"
#include <utility>

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(_logger, EXPR)
#define IfFalseLogRet(EXPR, hr) IfFalseLogRet_(_logger, EXPR, hr)

#define LogDebugV(format, ...) LogDebugV_(_logger, format, __VA_ARGS__)

typedef unordered_map<ThreadID, shared_ptr<ThreadData>>::iterator DataMapIterator;

ThreadDataManager::ThreadDataManager(const shared_ptr<ILogger>& logger)
{
    _logger = logger;
}

void ThreadDataManager::AddProfilerEventMask(DWORD& eventsLow)
{
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_THREADS;
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_EXCEPTIONS;
}

HRESULT ThreadDataManager::ThreadCreated(ThreadID threadId)
{
    lock_guard<mutex> lock(_dataMapMutex);

    HRESULT hr = S_OK;

    LogDebugV("Thread Created: %d", threadId);

    _dataMap.insert(make_pair(threadId, make_shared<ThreadData>(_logger)));

    return S_OK;
}

HRESULT ThreadDataManager::ThreadDestroyed(ThreadID threadId)
{
    lock_guard<mutex> lock(_dataMapMutex);

    HRESULT hr = S_OK;

    LogDebugV("Thread Destroyed: %d", threadId);

    _dataMap.erase(threadId);

    return S_OK;
}

HRESULT ThreadDataManager::ClearException(ThreadID threadId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    threadData->ClearException();

    return S_OK;
}

HRESULT ThreadDataManager::GetException(ThreadID threadId, bool* hasException, FunctionID* catcherFunctionId)
{
    ExpectedPtr(hasException);
    ExpectedPtr(catcherFunctionId);

    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    IfFailLogRet(threadData->GetException(hasException, catcherFunctionId));

    return *hasException ? S_FALSE : S_OK;
}

HRESULT ThreadDataManager::SetHasException(ThreadID threadId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    IfFailLogRet(threadData->SetHasException());

    return S_OK;
}

HRESULT ThreadDataManager::SetExceptionCatcherFunction(ThreadID threadId, FunctionID catcherFunctionId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    IfFailLogRet(threadData->SetExceptionCatcherFunction(catcherFunctionId));

    return S_OK;
}

HRESULT ThreadDataManager::AnyExceptions(bool* hasException)
{
    ExpectedPtr(hasException);

    HRESULT hr = S_OK;

    lock_guard<mutex> mapLock(_dataMapMutex);

    FunctionID catcherFunctionId;
    for (DataMapIterator it = _dataMap.begin(); it != _dataMap.end(); ++it )
    {
        IfFailLogRet(it->second->GetException(hasException, &catcherFunctionId));

        if (hasException)
        {
            return S_OK;
        }
    }

    return S_FALSE;
}

HRESULT ThreadDataManager::GetThreadData(ThreadID threadId, shared_ptr<ThreadData>& threadData)
{
    lock_guard<mutex> mapLock(_dataMapMutex);

    DataMapIterator iterator = _dataMap.find(threadId);
    IfFalseLogRet(iterator != _dataMap.end(), E_UNEXPECTED);

    threadData = iterator->second;

    return S_OK;
}
