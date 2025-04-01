using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandTracking : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] handPoints;
    void Start()
    {

    }
    void Update()
    {
        string data = udpReceive.data;
        // 检查是否有数据
        if (!string.IsNullOrEmpty(data))
        {
            // 移除数据包的起始和结束标记
            data = data.Remove(0, 3);
            data = data.Remove(data.Length - 1, 1);

            print(data);
            print(data.Length);
            string[] points = data.Split(',');

            // 检查分割后的数据点数量是否足够
            if (points.Length >= 63) // 21个点，每个点3个坐标值
            {
                for (int i = 0; i < 21; i++)
                {
                    float x = 7 - float.Parse(points[i * 3]) / 100;
                    float y = float.Parse(points[i * 3 + 1]) / 100;
                    float z = float.Parse(points[i * 3 + 2]) / 100;

                    handPoints[i].transform.localPosition = new Vector3(x, y, z);
                }
            }
            else
            {
                print("Received data does not contain enough points.");
            }
        }
        else
        {
            print("No data received.");
        }
    }
}

