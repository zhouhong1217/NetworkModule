#region 1. 请求构建器（RequestBuilder）

using System.Text;
using Newtonsoft.Json;

public class RequestBuilder
{
    private Request _request = new Request();

    public RequestBuilder SetType(RequestType type)
    {
        _request.Type = type;
        return this;
    }

    // 设置Protobuf数据
    public RequestBuilder SetProtoData<T>(T data) where T : class
    {
        _request.ProtoData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        return this;
    }

    public RequestBuilder SetUrl(string url)
    {
        _request.Url = url;
        return this;
    }

    public RequestBuilder SetTimeout(float seconds)
    {
        _request.Timeout = seconds;
        return this;
    }

    public RequestBuilder SetRetryPolicy(RetryPolicy policy, int maxRetries = 2)
    {
        _request.RetryPolicy = policy;
        _request.MaxRetries = maxRetries;
        return this;
    }

    public RequestBuilder AddHeader(string key, string value)
    {
        _request.Headers[key] = value;
        return this;
    }

    public RequestBuilder SetTag(object tag)
    {
        _request.Tag = tag;
        return this;
    }

    public Request Build()
    {
        // 设置默认URL
        if (string.IsNullOrEmpty(_request.Url))
        {
            _request.Url = $"https://your-api-server.com/{_request.Type.ToString().ToLower()}";
        }
        
        // 添加默认头
        if (!_request.Headers.ContainsKey("Content-Type"))
        {
            _request.Headers.Add("Content-Type", "application/x-protobuf");
        }
        
        return _request;
    }
}
#endregion