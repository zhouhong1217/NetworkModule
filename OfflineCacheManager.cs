#region 5. 离线缓存管理器

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class OfflineCacheManager
{
    private const string CACHE_FILE = "offline_requests.dat";
    private List<CachedRequest> _pendingRequests = new List<CachedRequest>();

    public OfflineCacheManager()
    {
        LoadPendingRequests();
    }

    // 缓存请求
    public void CacheRequest(Request request, Action<Response> onSuccess, 
        Action<NetworkError> onError, int priority)
    {
        var cached = new CachedRequest
        {
            Request = request,
            OnSuccess = onSuccess,
            OnError = onError,
            Priority = priority,
            Timestamp = DateTime.UtcNow
        };
        
        _pendingRequests.Add(cached);
        SavePendingRequests();
    }

    // 处理缓存请求
    public void ProcessCachedRequests()
    {
        if (_pendingRequests.Count == 0) return;
        
        // 按时间戳排序（先缓存的先处理）
        var orderedRequests = _pendingRequests
            .OrderBy(r => r.Timestamp)
            .ToList();
        
        _pendingRequests.Clear();
        
        foreach (var cached in orderedRequests)
        {
            NetworkManager.Instance.SendRequest(
                cached.Request, 
                cached.OnSuccess, 
                cached.OnError, 
                cached.Priority
            );
        }
    }

    // 加载缓存请求
    private void LoadPendingRequests()
    {
        if (!PlayerPrefs.HasKey(CACHE_FILE))
            return;
        
        try
        {
            _pendingRequests = JsonConvert.DeserializeObject<List<CachedRequest>>(PlayerPrefs.GetString(CACHE_FILE));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load offline cache: {ex.Message}");
            _pendingRequests = new List<CachedRequest>();
        }
    }

    // 保存缓存请求
    public void SavePendingRequests()
    {
        if (_pendingRequests.Count == 0) return;
        
        try
        {
            PlayerPrefs.SetString(CACHE_FILE, JsonConvert.SerializeObject(_pendingRequests));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save offline cache: {ex.Message}");
        }
    }

    [Serializable]
    private class CachedRequest
    {
        [JsonProperty] public Request Request { get; set; }
        [JsonProperty] public int Priority { get; set; }
        [JsonProperty] public DateTime Timestamp { get; set; }
        
        [JsonIgnore]
        public Action<Response> OnSuccess { get; set; }
        
        [JsonIgnore]
        public Action<NetworkError> OnError { get; set; }
    }
}
#endregion