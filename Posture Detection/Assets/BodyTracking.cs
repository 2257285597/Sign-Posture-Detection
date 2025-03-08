using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyTra : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] BodyPoints;

    void Update()
    {
        string rawData = udpReceive.data;

        // 1. ���������Ч��
        if (string.IsNullOrEmpty(rawData)) return;
        if (!rawData.StartsWith("[") || !rawData.EndsWith("]")) return;

        try
        {
            // 2. ��ȫ�Ƴ���β�ַ�
            string cleanedData = rawData.Substring(1, rawData.Length - 2);

            // 3. �ָ����ݲ���鳤��
            string[] points = cleanedData.Split(',');
            if (points.Length != 33 * 3) // Ԥ��33���㣬ÿ����x,y,z
            {
                Debug.LogWarning($"���ݳ����쳣��Ԥ��99��ʵ��{points.Length}");
                return;
            }

            // 4. ��ȫ���������¹ؽڵ�
            for (int i = 0; i < 33; i++)
            {
                int index = i * 3;
                float x = 7 - float.Parse(points[index]) / 100;
                float y = float.Parse(points[index + 1]) / 100;
                float z = float.Parse(points[index + 2]) / 300;

                BodyPoints[i].transform.localPosition = new Vector3(x * 0.5f, y * 0.5f, z * 0.5f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"���ݴ���ʧ��: {e.Message}");
        }
    }
}