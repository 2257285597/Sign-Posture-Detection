using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Posture : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;

    //获取UI组件
    public GameObject GameObject;
    private void Update()
    {
        Debug.Log(CalculateAngle());

    }
    // 计算三个点之间的夹角
    public float CalculateAngle()
    {
        // 计算向量AB和向量BC
        Vector3 ab = pointB.position - pointA.position;
        Vector3 bc = pointC.position - pointB.position;

        // 归一化向量
        Vector3 abNormalized = ab.normalized;
        Vector3 bcNormalized = bc.normalized;

        // 计算点乘得到余弦值
        float dot = Vector3.Dot(abNormalized, bcNormalized);

        // 由于点乘得到的是余弦值，我们需要通过反余弦函数得到角度
        float angleInRadians = Mathf.Acos(dot);

        // 将弧度转换为度
        float angleInDegrees = angleInRadians * Mathf.Rad2Deg;

        if (angleInDegrees > 90f)
        {
            // 如果角度大于90度，激活组件
            GameObject.SetActive(true);
        }
        else
        {
            // 如果角度小于或等于90度，关闭组件
            GameObject.SetActive(false);
        }
        return angleInDegrees;
    }
}
