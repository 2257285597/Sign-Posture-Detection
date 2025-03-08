using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Posture : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;

    //��ȡUI���
    public GameObject GameObject;
    private void Update()
    {
        Debug.Log(CalculateAngle());

    }
    // ����������֮��ļн�
    public float CalculateAngle()
    {
        // ��������AB������BC
        Vector3 ab = pointB.position - pointA.position;
        Vector3 bc = pointC.position - pointB.position;

        // ��һ������
        Vector3 abNormalized = ab.normalized;
        Vector3 bcNormalized = bc.normalized;

        // �����˵õ�����ֵ
        float dot = Vector3.Dot(abNormalized, bcNormalized);

        // ���ڵ�˵õ���������ֵ��������Ҫͨ�������Һ����õ��Ƕ�
        float angleInRadians = Mathf.Acos(dot);

        // ������ת��Ϊ��
        float angleInDegrees = angleInRadians * Mathf.Rad2Deg;

        if (angleInDegrees > 90f)
        {
            // ����Ƕȴ���90�ȣ��������
            GameObject.SetActive(true);
        }
        else
        {
            // ����Ƕ�С�ڻ����90�ȣ��ر����
            GameObject.SetActive(false);
        }
        return angleInDegrees;
    }
}
