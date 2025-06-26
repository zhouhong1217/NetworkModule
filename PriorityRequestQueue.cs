#region 2. 优先级请求队列（PriorityRequestQueue）

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;
using UnityEngine.Networking;

public class PriorityRequestQueue
{
    private List<RequestTask>[] _priorityQueues;
    private List<Coroutine> _activeCoroutines = new List<Coroutine>();
    private int _maxConcurrent;
    private MonoBehaviour _coroutineRunner;

    public PriorityRequestQueue(int priorityLevels, int maxConcurrent = 4)
    {
        _maxConcurrent = maxConcurrent;
        _priorityQueues = new List<RequestTask>[priorityLevels];
        
        for (int i = 0; i < priorityLevels; i++)
        {
            _priorityQueues[i] = new List<RequestTask>();
        }
        
        _coroutineRunner = NetworkManager.Instance;
    }

    public void Enqueue(Request request, Action<Response> onSuccess, 
        Action<NetworkError> onError, int priority = 1)
    {
        // 确保优先级在有效范围内
        int actualPriority = Math.Clamp(priority, 0, _priorityQueues.Length - 1);
        var task = new RequestTask(request, onSuccess, onError, actualPriority);
        _priorityQueues[actualPriority].Add(task);
        TryProcessNext();
    }

    private void TryProcessNext()
    {
        if (_activeCoroutines.Count >= _maxConcurrent) return;
        
        // 从高优先级队列开始查找待处理请求
        for (int i = _priorityQueues.Length - 1; i >= 0; i--)
        {
            if (_priorityQueues[i].Count > 0)
            {
                RequestTask task = _priorityQueues[i][0];
                _priorityQueues[i].RemoveAt(0);
                
                Coroutine coroutine = _coroutineRunner.StartCoroutine(ExecuteRequest(task));
                _activeCoroutines.Add(coroutine);
                
                if (_activeCoroutines.Count >= _maxConcurrent) break;
            }
        }
    }

    private IEnumerator ExecuteRequest(RequestTask task)
    {
        UnityWebRequest unityRequest = CreateUnityWebRequest(task.Request);
        unityRequest.SendWebRequest();

        // 超时控制
        float startTime = Time.time;
        while (!unityRequest.isDone)
        {
            if (Time.time - startTime > task.Request.Timeout)
            {
                unityRequest.Abort();
                HandleFailure(task, unityRequest, true);
                yield break;
            }
            yield return null;
        }

        // 处理结果
        if (unityRequest.result == UnityWebRequest.Result.Success)
        {
            Response response = ProcessResponse(unityRequest);
            response.Tag = task.Request.Tag; // 传递上下文
            task.OnSuccess?.Invoke(response);
        }
        else
        {
            HandleFailure(task, unityRequest, false);
        }

        unityRequest.Dispose();
        _activeCoroutines.Remove(_coroutineRunner.StartCoroutine(ExecuteRequest(task)));
        TryProcessNext();
    }

    private UnityWebRequest CreateUnityWebRequest(Request request)
    {
        UnityWebRequest unityRequest;
        
        switch (request.Type)
        {
            case RequestType.POST:
                unityRequest = new UnityWebRequest(request.Url, "POST");
                break;
                
            case RequestType.PUT:
                unityRequest = new UnityWebRequest(request.Url, "PUT");
                break;
                
            case RequestType.DELETE:
                unityRequest = UnityWebRequest.Delete(request.Url);
                break;
                
            default: // GET
                unityRequest = UnityWebRequest.Get(request.Url);
                break;
        }

        // 设置Protobuf数据
        if (request.ProtoData != null && request.ProtoData.Length > 0 && 
            (request.Type == RequestType.POST || request.Type == RequestType.PUT))
        {
            unityRequest.uploadHandler = new UploadHandlerRaw(request.ProtoData);
        }

        // 添加签名头
        string signature = SecurityUtil.GenerateRequestSignature(request.ProtoData);
        request.Headers["X-Api-Signature"] = signature;

        // 设置请求头
        foreach (var header in request.Headers)
        {
            unityRequest.SetRequestHeader(header.Key, header.Value);
        }

        unityRequest.downloadHandler = new DownloadHandlerBuffer();
        return unityRequest;
    }

    private Response ProcessResponse(UnityWebRequest unityRequest)
    {
        // 验证响应签名
        byte[] responseData = unityRequest.downloadHandler.data;
        if (!SecurityUtil.ValidateResponseSignature(responseData, 
            unityRequest.GetResponseHeader("X-Data-Sign")))
        {
            throw new SecurityException("Response signature validation failed");
        }

        return new Response
        {
            StatusCode = (int)unityRequest.responseCode,
            ProtoData = responseData
        };
    }

    private void HandleFailure(RequestTask task, UnityWebRequest unityRequest, bool isTimeout)
    {
        if (task.CurrentRetryCount < task.Request.MaxRetries && 
            task.Request.RetryPolicy != RetryPolicy.None)
        {
            task.CurrentRetryCount++;
            float delay = CalculateRetryDelay(task.Request.RetryPolicy, task.CurrentRetryCount);
            NetworkManager.Instance.StartCoroutine(RequeueAfterDelay(task, delay));
        }
        else
        {
            task.OnError?.Invoke(new NetworkError
            {
                Code = (int)unityRequest.responseCode,
                Message = isTimeout ? "Request timed out" : unityRequest.error,
                IsNetworkError = unityRequest.result == UnityWebRequest.Result.ConnectionError
            });
        }
    }

    private float CalculateRetryDelay(RetryPolicy policy, int retryCount)
    {
        return policy switch
        {
            RetryPolicy.Linear => 2f, // 固定2秒延迟
            RetryPolicy.ExponentialBackoff => Mathf.Pow(2, retryCount), // 指数退避
            _ => 0f
        };
    }

    private IEnumerator RequeueAfterDelay(RequestTask task, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 根据原始优先级重新入队
        _priorityQueues[task.Priority].Add(task);
        TryProcessNext();
    }
}
#endregion