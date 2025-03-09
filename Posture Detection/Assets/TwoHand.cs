using System.Collections;//（UDP1使用版本+Unity滤波）
using System.Collections.Generic;
using UnityEngine;

public class TowHand : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] leftHandPoints = new GameObject[22];  // 左手关节点
    public GameObject[] rightHandPoints = new GameObject[22]; // 右手关节点
    public GameObject LeftHand;
    public GameObject RightHand;

    // 在类内添加以下变量
    [Header("平滑参数")]
    [Range(0.1f, 0.9f)]
    public float smoothFactor = 0.5f; // 平滑系数（值越小越平滑）

    // 存储历史位置
    private Vector3[] leftHandPrevPos = new Vector3[21];
    private Vector3[] rightHandPrevPos = new Vector3[21];

    private void Start()
    {
        InitializeHand(LeftHand, leftHandPoints, "左手");
        InitializeHand(RightHand, rightHandPoints, "右手");
    }

    /// <summary>
    /// 手部关节初始化方法（保持原始Find实现）
    /// </summary>
    private void InitializeHand(GameObject handRoot, GameObject[] jointsArray, string handName)
    {
        if (handRoot == null)
        {
            Debug.LogError($"{handName}根物体未分配！");
            return;
        }

        // 查找Points子物体
        Transform pointsParent = handRoot.transform.Find("Points");
        if (pointsParent == null)
        {
            Debug.LogError($"{handName}找不到Points子物体！");
            return;
        }

        // 保持原始Find实现
        for (int i = 0; i < jointsArray.Length; i++)
        {
            // 注意：保持您原有的命名规则（示例使用Point0-Point20）
            Transform joint = pointsParent.Find($"Point{i}");

            if (joint != null)
            {
                jointsArray[i] = joint.gameObject;
                Debug.Log($"{handName}绑定关节[{i}]: {joint.name}");
            }
            else
            {
                Debug.LogError($"{handName}找不到关节：Point{i}");
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("手动执行绑定")]
    private void EditorBind()
    {
        InitializeHand(LeftHand, leftHandPoints, "左手");
        InitializeHand(RightHand, rightHandPoints, "右手");
        Debug.Log("手动绑定完成");
    }
#endif

    void UpdateHand(GameObject[] targetHand, Vector3 newPos, int index)
    {
        // 低通滤波计算
        Vector3 filteredPos = Vector3.Lerp(
            targetHand[index].transform.localPosition,
            newPos,
            smoothFactor
        );

        targetHand[index].transform.localPosition = filteredPos;
    }

    void Update()
    {
        string data = udpReceive.data;

        if (!string.IsNullOrEmpty(data))
        {
            // 清理数据格式
            data = data.Trim('[', ']');
            string[] points = data.Split(',');
            int totalPoints = points.Length;

            // 先隐藏所有手部模型
            SetHandActive(leftHandPoints, false);
            SetHandActive(rightHandPoints, false);

            // 计算检测到的手数量
            int handCount = totalPoints / 64; // 每只手64个数据（1类型+21点*3坐标）

            for (int h = 0; h < handCount; h++)
            {
                int startIndex = h * 64;

                // 解析手类型 (0=左手，1=右手)
                int handType = int.Parse(points[startIndex].Trim());

                // 选择对应手的关节点
                GameObject[] targetHand = handType == 0 ? leftHandPoints : rightHandPoints;

                // 激活当前手模型
                SetHandActive(targetHand, true);

                // 更新关节点位置
                for (int i = 0; i < 21; i++)
                {
                    int baseIndex = startIndex + 1 + i * 3;

                    float x = float.Parse(points[baseIndex]);
                    float y = float.Parse(points[baseIndex + 1]);
                    float z = float.Parse(points[baseIndex + 2]);

                    // 坐标转换（根据场景需要调整）
                    x = 7 - x / 100f;  // X轴镜像
                    y = y / 100f;
                    z = z / 100f;

                    Vector3 rawPos = new Vector3(x, y, z);
                    UpdateHand(targetHand, rawPos, i);
                }
            }
        }
    }
    // 控制手部模型显示/隐藏
    void SetHandActive(GameObject[] hand, bool state)
    {
        foreach (GameObject point in hand)
        {
            point.SetActive(state);
        }
    }
}
//using System;//（Unity端口滤波版本配合UDP2使用）
//using System.Linq;
//using UnityEngine;

//public class TwoHandsController : MonoBehaviour
//{
//    public UDPReceive2 udpReceive;
//    public GameObject[] leftHandPoints;  // 左手关节点
//    public GameObject[] rightHandPoints; // 右手关节点
//    //低通滤波参数
//    [Header("Smoothing")]
//    [Range(0.1f, 0.9f)]
//    public float smoothFactor = 0.5f;

//    private Vector3[] leftHandPrev = new Vector3[21];
//    private Vector3[] rightHandPrev = new Vector3[21];

//    void Update()
//    {
//        if (udpReceive == null || udpReceive.handData == null) return;

//        float[] receivedData = udpReceive.handData;
//        int dataLength = receivedData.Length;

//        // 隐藏所有手部模型
//        SetHandActive(leftHandPoints, false);
//        SetHandActive(rightHandPoints, false);

//        // 计算检测到的手数量 (64 = 1手类型 + 21点*3坐标)
//        int handCount = dataLength / 64;
//        handCount = Mathf.Clamp(handCount, 0, 2);

//        for (int h = 0; h < handCount; h++)
//        {
//            int startIndex = h * 64;

//            // 解析手类型 (0=左手，1=右手)
//            int handType = (int)receivedData[startIndex];
//            bool isLeftHand = handType == 0;

//            GameObject[] targetHand = isLeftHand ? leftHandPoints : rightHandPoints;
//            Vector3[] prevPositions = isLeftHand ? leftHandPrev : rightHandPrev;

//            // 激活当前手模型
//            SetHandActive(targetHand, true);

//            // 更新关节点位置
//            for (int i = 0; i < 21; i++)
//            {
//                int dataIndex = startIndex + 1 + i * 3;

//                if (dataIndex + 2 >= dataLength) continue;

//                // 获取原始坐标
//                float x = receivedData[dataIndex];
//                float y = receivedData[dataIndex + 1];
//                float z = receivedData[dataIndex + 2];

//                // 坐标转换
//                Vector3 rawPos = new Vector3(
//                    7 - x / 100f,  // X轴镜像
//                    y / 100f,
//                    z / 100f
//                );

//                // 应用平滑
//                if (targetHand[i] != null)
//                {
//                    Vector3 smoothedPos = Vector3.Lerp(
//                        prevPositions[i],
//                        rawPos,
//                        smoothFactor
//                    );

//                    targetHand[i].transform.localPosition = smoothedPos;
//                    prevPositions[i] = smoothedPos;
//                }
//            }
//        }
//    }

//    void SetHandActive(GameObject[] hand, bool state)
//    {
//        foreach (var point in hand.Where(p => p != null))
//        {
//            point.SetActive(state);
//        }
//    }
//}
//using System.Linq;//（前端Py滤波版本）
//using UnityEngine;

//public class TwoHandsController : MonoBehaviour
//{
//    public UDPReceive2 udpReceive;
//    public GameObject[] leftHandPoints;  // 左手关节点预制体
//    public GameObject[] rightHandPoints; // 右手关节点预制体

//    void Update()
//    {
//        if (udpReceive == null || udpReceive.handData == null) return;

//        float[] receivedData = udpReceive.handData;
//        int dataLength = receivedData.Length;

//        隐藏所有手部模型
//        SetHandActive(leftHandPoints, false);
//        SetHandActive(rightHandPoints, false);

//        计算检测到的手数量(64 = 1手类型 + 21点 * 3坐标)
//        int handCount = Mathf.Clamp(dataLength / 64, 0, 2);

//        for (int h = 0; h < handCount; h++)
//        {
//            int startIndex = h * 64;

//            解析手类型并获取对应关节点
//            bool isLeftHand = receivedData[startIndex] == 0;
//            var targetHand = isLeftHand ? leftHandPoints : rightHandPoints;

//            激活当前手模型
//            SetHandActive(targetHand, true);

//            更新关节点位置（直接使用滤波后的坐标）
//            for (int i = 0; i < 21; i++)
//            {
//                int dataIndex = startIndex + 1 + i * 3;

//                if (dataIndex + 2 >= dataLength) continue;

//                targetHand[i].transform.localPosition = new Vector3(
//                    7 - receivedData[dataIndex] / 100f,   // X轴镜像
//                    receivedData[dataIndex + 1] / 100f,    // Y轴
//                    receivedData[dataIndex + 2] / 100f     // Z轴
//                );
//            }
//        }
//    }

//    void SetHandActive(GameObject[] hand, bool state)
//    {
//        foreach (var point in hand.Where(p => p != null))
//        {
//            point.SetActive(state);
//        }
//    }
//}