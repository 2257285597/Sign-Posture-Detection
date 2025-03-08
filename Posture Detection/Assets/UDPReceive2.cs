using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPReceive2 : MonoBehaviour
{
    public int port = 5055;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    [HideInInspector] public float[] handData;

    void Start()
    {
        udpClient = new UdpClient(port);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        while (isRunning)
        {
            try
            {
                byte[] bytes = udpClient.Receive(ref remoteEndPoint);
                handData = new float[bytes.Length / 4];
                Buffer.BlockCopy(bytes, 0, handData, 0, bytes.Length);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}