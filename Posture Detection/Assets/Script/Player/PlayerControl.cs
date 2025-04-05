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

    [Header("手势控制")]
    public GestureReceiver gestureReceiver; // 手势接收器引用
    public GameObject effect4Prefab; // 手势4的特效
    public GameObject effect5Prefab; // 手势5的特效
    public float gestureMoveSpeed = 3f; // 手势控制移动速度

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private float mouseX, mouseY;
    private int lastGesture = 0; // 上一个手势状态
    private bool isPlayingEffect = false; // 是否正在播放特效

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

        // 如果没有指定手势接收器，尝试查找
        if (gestureReceiver == null)
        {
            gestureReceiver = FindObjectOfType<GestureReceiver>();
            if (gestureReceiver == null)
            {
                Debug.LogWarning("未找到GestureReceiver，手势控制功能将不可用");
            }
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

        // 处理手势控制
        HandleGestureControl();

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

    // 处理手势控制
    private void HandleGestureControl()
    {
        if (gestureReceiver == null)
            return;

        // 通过反射获取当前手势值（因为currentGesture是私有的）
        int currentGesture = (int)GetPrivateFieldValue(gestureReceiver, "currentGesture");

        // 处理手势3：向前移动
        if (currentGesture == 3)
        {
            Vector3 forwardMove = transform.forward * gestureMoveSpeed * Time.deltaTime;
            controller.Move(forwardMove);
        }

        // 处理手势2：向后移动
        else if (currentGesture == 2)
        {
            Vector3 backwardMove = -transform.forward * gestureMoveSpeed * Time.deltaTime;
            controller.Move(backwardMove);
        }

        // 处理手势1：跳跃（只在检测到手势变化时触发一次）
        else if (currentGesture == 1 && lastGesture != 1 && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
        }

        // 处理手势4：播放特效4
        else if (currentGesture == 4 && lastGesture != 4 && !isPlayingEffect && effect4Prefab != null)
        {
            StartCoroutine(PlayEffect(effect4Prefab));
        }

        // 处理手势5：播放特效5
        else if (currentGesture == 5 && lastGesture != 5 && !isPlayingEffect && effect5Prefab != null)
        {
            StartCoroutine(PlayEffect(effect5Prefab));
        }

        // 更新上一个手势状态
        lastGesture = currentGesture;
    }

    // 播放特效的协程
    private IEnumerator PlayEffect(GameObject effectPrefab)
    {
        isPlayingEffect = true;

        // 实例化特效
        GameObject effectInstance = Instantiate(effectPrefab, transform.position + transform.forward + Vector3.up, Quaternion.identity);

        // 获取特效的ParticleSystem组件
        ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();

        // 如果有ParticleSystem组件，则等待其播放完毕
        if (ps != null)
        {
            yield return new WaitForSeconds(ps.main.duration);
        }
        else
        {
            // 如果没有ParticleSystem，默认等待2秒
            yield return new WaitForSeconds(2f);
        }

        // 销毁特效对象
        Destroy(effectInstance);
        isPlayingEffect = false;
    }

    // 获取私有字段的辅助方法
    private object GetPrivateFieldValue(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            return field.GetValue(obj);
        }

        return 0; // 默认返回0
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
