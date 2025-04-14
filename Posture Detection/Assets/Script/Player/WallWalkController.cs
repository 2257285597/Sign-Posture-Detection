using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallWalkController : MonoBehaviour
{
    [Header("墙面行走设置")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = 20f;
    public float wallDetectionDistance = 0.6f;
    public float wallSwitchCooldown = 0.3f;
    public LayerMask wallLayer;
    public bool autoStickToWalls = false; // 是否自动吸附到墙面
    public float wallStickForce = 10f;    // 使角色吸附在墙上的力度

    [Header("地面检测")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public float wallGroundedCheckDistance = 0.5f; // 墙面上的接地检测距离

    [Header("相机设置")]
    public Camera playerCamera;
    public Transform cameraTarget;
    public float mouseSensitivity = 2f;
    public float upperLimit = 80f;
    public float lowerLimit = -80f;

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool showDebugRays = true;

    // 私有变量
    private CharacterController controller;
    private Vector3 playerVelocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private float mouseX, mouseY;

    // 墙面行走相关
    private Vector3 currentGravityDirection = Vector3.down;
    private Vector3 targetGravityDirection = Vector3.down;
    private bool isOnWall = false;
    private Vector3 wallNormal;
    private float lastWallSwitchTime = 0f;
    private Transform originalParent;
    private Quaternion targetRotation;
    private float gravityTransitionSpeed = 8f;
    private bool isWallGrounded = false;  // 是否在墙上接地
    private Vector3 lastWallNormal;       // 上一次检测到的墙面法线
    private Vector3 cameraForward;        // 相机前方向
    private Vector3 cameraRight;          // 相机右方向

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("WallWalkController需要CharacterController组件!");
            enabled = false;
            return;
        }

        // 记录原始父级
        originalParent = transform.parent;

        // 相机目标设置
        if (cameraTarget == null)
        {
            GameObject camTarget = new GameObject("WallWalker_CameraTarget");
            camTarget.transform.SetParent(transform);
            camTarget.transform.localPosition = new Vector3(0, 1.6f, 0);
            cameraTarget = camTarget.transform;
        }

        // 相机设置
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // 地面检测设置
        if (groundCheck == null)
        {
            GameObject check = new GameObject("WallWalker_GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheck = check.transform;
            Debug.Log("为WallWalkController自动创建了GroundCheck");
        }

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 检测墙面
        DetectWalls();

        // 更新重力方向
        UpdateGravityDirection();

        // 地面检测 - 考虑当前重力方向
        CheckGroundedState();

        // 处理玩家输入
        HandleInput();

        // 应用重力和墙面吸附力
        ApplyPhysics();

        // 更新相机
        UpdateCamera();

        // 更新相机相对方向向量 - 用于移动计算
        UpdateCameraRelativeDirections();
    }

    void UpdateCameraRelativeDirections()
    {
        // 使用相机的前方和右方向作为移动的参考方向
        if (playerCamera != null)
        {
            // 获取相机的前向和右向，但忽略Y轴分量，以保持在水平面上移动
            cameraForward = Vector3.ProjectOnPlane(playerCamera.transform.forward, -currentGravityDirection).normalized;
            cameraRight = Vector3.ProjectOnPlane(playerCamera.transform.right, -currentGravityDirection).normalized;

            // 如果投影后长度太小，使用备选方案
            if (cameraForward.magnitude < 0.1f)
            {
                Vector3 right = Vector3.Cross(Vector3.forward, -currentGravityDirection).normalized;
                cameraForward = Vector3.Cross(right, -currentGravityDirection).normalized;
            }
            if (cameraRight.magnitude < 0.1f)
            {
                cameraRight = Vector3.Cross(-currentGravityDirection, cameraForward).normalized;
            }

            // 调试
            if (showDebugRays)
            {
                Debug.DrawRay(transform.position, cameraForward * 2f, Color.blue);
                Debug.DrawRay(transform.position, cameraRight * 2f, Color.red);
            }
        }
    }

    void CheckGroundedState()
    {
        if (groundCheck == null) return;

        // 默认设置为非接地状态
        isGrounded = false;
        isWallGrounded = false;

        RaycastHit hit;

        // 正常地面检测
        if (!isOnWall)
        {
            if (Physics.SphereCast(groundCheck.position, groundDistance, currentGravityDirection, out hit, 0.2f, groundMask))
            {
                isGrounded = true;
                if (Vector3.Dot(playerVelocity, currentGravityDirection) > 0)
                {
                    playerVelocity = currentGravityDirection * -0.1f;
                }
            }
        }
        // 墙面接地检测 - 直接检测当前墙面，因为我们应该始终"接地"在墙上
        else
        {
            // 检查是否接触墙面
            if (Physics.Raycast(transform.position, -wallNormal, out hit, wallDetectionDistance * 1.2f, wallLayer))
            {
                isWallGrounded = true;
                isGrounded = true;

                if (showDebugRays)
                {
                    Debug.DrawRay(hit.point, hit.normal, Color.green);
                }
            }

            // 如果不在墙面上，尝试多方向检测
            if (!isWallGrounded)
            {
                // 多方向检测，确保在墙面的任何方向都能检测到"地面"
                Vector3[] checkDirections = new Vector3[4];

                // 构建以当前重力方向为基础的四个方向
                Vector3 right = Vector3.Cross(Vector3.forward, -currentGravityDirection).normalized;
                Vector3 forward = Vector3.Cross(right, -currentGravityDirection).normalized;

                checkDirections[0] = forward;
                checkDirections[1] = -forward;
                checkDirections[2] = right;
                checkDirections[3] = -right;

                // 检查所有方向
                foreach (Vector3 dir in checkDirections)
                {
                    if (showDebugRays)
                    {
                        Debug.DrawRay(transform.position, dir * wallGroundedCheckDistance, Color.magenta);
                    }

                    if (Physics.Raycast(transform.position, dir, out hit, wallGroundedCheckDistance, wallLayer))
                    {
                        // 检查这个表面是否与当前墙面平行(法线方向相似)
                        if (Vector3.Dot(hit.normal, wallNormal) > 0.7f)
                        {
                            isWallGrounded = true;
                            isGrounded = true; // 同时设置常规接地状态为true

                            // 调整重力方向以匹配这个墙面
                            targetGravityDirection = -hit.normal;
                            lastWallNormal = hit.normal;

                            if (showDebugRays)
                            {
                                Debug.DrawRay(hit.point, hit.normal * 1f, Color.blue);
                            }

                            break;
                        }
                    }
                }
            }
        }
    }

    void DetectWalls()
    {
        // 如果当前在墙上且接地，持续检测当前墙面
        if (isOnWall)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -wallNormal, out hit, wallDetectionDistance * 1.2f, wallLayer))
            {
                // 更新墙面法线
                wallNormal = hit.normal;
                targetGravityDirection = -hit.normal;
                lastWallNormal = hit.normal;
                return; // 继续保持在当前墙面
            }
            else
            {
                // 如果离开了墙面，恢复正常重力
                SwitchToNormal();
                return;
            }
        }

        // 常规墙面检测
        if (!autoStickToWalls && !Input.GetButtonDown("Jump")) return;

        RaycastHit hit2;
        Vector3[] directions = {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        bool foundWall = false;

        foreach (Vector3 direction in directions)
        {
            // 显示调试射线
            if (showDebugRays)
            {
                Debug.DrawRay(transform.position, direction * wallDetectionDistance, Color.yellow);
            }

            if (Physics.Raycast(transform.position, direction, out hit2, wallDetectionDistance, wallLayer))
            {
                foundWall = true;

                // 显示法线方向
                if (showDebugRays)
                {
                    Debug.DrawRay(hit2.point, hit2.normal * 1f, Color.green);
                }

                // 如果是自动吸附或按下跳跃键，且冷却时间已过
                if ((autoStickToWalls || Input.GetButtonDown("Jump")) &&
                    Time.time > lastWallSwitchTime + wallSwitchCooldown)
                {
                    SwitchToWall(hit2);
                }

                break;
            }
        }
    }

    void SwitchToWall(RaycastHit hit)
    {
        if (!isOnWall || Vector3.Dot(targetGravityDirection, -hit.normal) < 0.9f)
        {
            isOnWall = true;
            lastWallSwitchTime = Time.time;

            // 设置目标重力方向为墙面法线的反方向
            targetGravityDirection = -hit.normal;
            wallNormal = hit.normal;
            lastWallNormal = hit.normal;

            // 计算目标旋转 - 保持角色的上方朝向为世界"上"方向
            Vector3 upDirection = Vector3.up;
            Vector3 forwardDirection = Vector3.Cross(hit.normal, upDirection).normalized;
            if (forwardDirection.magnitude < 0.01f)
            {
                // 如果墙面法线与世界上方向平行，使用角色当前的前方向
                forwardDirection = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
            }
            Vector3 rightDirection = Vector3.Cross(forwardDirection, -hit.normal).normalized;
            forwardDirection = Vector3.Cross(-hit.normal, rightDirection).normalized;

            targetRotation = Quaternion.LookRotation(forwardDirection, -hit.normal);

            // 临时禁用控制器以避免碰撞问题
            StartCoroutine(DisableAndReenableController());

            // 立即将重力方向设置为墙面法线方向，防止滑落
            currentGravityDirection = targetGravityDirection;

            // 消除与墙面法线方向相反的速度分量，防止弹开
            playerVelocity = Vector3.ProjectOnPlane(playerVelocity, wallNormal);
        }
    }

    IEnumerator DisableAndReenableController()
    {
        if (controller != null)
        {
            controller.enabled = false;
            yield return new WaitForFixedUpdate();
            controller.enabled = true;
        }
    }

    void SwitchToNormal()
    {
        if (isOnWall)
        {
            isOnWall = false;
            isWallGrounded = false;
            lastWallSwitchTime = Time.time;
            targetGravityDirection = Vector3.down;

            // 计算回到正常模式的旋转 - 保持当前的前方向，但改变上方向为世界上方向
            Vector3 currentForward = transform.forward;
            targetRotation = Quaternion.LookRotation(currentForward, Vector3.up);
        }
    }

    void UpdateGravityDirection()
    {
        // 平滑过渡到目标重力方向
        currentGravityDirection = Vector3.Slerp(currentGravityDirection, targetGravityDirection, Time.deltaTime * gravityTransitionSpeed);

        // 平滑过渡到目标旋转
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * gravityTransitionSpeed);
    }

    void HandleInput()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 计算移动方向 - 使用相机相对方向而非角色相对方向
        Vector3 move;
        if (isOnWall)
        {
            // 在墙上行走时使用相机相对方向
            move = cameraRight * horizontal + cameraForward * vertical;

            // 移动方向投影到墙面上，确保不会脱离墙面
            move = Vector3.ProjectOnPlane(move, wallNormal).normalized;

            // 调试 - 显示最终移动方向
            if (showDebugRays && move.magnitude > 0.1f)
            {
                Debug.DrawRay(transform.position, move * 2f, Color.green);
            }
        }
        else
        {
            // 正常移动 - 也使用相机相对方向
            Vector3 camForwardGround = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            Vector3 camRightGround = Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up).normalized;

            move = camRightGround * horizontal + camForwardGround * vertical;
        }

        // 应用移动 - 只有在有输入的情况下才移动
        if (move.magnitude > 0.1f)
        {
            controller.Move(move * moveSpeed * Time.deltaTime);
        }

        // 处理跳跃 - 只在非墙面切换状态且接地时处理
        if (Input.GetButtonDown("Jump") && isGrounded && Time.time > lastWallSwitchTime + wallSwitchCooldown)
        {
            // 如果在墙上且按下跳跃，就从墙上跳离
            if (isOnWall)
            {
                // 从墙上跳离 - 施加一个与墙面法线方向相同的力
                playerVelocity = wallNormal * jumpForce * 0.5f;
                playerVelocity += -currentGravityDirection * Mathf.Sqrt(jumpForce * 2f * gravity);
                // 立即切换回正常模式
                SwitchToNormal();
            }
            else
            {
                // 普通跳跃 - 与当前重力方向相反
                playerVelocity = -currentGravityDirection * Mathf.Sqrt(jumpForce * 2f * gravity);
            }
        }
    }

    void ApplyPhysics()
    {
        if (isOnWall)
        {
            // 在墙上时应用墙面吸附力和较小的重力

            // 1. 向墙面施加吸附力
            Vector3 stickForce = -wallNormal * wallStickForce * Time.deltaTime;
            controller.Move(stickForce);

            // 2. 应用较小的垂直方向上的重力 (墙面上滑落的效果)
            if (!isWallGrounded)
            {
                // 墙面上的重力只有正常重力的一小部分，防止快速滑落
                playerVelocity += currentGravityDirection * (gravity * 0.3f) * Time.deltaTime;
            }
            else
            {
                // 在墙面接地状态下，消除与墙面垂直的速度
                playerVelocity = Vector3.ProjectOnPlane(playerVelocity, -currentGravityDirection);
            }
        }
        else
        {
            // 正常重力
            playerVelocity += currentGravityDirection * gravity * Time.deltaTime;
        }

        // 限制最大下落速度
        float maxFallSpeed = 30f;
        float currentFallSpeed = Vector3.Dot(playerVelocity, currentGravityDirection);
        if (currentFallSpeed > maxFallSpeed)
        {
            // 分解速度向量，只限制重力方向的分量
            Vector3 gravityComponent = Vector3.Project(playerVelocity, currentGravityDirection);
            Vector3 lateralComponent = playerVelocity - gravityComponent;
            playerVelocity = lateralComponent + currentGravityDirection * maxFallSpeed;
        }

        // 应用最终速度
        controller.Move(playerVelocity * Time.deltaTime);
    }

    void UpdateCamera()
    {
        // 获取鼠标输入
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 垂直旋转（上下看）- 限制范围
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, lowerLimit, upperLimit);
        cameraTarget.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 水平旋转（左右看）
        transform.Rotate(Vector3.up * mouseX, Space.Self);

        // 更新相机位置和旋转
        if (playerCamera != null)
        {
            playerCamera.transform.position = cameraTarget.position;
            playerCamera.transform.rotation = cameraTarget.rotation;
        }
    }

    void OnDrawGizmosSelected()
    {
        // 显示地面检测范围
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        // 显示墙面检测范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wallDetectionDistance);
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // 显示调试信息
        GUILayout.BeginArea(new Rect(10, 10, 250, 250));
        GUILayout.Label("墙面行走状态: " + (isOnWall ? "在墙上" : "正常"));
        GUILayout.Label("接地状态: " + (isGrounded ? "接地" : "悬空"));
        GUILayout.Label("墙面接地状态: " + (isWallGrounded ? "墙面接地" : "墙面悬空"));
        GUILayout.Label("重力方向: " + currentGravityDirection.ToString("F2"));
        GUILayout.Label("速度: " + playerVelocity.magnitude.ToString("F2"));
        GUILayout.Label("墙面法线: " + wallNormal.ToString("F2"));

        // 显示输入和移动方向
        Vector2 inputVec = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        GUILayout.Label("输入: " + inputVec.ToString("F2"));

        if (isOnWall)
        {
            GUILayout.Label("墙面上S键 = " + (inputVec.y < 0 ? "向下移动" : "不是向下"));
        }

        GUILayout.EndArea();
    }
}

