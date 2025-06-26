#region 3. WebSocket控制器（WebSocketController）

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class WebSocketController
{
    WebSocket _websocket;

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
        
        // TODO 这里引用NativeWebSocket，使用NativeWebSocket创建WebSocket
        websocket = new WebSocket("ws://localhost:3000");

        websocket.OnOpen += () =>
        {
        Debug.Log("Connection open!");
        };

        websocket.OnError += (e) =>
        {
        Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
        Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            Debug.Log("OnMessage!");
            Debug.Log(bytes);

            // getting the message as a string
            // var message = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log("OnMessage! " + message);
        };

        // waiting for messages
        await websocket.Connect();
        // _webSocketRequest = UnityWebRequest.Get(_serverUri);
        // _webSocketRequest.SetRequestHeader("Upgrade", "websocket");
        // _webSocketRequest.SetRequestHeader("Connection", "Upgrade");
        // _webSocketRequest.downloadHandler = new DownloadHandlerBuffer();
        // _webSocketRequest.SendWebRequest();
        
        // NetworkManager.Instance.StartCoroutine(WaitForConnection());


        if (websocket.State == WebSocketState.Open)
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