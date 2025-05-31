// using UnityEngine;
// using UnityEngine.XR.Interaction.Toolkit;
// using Unity.XR.CoreUtils;

// [AddComponentMenu("XR/My Locomotion + Jump & Smooth Crouch")]
// public class MyLocomotionProvider : LocomotionProvider
// {
//     [Header("必须在 Inspector 里关联：")]
//     [Tooltip("场景中挂在 XR Origin 上的 Locomotion System 组件")]
//     public LocomotionSystem locomotionSystem;

//     [Tooltip("摄像机容器（Camera Offset），用来调整蹲下时的高度")]
//     public Transform cameraOffset;

//     [Header("移动参数：")]
//     [Tooltip("水平移动速度（米/秒）")]
//     public float moveSpeed = 1.5f;

//     [Tooltip("重力加速度（米/秒²），正值表示速度朝下变化")]
//     public float gravity = 9.81f;

//     [Tooltip("起跳初速度（米/秒），按下 Space 时角色获得的上抛速度")]
//     public float jumpSpeed = 2.5f;

//     [Header("蹲下参数：")]
//     [Tooltip("站立时 CharacterController 的高度")]
//     public float standHeight = 1.8f;

//     [Tooltip("蹲下时 CharacterController 的高度")]
//     public float crouchHeight = 1.0f;

//     [Tooltip("站立时摄像机容器的局部 Y（通常 = 0）")]
//     public float standCameraY = 0f;

//     [Tooltip("蹲下时摄像机容器的局部 Y（比如 -0.5，镜头往下沉）")]
//     public float crouchCameraY = -0.25f;

//     [Header("蹲下/抬起 平滑速度：")]
//     [Tooltip("蹲下/抬起时摄像机容器与胶囊高度插值的速度，数值越大过渡越快")]
//     public float crouchSmoothSpeed = 5f;

//     // 私有缓存
//     private XROrigin xrOrigin;
//     private CharacterController controller;

//     // 垂直速度（负值表示向下坠落）
//     private float verticalVelocity = 0f;

//     // 记录当前“目标”状态
//     private bool isCrouchPressedLastFrame = false; // 用来判断上一帧是否在蹲

//     private void Awake()
//     {
//         // 1. 找 XROrigin
//         xrOrigin = GetComponentInParent<XROrigin>();
//         if (xrOrigin == null)
//         {
//             Debug.LogError("[MyLocomotionProvider] 找不到 XROrigin，请确认脚本挂在 XR Origin 或其子物体下。");
//         }

//         // 2. 找 CharacterController
//         controller = GetComponent<CharacterController>();
//         if (controller == null)
//         {
//             controller = GetComponentInParent<CharacterController>();
//         }
//         if (controller == null)
//         {
//             Debug.LogError("[MyLocomotionProvider] 找不到 CharacterController，请在 XR Origin（或其父级）上添加。");
//         }
//         else
//         {
//             // 初始化 CharacterController 的高度为 standHeight
//             controller.height = standHeight;
//             // center 要设置到 height/2
//             controller.center = new Vector3(0f, standHeight / 2f, 0f);
//         }

//         // 3. 检查 cameraOffset
//         if (cameraOffset == null)
//         {
//             Debug.LogError("[MyLocomotionProvider] cameraOffset 未赋值，请在 Inspector 中拖入“Camera Offset”空物体。");
//         }
//         else
//         {
//             // 初始化摄像机容器位置为 standCameraY
//             Vector3 cpos = cameraOffset.localPosition;
//             cpos.y = standCameraY;
//             cameraOffset.localPosition = cpos;
//         }
//     }

//     private void Start()
//     {
//         // 4. 检查 locomotionSystem
//         if (locomotionSystem == null)
//         {
//             Debug.LogError("[MyLocomotionProvider] locomotionSystem 未赋值，请在 Inspector 中将场景里的 Locomotion System 拖进来。");
//         }

//         // 只要有任意必需组件缺失，就报错并停掉
//         if (xrOrigin == null || controller == null || cameraOffset == null || locomotionSystem == null)
//         {
//             Debug.LogError("[MyLocomotionProvider] 存在未赋值或未找到的组件，脚本无法正常工作。");
//             enabled = false; // 禁用此脚本
//             return;
//         }

//         // 简单激活一次 LocomotionSystem，让它“热身”
//         BeginLocomotion();
//         EndLocomotion();
//     }

//     private void Update()
//     {
//         // 再次保险检查
//         if (xrOrigin == null || controller == null || cameraOffset == null || locomotionSystem == null)
//             return;

//         // —— 一、水平移动部分 —— 
//         Transform camTf = xrOrigin.Camera.transform;
//         // “前”和“右”的水平向量
//         Vector3 forward = camTf.forward;
//         forward.y = 0f;
//         forward.Normalize();
//         Vector3 right = camTf.right;
//         right.y = 0f;
//         right.Normalize();

//         // 根据 WASD 累加方向
//         Vector3 moveDir = Vector3.zero;
//         if (Input.GetKey(KeyCode.W)) moveDir += forward;
//         if (Input.GetKey(KeyCode.S)) moveDir -= forward;
//         if (Input.GetKey(KeyCode.A)) moveDir -= right;
//         if (Input.GetKey(KeyCode.D)) moveDir += right;

//         Vector3 horizontalMove = Vector3.zero;
//         if (moveDir != Vector3.zero)
//         {
//             moveDir.Normalize();
//             horizontalMove = moveDir * moveSpeed * Time.deltaTime;
//         }

//         // —— 二、跳跃与重力部分 —— 
//         // 如果在地面，并且按下空格（KeyDown 而不是 GetKey，可以保证一次按下只触发一次）
//         if (controller.isGrounded)
//         {
//             // 着地时，将 verticalVelocity 置零（防止悬空）
//             verticalVelocity = 0f;

//             // 如果刚按下的是“空格”，就起跳
//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 verticalVelocity = jumpSpeed;
//             }
//         }
//         else
//         {
//             // 不在地面时，持续按重力让角色下落
//             verticalVelocity -= gravity * Time.deltaTime;
//         }
//         // 计算本帧因重力/跳跃所造成的垂直位移
//         Vector3 verticalMove = Vector3.up * verticalVelocity * Time.deltaTime;

//         // —— 三、蹲下（Crouch）部分 —— 
//         bool isCrouching = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

//         // 1. 先决定“目标”高度与摄像机Y
//         float targetHeight = isCrouching ? crouchHeight : standHeight;
//         float targetCameraY = isCrouching ? crouchCameraY : standCameraY;

//         // 2. 把 CharacterController.height 从当前值，平滑过渡到 targetHeight
//         //    用 Lerp：参数 t = crouchSmoothSpeed * Time.deltaTime
//         float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchSmoothSpeed * Time.deltaTime);
//         controller.height = newHeight;
//         // height 变了，center 要同步到 height/2
//         controller.center = new Vector3(0f, newHeight / 2f, 0f);

//         // 3. 把摄像机容器的 localPosition.y 从当前值，平滑过渡到 targetCameraY
//         Vector3 camPos = cameraOffset.localPosition;
//         float newCamY = Mathf.Lerp(camPos.y, targetCameraY, crouchSmoothSpeed * Time.deltaTime);
//         camPos.y = newCamY;
//         cameraOffset.localPosition = camPos;

//         // —— 四、合并所有移动并交给 CharacterController —— 
//         Vector3 finalMove = horizontalMove + verticalMove;
//         if (CanBeginLocomotion() && finalMove != Vector3.zero)
//         {
//             BeginLocomotion();
//             controller.Move(finalMove);
//             EndLocomotion();
//         }
//     }
// }
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // 你原来有这行，但在这个脚本里似乎没用到XR Interaction Toolkit的特定类
using Unity.XR.CoreUtils; // 你原来有这行，XROrigin 来自这里

[AddComponentMenu("XR/My Locomotion + Jump & Smooth Crouch + Look")] // 我稍微修改了下菜单名
public class MyLocomotionProvider : LocomotionProvider
{
    [Header("必须在 Inspector 里关联：")]
    [Tooltip("场景中挂在 XR Origin 上的 Locomotion System 组件")]
    public LocomotionSystem locomotionSystem;

    [Tooltip("摄像机容器（Camera Offset），用来调整蹲下时的高度")]
    public Transform cameraOffset; // 注意：如果你的摄像机不是 Camera Offset 的子对象，这个可能需要调整

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

    [Header("视角旋转参数：")] // 新增
    [Tooltip("鼠标灵敏度")]
    public float mouseSensitivity = 100f;

    // 私有缓存
    private XROrigin xrOrigin;
    private CharacterController controller;
    private Transform mainCameraTransform; // 新增：缓存主摄像机的Transform
    private float xRotation = 0f;          // 新增：存储摄像机上下旋转的角度

    // 垂直速度（负值表示向下坠落）
    private float verticalVelocity = 0f;

    // 记录当前“目标”状态 (这部分似乎未在你之前的代码中完全使用，但保留)
    // private bool isCrouchPressedLastFrame = false; 

    private void Awake()
    {
        // 1. 找 XROrigin
        xrOrigin = GetComponentInParent<XROrigin>(); // 通常此脚本挂在XR Origin上，所以用GetComponent也可以
        if (xrOrigin == null)
        {
            Debug.LogError("[MyLocomotionProvider] 找不到 XROrigin，请确认脚本挂在 XR Origin 上。");
        }
        else 
        {
            // 尝试获取 XROrigin 组件暴露的 Camera 引用
            if (xrOrigin.Camera != null)
            {
                mainCameraTransform = xrOrigin.Camera.transform;
            }
            else // 如果 XROrigin 没有直接暴露 Camera，则在其子对象中查找
            {
                Camera cam = GetComponentInChildren<Camera>(true); // true 查找包括未激活的
                if (cam != null)
                {
                    mainCameraTransform = cam.transform;
                }
            }

            if (mainCameraTransform == null)
            {
                Debug.LogError("[MyLocomotionProvider] 找不到 Main Camera Transform，请确保 XR Origin 下有主摄像机，并且 XROrigin 组件正确配置。");
            }
        }

        // 2. 找 CharacterController (通常也挂在XR Origin上)
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("[MyLocomotionProvider] 找不到 CharacterController，请在 XR Origin 上添加。");
        }
        else
        {
            controller.height = standHeight;
            controller.center = new Vector3(0f, standHeight / 2f, 0f);
        }

        // 3. 检查 cameraOffset
        if (cameraOffset == null)
        {
            Debug.LogError("[MyLocomotionProvider] cameraOffset 未赋值，请在 Inspector 中拖入“Camera Offset”物体。如果非VR，主摄像机可能直接是XROrigin的子对象，此引用可能不需要或用途不同。");
        }
        else
        {
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

        if (xrOrigin == null || controller == null || mainCameraTransform == null || locomotionSystem == null) // 确保mainCameraTransform也被检查
        {
            Debug.LogError("[MyLocomotionProvider] 存在未赋值或未找到的核心组件，脚本无法正常工作。");
            enabled = false; 
            return;
        }

        if (cameraOffset == null && (crouchHeight != standHeight || crouchCameraY != standCameraY))
        {
            Debug.LogWarning("[MyLocomotionProvider] cameraOffset 未赋值，但蹲下参数与站立不同，蹲下时的摄像机高度调整可能不会生效。");
        }


        BeginLocomotion();
        EndLocomotion();

        // 新增：锁定并隐藏鼠标光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (enabled == false) return; // 如果在Start中被禁用了，就直接返回

        // ———— 视角旋转部分 ————
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 左右旋转: 旋转整个 XR Origin (即 this.transform)
        transform.Rotate(Vector3.up * mouseX);

        // 上下旋转: 只旋转 Main Camera
        xRotation -= mouseY; 
        xRotation = Mathf.Clamp(xRotation, -85f, 85f); 
        mainCameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // ———— 视角旋转部分结束 ————

        // —— 一、水平移动部分 —— 
        Transform camTfForMovement = mainCameraTransform; // 使用主摄像机的朝向来决定移动方向
        Vector3 forward = camTfForMovement.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = camTfForMovement.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveDir += forward;
        if (Input.GetKey(KeyCode.S)) moveDir -= forward;
        if (Input.GetKey(KeyCode.A)) moveDir -= right;
        if (Input.GetKey(KeyCode.D)) moveDir += right;

        Vector3 horizontalMove = Vector3.zero;
        if (moveDir != Vector3.zero)
        {
            moveDir.Normalize(); // 确保斜向移动速度一致
            horizontalMove = moveDir * moveSpeed * Time.deltaTime;
        }

        // —— 二、跳跃与重力部分 —— 
        if (controller.isGrounded)
        {
            verticalVelocity = 0f; // 防止累积一个小的负速度导致isGrounded间歇性为false
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpSpeed;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        Vector3 verticalMove = Vector3.up * verticalVelocity * Time.deltaTime;

        // —— 三、蹲下（Crouch）部分 —— 
        bool isCrouching = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCameraYOffset = isCrouching ? crouchCameraY : standCameraY; // 这是相对于Camera Offset的Y值

        float newHeight = Mathf.Lerp(controller.height, targetHeight, crouchSmoothSpeed * Time.deltaTime);
        controller.height = newHeight;
        controller.center = new Vector3(0f, newHeight / 2f, 0f);

        // 只有当 cameraOffset 被赋值时，才调整它
        if (cameraOffset != null)
        {
            Vector3 camOffsetPos = cameraOffset.localPosition;
            float newCamOffsetY = Mathf.Lerp(camOffsetPos.y, targetCameraYOffset, crouchSmoothSpeed * Time.deltaTime);
            camOffsetPos.y = newCamOffsetY;
            cameraOffset.localPosition = camOffsetPos;
        }


        // —— 四、合并所有移动并交给 CharacterController —— 
        Vector3 finalMove = horizontalMove + verticalMove;
        if (CanBeginLocomotion()) // 确保LocomotionProvider允许移动
        {
            BeginLocomotion();
            controller.Move(finalMove); // 应用移动
            EndLocomotion();
        }
    }
}