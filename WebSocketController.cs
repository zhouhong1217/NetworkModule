#region 3. WebSocket控制器（WebSocketController）

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebSocketController
{
    private UnityWebRequest _webSocketRequest;
    private Uri _serverUri;
    private bool _isConnected;
    private Coroutine _heartbeatCoroutine;
    private List<Action<string>> _messageHandlers = new List<Action<string>>();
    private float _baseHeartbeatInterval = 5f;

    public WebSocketController(string url)
    {
        _serverUri = new Uri(url);
    }

    public void Connect()
    {
        if (_isConnected) return;
        
        _webSocketRequest = UnityWebRequest.Get(_serverUri);
        _webSocketRequest.SetRequestHeader("Upgrade", "websocket");
        _webSocketRequest.SetRequestHeader("Connection", "Upgrade");
        _webSocketRequest.downloadHandler = new DownloadHandlerBuffer();
        _webSocketRequest.SendWebRequest();
        
        NetworkManager.Instance.StartCoroutine(WaitForConnection());
    }

    private IEnumerator WaitForConnection()
    {
        while (!_webSocketRequest.isDone)
        {
            if (_webSocketRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"WebSocket connection error: {_webSocketRequest.error}");
                yield break;
            }
            yield return null;
        }

        if (_webSocketRequest.responseCode == 101) // Switching Protocols
        {
            _isConnected = true;
            StartHeartbeat();
            NetworkManager.Instance.StartCoroutine(ListenForMessages());
        }
    }

    public void Disconnect()
    {
        if (!_isConnected) return;
        
        StopHeartbeat();
        _webSocketRequest?.Abort();
        _webSocketRequest?.Dispose();
        _isConnected = false;
    }

    public void Send(string message)
    {
        if (!_isConnected) return;
        
        // 实际发送逻辑（需要根据Unity WebSocket实现调整）
        // 这里简化处理，实际应使用WebSocket.Send()
        Debug.Log($"WebSocket sending: {message}");
    }

    public void Subscribe(Action<string> handler)
    {
        if (!_messageHandlers.Contains(handler))
        {
            _messageHandlers.Add(handler);
        }
    }

    private void StartHeartbeat()
    {
        StopHeartbeat();
        _heartbeatCoroutine = NetworkManager.Instance.StartCoroutine(HeartbeatRoutine());
    }

    private void StopHeartbeat()
    {
        if (_heartbeatCoroutine != null)
        {
            NetworkManager.Instance.StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }
    }

    private IEnumerator HeartbeatRoutine()
    {
        while (_isConnected)
        {
            yield return new WaitForSeconds(GetAdaptiveHeartbeatInterval());
            
            if (_isConnected)
            {
                Send("ping"); // 发送心跳包
            }
        }
    }

    private IEnumerator ListenForMessages()
    {
        while (_isConnected)
        {
            // 简化处理 - 实际应处理WebSocket消息接收
            // 这里假设有一个消息队列
            yield return null;
        }
    }

    private float GetAdaptiveHeartbeatInterval()
    {
        switch (Application.internetReachability)
        {
            case NetworkReachability.ReachableViaLocalAreaNetwork:
                return _baseHeartbeatInterval; // WiFi环境：5秒
            
            case NetworkReachability.ReachableViaCarrierDataNetwork:
                return _baseHeartbeatInterval * 3; // 移动网络：15秒
            
            default: // 弱网环境
                return _baseHeartbeatInterval * 12; // 60秒
        }
    }
}
#endregion