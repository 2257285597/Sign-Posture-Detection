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

        // 1. 检查数据有效性
        if (string.IsNullOrEmpty(rawData)) return;
        if (!rawData.StartsWith("[") || !rawData.EndsWith("]")) return;

        try
        {
            // 2. 安全移除首尾字符
            string cleanedData = rawData.Substring(1, rawData.Length - 2);

            // 3. 分割数据并检查长度
            string[] points = cleanedData.Split(',');
            if (points.Length != 33 * 3) // 预期33个点，每个点x,y,z
            {
                Debug.LogWarning($"数据长度异常，预期99，实际{points.Length}");
                return;
            }

            // 4. 安全遍历并更新关节点
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
            Debug.LogError($"数据处理失败: {e.Message}");
        }
    }
}