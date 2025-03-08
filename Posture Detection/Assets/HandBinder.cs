using System.Drawing;
using UnityEngine;

public class RobustHandBinder : MonoBehaviour
{
   public GameObject LeftHand;
    public GameObject RightHand;

    private void Start()
    {
        LeftHand=GetComponent<GameObject>();
        RightHand=GetComponent<GameObject>();

        Transform Points = LeftHand.transform.Find("Points");
        GameObject points = Points.gameObject;

    }
}