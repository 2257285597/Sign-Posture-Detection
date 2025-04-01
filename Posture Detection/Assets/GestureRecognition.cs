using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureRecognition : MonoBehaviour
{
    // 引用TowHand脚本来获取手部关键点数据
    public TowHand handTracking;

    // 手势类型枚举
    public enum HandGesture
    {
        None,           // 无法识别的手势
        OpenPalm,       // 张开手掌
        Fist,           // 握拳
        PointingIndex,  // 食指指向
        OkSign,         // OK手势
        Victory         // V手势
    }

    // 存储当前识别的左右手手势
    public HandGesture leftHandGesture = HandGesture.None;
    public HandGesture rightHandGesture = HandGesture.None;

    // 手势识别阈值参数
    [Header("手势识别参数")]
    [Range(0.01f, 0.1f)]
    public float fingerExtendedThreshold = 0.04f;  // 判断手指是否伸展的阈值
    [Range(0.01f, 0.1f)]
    public float fingerCloseThreshold = 0.025f;    // 判断手指是否弯曲的阈值

    // 手指关节索引常量 (MediaPipe标准索引)
    private static readonly int WRIST = 0;        // 手腕
    private static readonly int THUMB_CMC = 1;    // 大拇指腕掌关节
    private static readonly int THUMB_MCP = 2;    // 大拇指掌指关节
    private static readonly int THUMB_IP = 3;     // 大拇指指间关节
    private static readonly int THUMB_TIP = 4;    // 大拇指指尖
    private static readonly int INDEX_MCP = 5;    // 食指掌指关节
    private static readonly int INDEX_PIP = 6;    // 食指近端指间关节
    private static readonly int INDEX_DIP = 7;    // 食指远端指间关节
    private static readonly int INDEX_TIP = 8;    // 食指指尖
    private static readonly int MIDDLE_MCP = 9;   // 中指掌指关节
    private static readonly int MIDDLE_PIP = 10;  // 中指近端指间关节
    private static readonly int MIDDLE_DIP = 11;  // 中指远端指间关节
    private static readonly int MIDDLE_TIP = 12;  // 中指指尖
    private static readonly int RING_MCP = 13;    // 无名指掌指关节
    private static readonly int RING_PIP = 14;    // 无名指近端指间关节
    private static readonly int RING_DIP = 15;    // 无名指远端指间关节
    private static readonly int RING_TIP = 16;    // 无名指指尖
    private static readonly int PINKY_MCP = 17;   // 小指掌指关节
    private static readonly int PINKY_PIP = 18;   // 小指近端指间关节
    private static readonly int PINKY_DIP = 19;   // 小指远端指间关节
    private static readonly int PINKY_TIP = 20;   // 小指指尖

    void Start()
    {
        // 确保已分配手部跟踪脚本
        if (handTracking == null)
        {
            handTracking = FindObjectOfType<TowHand>();
            if (handTracking == null)
            {
                Debug.LogError("找不到TowHand脚本，请手动分配引用！");
            }
        }
    }

    void Update()
    {
        // 识别左手手势（如果手部可见）
        if (IsHandVisible(handTracking.leftHandPoints))
        {
            leftHandGesture = RecognizeHandGesture(handTracking.leftHandPoints);
        }
        else
        {
            leftHandGesture = HandGesture.None;
        }

        // 识别右手手势（如果手部可见）
        if (IsHandVisible(handTracking.rightHandPoints))
        {
            rightHandGesture = RecognizeHandGesture(handTracking.rightHandPoints);
        }
        else
        {
            rightHandGesture = HandGesture.None;
        }

        // 输出手势调试信息
        Debug.Log($"左手手势: {leftHandGesture}, 右手手势: {rightHandGesture}");
    }

    // 检查手部是否可见（通过检查手腕点是否激活）
    private bool IsHandVisible(GameObject[] handPoints)
    {
        return handPoints != null && handPoints.Length > 0 && handPoints[WRIST] != null && handPoints[WRIST].activeSelf;
    }

    // 手势识别主函数
    private HandGesture RecognizeHandGesture(GameObject[] handPoints)
    {
        // 检查是否是张开手掌：所有手指伸直
        if (IsAllFingersExtended(handPoints))
        {
            return HandGesture.OpenPalm;
        }

        // 检查是否是握拳：所有手指都弯曲
        if (IsAllFingersClosed(handPoints))
        {
            return HandGesture.Fist;
        }

        // 检查是否是食指指向：只有食指伸出
        if (IsFingerExtended(handPoints, INDEX_MCP, INDEX_PIP, INDEX_DIP, INDEX_TIP) &&
            !IsFingerExtended(handPoints, MIDDLE_MCP, MIDDLE_PIP, MIDDLE_DIP, MIDDLE_TIP) &&
            !IsFingerExtended(handPoints, RING_MCP, RING_PIP, RING_DIP, RING_TIP) &&
            !IsFingerExtended(handPoints, PINKY_MCP, PINKY_PIP, PINKY_DIP, PINKY_TIP))
        {
            return HandGesture.PointingIndex;
        }

        // 检查是否是OK手势：大拇指和食指形成圈
        if (IsOkSign(handPoints))
        {
            return HandGesture.OkSign;
        }

        // 检查是否是V手势：食指和中指伸出成V形
        if (IsFingerExtended(handPoints, INDEX_MCP, INDEX_PIP, INDEX_DIP, INDEX_TIP) &&
            IsFingerExtended(handPoints, MIDDLE_MCP, MIDDLE_PIP, MIDDLE_DIP, MIDDLE_TIP) &&
            !IsFingerExtended(handPoints, RING_MCP, RING_PIP, RING_DIP, RING_TIP) &&
            !IsFingerExtended(handPoints, PINKY_MCP, PINKY_PIP, PINKY_DIP, PINKY_TIP))
        {
            return HandGesture.Victory;
        }

        // 无法识别为任何预定义手势
        return HandGesture.None;
    }

    // 判断特定手指是否伸展
    private bool IsFingerExtended(GameObject[] handPoints, int mcpIdx, int pipIdx, int dipIdx, int tipIdx)
    {
        // 计算手指伸直程度：指尖到掌指关节的距离是否大于阈值
        float distance = Vector3.Distance(handPoints[tipIdx].transform.position, handPoints[mcpIdx].transform.position);
        float palmSize = Vector3.Distance(handPoints[INDEX_MCP].transform.position, handPoints[PINKY_MCP].transform.position);

        // 根据手掌大小进行归一化判断
        return distance > palmSize * fingerExtendedThreshold;
    }

    // 判断特定手指是否弯曲
    private bool IsFingerClosed(GameObject[] handPoints, int mcpIdx, int pipIdx, int dipIdx, int tipIdx)
    {
        // 计算指尖到手掌的距离
        float distance = Vector3.Distance(handPoints[tipIdx].transform.position, handPoints[WRIST].transform.position);
        float palmSize = Vector3.Distance(handPoints[INDEX_MCP].transform.position, handPoints[PINKY_MCP].transform.position);

        // 如果指尖距离手掌很近，说明手指弯曲
        return distance < palmSize * fingerCloseThreshold;
    }

    // 判断是否所有手指都伸展
    private bool IsAllFingersExtended(GameObject[] handPoints)
    {
        return IsFingerExtended(handPoints, INDEX_MCP, INDEX_PIP, INDEX_DIP, INDEX_TIP) &&
               IsFingerExtended(handPoints, MIDDLE_MCP, MIDDLE_PIP, MIDDLE_DIP, MIDDLE_TIP) &&
               IsFingerExtended(handPoints, RING_MCP, RING_PIP, RING_DIP, RING_TIP) &&
               IsFingerExtended(handPoints, PINKY_MCP, PINKY_PIP, PINKY_DIP, PINKY_TIP);
    }

    // 判断是否所有手指都弯曲
    private bool IsAllFingersClosed(GameObject[] handPoints)
    {
        return IsFingerClosed(handPoints, INDEX_MCP, INDEX_PIP, INDEX_DIP, INDEX_TIP) &&
               IsFingerClosed(handPoints, MIDDLE_MCP, MIDDLE_PIP, MIDDLE_DIP, MIDDLE_TIP) &&
               IsFingerClosed(handPoints, RING_MCP, RING_PIP, RING_DIP, RING_TIP) &&
               IsFingerClosed(handPoints, PINKY_MCP, PINKY_PIP, PINKY_DIP, PINKY_TIP);
    }

    // 判断是否是OK手势（大拇指和食指形成圈）
    private bool IsOkSign(GameObject[] handPoints)
    {
        // 计算大拇指指尖和食指指尖之间的距离
        float distance = Vector3.Distance(handPoints[THUMB_TIP].transform.position, handPoints[INDEX_TIP].transform.position);
        float palmSize = Vector3.Distance(handPoints[INDEX_MCP].transform.position, handPoints[PINKY_MCP].transform.position);

        // 如果距离很小，且其他手指伸展，则判定为OK手势
        return distance < palmSize * fingerCloseThreshold * 1.2f &&
               IsFingerExtended(handPoints, MIDDLE_MCP, MIDDLE_PIP, MIDDLE_DIP, MIDDLE_TIP) &&
               IsFingerExtended(handPoints, RING_MCP, RING_PIP, RING_DIP, RING_TIP) &&
               IsFingerExtended(handPoints, PINKY_MCP, PINKY_PIP, PINKY_DIP, PINKY_TIP);
    }
}
