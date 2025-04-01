using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

public class GestureReceiver : MonoBehaviour
{
    [Header("网络设置")]
    public int listenPort = 5065;
    
    [Header("手势响应")]
    public GameObject[] gestureObjects; // 数组索引对应手势类型：0=未识别，1=打开手掌，2=握拳，3=指向，4=OK，5=胜利，6=大拇指向上
    
    private UdpClient udpClient;
    private int currentGesture = 0;
    private bool isReceiving = false;
    
    void Start()
    {
        // 初始化UDP客户端
        try
        {
            udpClient = new UdpClient(listenPort);
            isReceiving = true;
            // 开始接收数据
            StartCoroutine(ReceiveData());
            Debug.Log("手势接收器已启动，监听端口: " + listenPort);
        }
        catch (Exception e)
        {
            Debug.LogError("UDP客户端初始化失败: " + e.Message);
        }
        
        // 初始设置对象状态
        UpdateObjectsVisibility();
    }
    
    void OnApplicationQuit()
    {
        isReceiving = false;
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
    
    private IEnumerator ReceiveData()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, listenPort);
        
        while (isReceiving)
        {
            try
            {
                if (udpClient.Available > 0)
                {
                    byte[] data = udpClient.Receive(ref endPoint);
                    string message = Encoding.UTF8.GetString(data);
                    
                    // 尝试解析为整数
                    if (int.TryParse(message, out int gestureType))
                    {
                        // 更新当前手势
                        currentGesture = gestureType;
                        Debug.Log("收到手势: " + currentGesture);
                        
                        // 更新对象可见性
                        UpdateObjectsVisibility();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("接收数据错误: " + e.Message);
            }
            
            yield return null; // 等待下一帧
        }
    }
    
    void UpdateObjectsVisibility()
    {
        if (gestureObjects == null || gestureObjects.Length == 0)
            return;
            
        // 隐藏所有手势对象
        foreach (var obj in gestureObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
        
        // 激活当前手势对应的对象（如果存在）
        if (currentGesture >= 0 && currentGesture < gestureObjects.Length && gestureObjects[currentGesture] != null)
        {
            gestureObjects[currentGesture].SetActive(true);
        }
    }
}
