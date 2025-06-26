using System;
using System.Collections;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _instance;
    public static NetworkManager Instance => _instance;

    private PriorityRequestQueue _requestQueue;
    private WebSocketController _webSocketController;
    private OfflineCacheManager _offlineCache;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 初始化优先级请求队列
        _requestQueue = new PriorityRequestQueue(4); // 4个优先级级别
        
        // 初始化离线缓存管理器
        _offlineCache = new OfflineCacheManager();
        
        // 初始化WebSocket控制器
        _webSocketController = new WebSocketController("wss://your-game-server.com/realtime");
        _webSocketController.Connect();
        
        // 网络状态监控
        StartCoroutine(NetworkStatusMonitor());
    }

    void OnDestroy()
    {
        _webSocketController?.Disconnect();
        _offlineCache?.SavePendingRequests();
    }

    // 发送请求的公共接口（带优先级）
    public void SendRequest(Request request, Action<Response> onSuccess, 
        Action<NetworkError> onError, int priority = 1)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // 离线模式：存入缓存
            _offlineCache.CacheRequest(request, onSuccess, onError, priority);
            return;
        }
        
        _requestQueue.Enqueue(request, onSuccess, onError, priority);
    }

    // WebSocket发送消息
    public void SendWebSocketMessage(string message)
    {
        _webSocketController?.Send(message);
    }

    // 订阅WebSocket消息
    public void SubscribeWebSocket(Action<string> handler)
    {
        _webSocketController?.Subscribe(handler);
    }

    // 网络状态监控协程
    private IEnumerator NetworkStatusMonitor()
    {
        bool wasOnline = Application.internetReachability != NetworkReachability.NotReachable;
        
        while (true)
        {
            yield return new WaitForSeconds(5f);
            
            bool isOnline = Application.internetReachability != NetworkReachability.NotReachable;
            
            if (isOnline && !wasOnline)
            {
                // 网络恢复：处理缓存请求
                _offlineCache.ProcessCachedRequests();
            }
            
            wasOnline = isOnline;
        }
    }
}