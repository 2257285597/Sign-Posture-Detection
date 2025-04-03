using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = 20f;

    [Header("地面检测")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("相机设置")]
    public Camera playerCamera; // 指定用于角色的相机
    public Transform cameraTarget;
    public float mouseSensitivity = 2f;
    public float upperLimit = 80f;
    public float lowerLimit = -80f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private float mouseX, mouseY;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 如果没有指定相机目标，则创建一个
        if (cameraTarget == null)
        {
            GameObject camTarget = new GameObject("CameraTarget");
            camTarget.transform.SetParent(transform);
            camTarget.transform.localPosition = new Vector3(0, 1f, 0); // 大约头部高度
            cameraTarget = camTarget.transform;
        }

        // 如果没有指定玩家相机，使用主相机作为引用（但不改变其父级）
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // 锁定并隐藏光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        // 地面检测
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // 如果在地面上且有向下的速度，重置速度
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // 稍微下压，确保与地面接触
        }

        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 计算移动方向（相对于角色朝向）
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
        }

        // 应用重力
        playerVelocity.y -= gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // 相机旋转控制
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 垂直旋转（上下看）- 限制范围
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, lowerLimit, upperLimit);
        cameraTarget.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 水平旋转（左右看）
        transform.Rotate(Vector3.up * mouseX);

        // 更新相机位置和旋转
        if (playerCamera != null)
        {
            // 设置相机位置和旋转，但不改变其父级关系
            playerCamera.transform.position = cameraTarget.position;
            playerCamera.transform.rotation = cameraTarget.rotation;
        }
    }

    // 用于调试的可视化
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
