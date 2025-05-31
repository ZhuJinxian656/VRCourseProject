using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

[AddComponentMenu("XR/My Locomotion + Jump & Smooth Crouch")]
public class MyLocomotionProvider : LocomotionProvider
{
    [Header("必须在 Inspector 里关联：")]
    [Tooltip("场景中挂在 XR Origin 上的 Locomotion System 组件")]
    public LocomotionSystem locomotionSystem;

    [Tooltip("摄像机容器（Camera Offset），用来调整蹲下时的高度")]
    public Transform cameraOffset;

    [Header("移动参数：")]
    [Tooltip("水平移动速度（米/秒）")]
    public float moveSpeed = 1.5f;

    [Tooltip("重力加速度（米/秒²），正值表示速度朝下变化")]
    public float gravity = 9.81f;

    [Tooltip("起跳初速度（米/秒），按下 Space 时角色获得的上抛速度")]
    public float jumpSpeed = 2.5f;

    [Header("蹲下参数：")]
    [Tooltip("站立时 CharacterController 的高度")]
    public float standHeight = 1.8f;

    [Tooltip("蹲下时 CharacterController 的高度")]
    public float crouchHeight = 1.0f;

    [Tooltip("站立时摄像机容器的局部 Y（通常 = 0）")]
    public float standCameraY = 0f;

    [Tooltip("蹲下时摄像机容器的局部 Y（比如 -0.5，镜头往下沉）")]
    public float crouchCameraY = -0.25f;

    [Header("蹲下/抬起 平滑速度：")]
    [Tooltip("蹲下/抬起时摄像机容器与胶囊高度插值的速度，数值越大过渡越快")]
    public float crouchSmoothSpeed = 5f;

    // 私有缓存
    private XROrigin xrOrigin;
    private CharacterController controller;

    // 垂直速度（负值表示向下坠落）
    private float verticalVelocity = 0f;

    // 记录当前“目标”状态
    private bool isCrouchPressedLastFrame = false; // 用来判断上一帧是否在蹲

    private void Awake()
    {
        // 1. 找 XROrigin
        xrOrigin = GetComponentInParent<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("[MyLocomotionProvider] 找不到 XROrigin，请确认脚本挂在 XR Origin 或其子物体下。");
        }

        // 2. 找 CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = GetComponentInParent<CharacterController>();
        }
        if (controller == null)
        {
            Debug.LogError("[MyLocomotionProvider] 找不到 CharacterController，请在 XR Origin（或其父级）上添加。");
        }
        else
        {
            // 初始化 CharacterController 的高度为 standHeight
            controller.height = standHeight;
            // center 要设置到 height/2
            controller.center = new Vector3(0f, standHeight / 2f, 0f);
        }

        // 3. 检查 cameraOffset
        if (cameraOffset == null)
        {
            Debug.LogError("[MyLocomotionProvider] cameraOffset 未赋值，请在 Inspector 中拖入“Camera Offset”空物体。");
        }
        else
        {
            // 初始化摄像机容器位置为 standCameraY
            Vector3 cpos = cameraOffset.localPosition;
            cpos.y = standCameraY;
            cameraOffset.localPosition = cpos;
        }
    }

    private void Start()
    {
        // 4. 检查 locomotionSystem
        if (locomotionSystem == null)
        {
            Debug.LogError("[MyLocomotionProvider] locomotionSystem 未赋值，请在 Inspector 中将场景里的 Locomotion System 拖进来。");
        }

        // 只要有任意必需组件缺失，就报错并停掉
        if (xrOrigin == null || controller == null || cameraOffset == null || locomotionSystem == null)
        {
            Debug.LogError("[MyLocomotionProvider] 存在未赋值或未找到的组件，脚本无法正常工作。");
            enabled = false; // 禁用此脚本
            return;
        }

        // 简单激活一次 LocomotionSystem，让它“热身”
        BeginLocomotion();
        EndLocomotion();
    }

    private void Update()
    {
        // 再次保险检查
        if (xrOrigin == null || controller == null || cameraOffset == null || locomotionSystem == null)
            return;

        // —— 一、水平移动部分 —— 
        Transform camTf = xrOrigin.Camera.transform;
        // “前”和“右”的水平向量
        Vector3 forward = camTf.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = camTf.right;
        right.y = 0f;
        right.Normalize();

        // 根据 WASD 累加方向
        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveDir += forward;
        if (Input.GetKey(KeyCode.S)) moveDir -= forward;
        if (Input.GetKey(KeyCode.A)) moveDir -= right;
        if (Input.GetKey(KeyCode.D)) moveDir += right;

        Vector3 horizontalMove = Vector3.zero;
        if (moveDir != Vector3.zero)
        {
            moveDir.Normalize();
            horizontalMove = moveDir * moveSpeed * Time.deltaTime;
        }

        // —— 二、跳跃与重力部分 —— 
        // 如果在地面，并且按下空格（KeyDown 而不是 GetKey，可以保证一次按下只触发一次）
        if (controller.isGrounded)
        {
            // 着地时，将 verticalVelocity 置零（防止悬空）
            verticalVelocity = 0f;

            // 如果刚按下的是“空格”，就起跳
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpSpeed;
            }
        }
        else
        {
            // 不在地面时，持续按重力让角色下落
            verticalVelocity -= gravity * Time.deltaTime;
        }
        // 计算本帧因重力/跳跃所造成的垂直位移
        Vector3 verticalMove = Vector3.up * verticalVelocity * Time.deltaTime;

        // —— 三、蹲下（Crouch）部分 —— 
        bool isCrouching = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 1. 先决定“目标”高度与摄像机Y
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCameraY = isCrouching ? crouchCameraY : standCameraY;

        // 2. 把 CharacterController.height 从当前值，平滑过渡到 targetHeight
        //    用 Lerp：参数 t = crouchSmoothSpeed * Time.deltaTime
        float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchSmoothSpeed * Time.deltaTime);
        controller.height = newHeight;
        // height 变了，center 要同步到 height/2
        controller.center = new Vector3(0f, newHeight / 2f, 0f);

        // 3. 把摄像机容器的 localPosition.y 从当前值，平滑过渡到 targetCameraY
        Vector3 camPos = cameraOffset.localPosition;
        float newCamY = Mathf.Lerp(camPos.y, targetCameraY, crouchSmoothSpeed * Time.deltaTime);
        camPos.y = newCamY;
        cameraOffset.localPosition = camPos;

        // —— 四、合并所有移动并交给 CharacterController —— 
        Vector3 finalMove = horizontalMove + verticalMove;
        if (CanBeginLocomotion() && finalMove != Vector3.zero)
        {
            BeginLocomotion();
            controller.Move(finalMove);
            EndLocomotion();
        }
    }
}
