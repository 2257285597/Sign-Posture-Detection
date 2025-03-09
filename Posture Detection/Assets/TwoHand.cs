using System.Collections;//��UDP1ʹ�ð汾+Unity�˲���
using System.Collections.Generic;
using UnityEngine;

public class TowHand : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] leftHandPoints = new GameObject[22];  // ���ֹؽڵ�
    public GameObject[] rightHandPoints = new GameObject[22]; // ���ֹؽڵ�
    public GameObject LeftHand;
    public GameObject RightHand;

    // ������������±���
    [Header("ƽ������")]
    [Range(0.1f, 0.9f)]
    public float smoothFactor = 0.5f; // ƽ��ϵ����ֵԽСԽƽ����

    // �洢��ʷλ��
    private Vector3[] leftHandPrevPos = new Vector3[21];
    private Vector3[] rightHandPrevPos = new Vector3[21];

    private void Start()
    {
        InitializeHand(LeftHand, leftHandPoints, "����");
        InitializeHand(RightHand, rightHandPoints, "����");
    }

    /// <summary>
    /// �ֲ��ؽڳ�ʼ������������ԭʼFindʵ�֣�
    /// </summary>
    private void InitializeHand(GameObject handRoot, GameObject[] jointsArray, string handName)
    {
        if (handRoot == null)
        {
            Debug.LogError($"{handName}������δ���䣡");
            return;
        }

        // ����Points������
        Transform pointsParent = handRoot.transform.Find("Points");
        if (pointsParent == null)
        {
            Debug.LogError($"{handName}�Ҳ���Points�����壡");
            return;
        }

        // ����ԭʼFindʵ��
        for (int i = 0; i < jointsArray.Length; i++)
        {
            // ע�⣺������ԭ�е���������ʾ��ʹ��Point0-Point20��
            Transform joint = pointsParent.Find($"Point{i}");

            if (joint != null)
            {
                jointsArray[i] = joint.gameObject;
                Debug.Log($"{handName}�󶨹ؽ�[{i}]: {joint.name}");
            }
            else
            {
                Debug.LogError($"{handName}�Ҳ����ؽڣ�Point{i}");
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("�ֶ�ִ�а�")]
    private void EditorBind()
    {
        InitializeHand(LeftHand, leftHandPoints, "����");
        InitializeHand(RightHand, rightHandPoints, "����");
        Debug.Log("�ֶ������");
    }
#endif

    void UpdateHand(GameObject[] targetHand, Vector3 newPos, int index)
    {
        // ��ͨ�˲�����
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
            // �������ݸ�ʽ
            data = data.Trim('[', ']');
            string[] points = data.Split(',');
            int totalPoints = points.Length;

            // �����������ֲ�ģ��
            SetHandActive(leftHandPoints, false);
            SetHandActive(rightHandPoints, false);

            // �����⵽��������
            int handCount = totalPoints / 64; // ÿֻ��64�����ݣ�1����+21��*3���꣩

            for (int h = 0; h < handCount; h++)
            {
                int startIndex = h * 64;

                // ���������� (0=���֣�1=����)
                int handType = int.Parse(points[startIndex].Trim());

                // ѡ���Ӧ�ֵĹؽڵ�
                GameObject[] targetHand = handType == 0 ? leftHandPoints : rightHandPoints;

                // ���ǰ��ģ��
                SetHandActive(targetHand, true);

                // ���¹ؽڵ�λ��
                for (int i = 0; i < 21; i++)
                {
                    int baseIndex = startIndex + 1 + i * 3;

                    float x = float.Parse(points[baseIndex]);
                    float y = float.Parse(points[baseIndex + 1]);
                    float z = float.Parse(points[baseIndex + 2]);

                    // ����ת�������ݳ�����Ҫ������
                    x = 7 - x / 100f;  // X�᾵��
                    y = y / 100f;
                    z = z / 100f;

                    Vector3 rawPos = new Vector3(x, y, z);
                    UpdateHand(targetHand, rawPos, i);
                }
            }
        }
    }
    // �����ֲ�ģ����ʾ/����
    void SetHandActive(GameObject[] hand, bool state)
    {
        foreach (GameObject point in hand)
        {
            point.SetActive(state);
        }
    }
}
//using System;//��Unity�˿��˲��汾���UDP2ʹ�ã�
//using System.Linq;
//using UnityEngine;

//public class TwoHandsController : MonoBehaviour
//{
//    public UDPReceive2 udpReceive;
//    public GameObject[] leftHandPoints;  // ���ֹؽڵ�
//    public GameObject[] rightHandPoints; // ���ֹؽڵ�
//    //��ͨ�˲�����
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

//        // ���������ֲ�ģ��
//        SetHandActive(leftHandPoints, false);
//        SetHandActive(rightHandPoints, false);

//        // �����⵽�������� (64 = 1������ + 21��*3����)
//        int handCount = dataLength / 64;
//        handCount = Mathf.Clamp(handCount, 0, 2);

//        for (int h = 0; h < handCount; h++)
//        {
//            int startIndex = h * 64;

//            // ���������� (0=���֣�1=����)
//            int handType = (int)receivedData[startIndex];
//            bool isLeftHand = handType == 0;

//            GameObject[] targetHand = isLeftHand ? leftHandPoints : rightHandPoints;
//            Vector3[] prevPositions = isLeftHand ? leftHandPrev : rightHandPrev;

//            // ���ǰ��ģ��
//            SetHandActive(targetHand, true);

//            // ���¹ؽڵ�λ��
//            for (int i = 0; i < 21; i++)
//            {
//                int dataIndex = startIndex + 1 + i * 3;

//                if (dataIndex + 2 >= dataLength) continue;

//                // ��ȡԭʼ����
//                float x = receivedData[dataIndex];
//                float y = receivedData[dataIndex + 1];
//                float z = receivedData[dataIndex + 2];

//                // ����ת��
//                Vector3 rawPos = new Vector3(
//                    7 - x / 100f,  // X�᾵��
//                    y / 100f,
//                    z / 100f
//                );

//                // Ӧ��ƽ��
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
//using System.Linq;//��ǰ��Py�˲��汾��
//using UnityEngine;

//public class TwoHandsController : MonoBehaviour
//{
//    public UDPReceive2 udpReceive;
//    public GameObject[] leftHandPoints;  // ���ֹؽڵ�Ԥ����
//    public GameObject[] rightHandPoints; // ���ֹؽڵ�Ԥ����

//    void Update()
//    {
//        if (udpReceive == null || udpReceive.handData == null) return;

//        float[] receivedData = udpReceive.handData;
//        int dataLength = receivedData.Length;

//        ���������ֲ�ģ��
//        SetHandActive(leftHandPoints, false);
//        SetHandActive(rightHandPoints, false);

//        �����⵽��������(64 = 1������ + 21�� * 3����)
//        int handCount = Mathf.Clamp(dataLength / 64, 0, 2);

//        for (int h = 0; h < handCount; h++)
//        {
//            int startIndex = h * 64;

//            ���������Ͳ���ȡ��Ӧ�ؽڵ�
//            bool isLeftHand = receivedData[startIndex] == 0;
//            var targetHand = isLeftHand ? leftHandPoints : rightHandPoints;

//            ���ǰ��ģ��
//            SetHandActive(targetHand, true);

//            ���¹ؽڵ�λ�ã�ֱ��ʹ���˲�������꣩
//            for (int i = 0; i < 21; i++)
//            {
//                int dataIndex = startIndex + 1 + i * 3;

//                if (dataIndex + 2 >= dataLength) continue;

//                targetHand[i].transform.localPosition = new Vector3(
//                    7 - receivedData[dataIndex] / 100f,   // X�᾵��
//                    receivedData[dataIndex + 1] / 100f,    // Y��
//                    receivedData[dataIndex + 2] / 100f     // Z��
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