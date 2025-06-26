
#region 核心数据结构

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public enum RequestType { GET, POST, PUT, DELETE }
public enum RetryPolicy { None, Linear, ExponentialBackoff }

[Serializable]
public class Request
{
    [JsonProperty] public RequestType Type { get; set; }
    [JsonProperty] public string Url { get; set; }
    [JsonProperty] public byte[] ProtoData { get; set; }
    [JsonProperty] public float Timeout { get; set; } = 10f;
    [JsonProperty] public int MaxRetries { get; set; } = 2;
    [JsonProperty] public RetryPolicy RetryPolicy { get; set; }
    [JsonProperty] public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    [JsonIgnore] public object Tag { get; set; } // 用于存储额外上下文
}

[Serializable]
public class Response
{
    [JsonProperty] public int StatusCode { get; set; }
    [JsonProperty] public byte[] ProtoData { get; set; }
    [JsonIgnore] public object Tag { get; set; } // 用于存储额外上下文
}

[Serializable]
public class NetworkError
{
    [JsonProperty] public int Code { get; set; }
    [JsonProperty] public string Message { get; set; }
    [JsonProperty] public bool IsNetworkError { get; set; }
    [JsonProperty] public bool IsOffline { get; set; }
}

public class RequestTask
{
    public Request Request { get; }
    public Action<Response> OnSuccess { get; }
    public Action<NetworkError> OnError { get; }
    public int CurrentRetryCount { get; set; } = 0;
    public int Priority { get; }

    public RequestTask(Request request, Action<Response> onSuccess, 
        Action<NetworkError> onError, int priority)
    {
        Request = request;
        OnSuccess = onSuccess;
        OnError = onError;
        Priority = priority;
    }
}
#endregion