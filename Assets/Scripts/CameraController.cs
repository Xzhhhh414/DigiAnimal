using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    // 引用Cinemachine虚拟相机
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    // 摄像机移动速度
    [SerializeField] private float panSpeed = 15f;
    
    // 宠物跟随速度
    [SerializeField] private float followSpeed = 2f;
    
    // 引用PetManager
    [SerializeField] private PetManager petManager;
    
    // 是否正在拖动
    private bool isDragging = false;
    
    // 拖动起始位置
    private Vector2 dragStartPosition;
    
    // 摄像机组件
    private CinemachineFramingTransposer framingTransposer;
    
    // 当前目标位置
    private Vector3 targetFollowOffset;
    
    // 上一个选中的宠物
    private CharacterController2D previousSelectedPet = null;
    
    // 摄像机默认目标
    private Transform defaultTarget;
    
    // 调试模式
    [SerializeField] private bool debugMode = false;
    
    // 相机状态
    private enum CameraState { Free, FollowingPet }
    private CameraState currentState = CameraState.Free;
    
    // 相机目标位置
    private Vector3 lastCameraPosition;
    
    // 临时目标对象（用于平滑过渡）
    private GameObject tempTarget;
    
    void Start()
    {
        // 初始化相机设置
        InitializeCamera();
        
        // 如果没有指定PetManager，尝试在场景中查找
        if (petManager == null)
        {
            petManager = FindObjectOfType<PetManager>();
        }
        
        // 保存默认跟随目标
        defaultTarget = virtualCamera.Follow;
        
        // 创建临时目标对象
        CreateTempTarget();
        
        // 确保初始状态为自由移动
        currentState = CameraState.Free;
        virtualCamera.Follow = tempTarget.transform;
        virtualCamera.LookAt = null;
    }
    
    private void InitializeCamera()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("未设置Cinemachine虚拟相机，请在Inspector中分配");
            return;
        }
        
        // 获取FramingTransposer组件
        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framingTransposer == null)
        {
            Debug.LogWarning("虚拟相机没有FramingTransposer组件，请在Cinemachine相机上确保Body类型为'Framing Transposer'");
        }
        else
        {
            // 初始化目标位置
            targetFollowOffset = framingTransposer.m_TrackedObjectOffset;
        }
    }
    
    private void CreateTempTarget()
    {
        // 创建一个临时目标对象，用于平滑过渡
        tempTarget = new GameObject("TempCameraTarget");
        tempTarget.transform.position = virtualCamera.transform.position + virtualCamera.transform.forward * 10f;
    }
    
    void Update()
    {
        // 保存当前相机位置
        lastCameraPosition = virtualCamera.transform.position;
        
        // 检查是否有选中的宠物
        CharacterController2D selectedPet = petManager?.GetSelectedPet();
        
        // 如果选中的宠物改变，更新摄像机目标
        if (selectedPet != previousSelectedPet)
        {
            if (selectedPet != null)
            {
                // 从自由模式切换到宠物跟随模式
                SwitchToFollowMode(selectedPet.transform);
            }
            else
            {
                // 从宠物跟随模式切换到自由模式
                SwitchToFreeMode();
            }
            
            previousSelectedPet = selectedPet;
        }
        
        // 更新临时目标位置
        UpdateTempTargetPosition(selectedPet);
        
        // 只有在自由模式下才能拖动场景
        if (currentState == CameraState.Free)
        {
            HandleDragging();
            
            // 调试信息
            if (debugMode && isDragging)
            {
                // Debug.Log("正在拖动: " + isDragging + ", 偏移: " + targetFollowOffset);
            }
        }
    }
    
    private void UpdateTempTargetPosition(CharacterController2D selectedPet)
    {
        if (selectedPet != null && currentState == CameraState.FollowingPet)
        {
            // 只有当宠物在移动时才跟随
            bool isPetMoving = IsPetMoving(selectedPet.gameObject);
            
            if (isPetMoving)
            {
                // 在宠物移动时，临时目标逐渐移向选中的宠物
                Vector3 targetPos = selectedPet.transform.position;
                targetPos.z = tempTarget.transform.position.z; // 保持z轴不变
                
                // 平滑移动临时目标
                tempTarget.transform.position = Vector3.Lerp(
                    tempTarget.transform.position, 
                    targetPos, 
                    Time.deltaTime * followSpeed); 
            }
        }
    }
    
    private bool IsPetMoving(GameObject pet)
    {
        // 使用GetComponent获取Animator，然后检查isMoving参数
        Animator petAnimator = pet.GetComponent<Animator>();
        if (petAnimator != null)
        {
            return petAnimator.GetBool("isMoving");
        }
        return false;
    }
    
    private void SwitchToFollowMode(Transform target)
    {
        if (virtualCamera == null) return;
        
        if (debugMode) 
        {
            // Debug.Log("切换到宠物跟随模式");
        }
        
        // 切换前更新临时目标位置到当前视图中心
        UpdateTempTargetToCameraView();
        
        // 设置相机跟随临时目标而不是直接跟随宠物
        virtualCamera.Follow = tempTarget.transform;
        virtualCamera.LookAt = null;
        
        // 记录当前状态
        currentState = CameraState.FollowingPet;
        
        // 确保我们有FramingTransposer组件
        if (framingTransposer != null)
        {
            // 保持当前偏移
            targetFollowOffset = framingTransposer.m_TrackedObjectOffset;
        }
    }
    
    private void SwitchToFreeMode()
    {
        if (virtualCamera == null) return;
        
        if (debugMode) 
        {
            // Debug.Log("切换到自由浏览模式");
        }
        
        // 切换前更新临时目标位置到当前视图中心
        UpdateTempTargetToCameraView();
        
        // 设置相机跟随临时目标
        virtualCamera.Follow = tempTarget.transform;
        virtualCamera.LookAt = null;
        
        // 记录当前状态
        currentState = CameraState.Free;
        
        // 确保我们有FramingTransposer组件
        if (framingTransposer != null)
        {
            // 保持当前偏移
            targetFollowOffset = framingTransposer.m_TrackedObjectOffset;
        }
    }
    
    private void UpdateTempTargetToCameraView()
    {
        // 将临时目标移动到当前相机视图中心点
        Vector3 cameraForward = virtualCamera.transform.forward;
        Vector3 position = virtualCamera.transform.position + cameraForward * 10f;
        tempTarget.transform.position = position;
    }
    
    void HandleDragging()
    {
        // 处理触摸输入
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // 开始拖动
            if (touch.phase == TouchPhase.Began)
            {
                dragStartPosition = touch.position;
                isDragging = true;
                if (debugMode) 
                {
                    // Debug.Log("开始触摸拖动");
                }
            }
            // 拖动中
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 dragDelta = (Vector2)touch.position - dragStartPosition;
                PanCamera(-dragDelta);
                dragStartPosition = touch.position;
                if (debugMode) 
                {
                    // Debug.Log("触摸拖动中: " + dragDelta);
                }
            }
            // 结束拖动
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
                if (debugMode) 
                {
                    // Debug.Log("结束触摸拖动");
                }
            }
        }
        
        // 处理鼠标输入（编辑器中测试用）
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = Input.mousePosition;
            isDragging = true;
            if (debugMode) 
            {
                // Debug.Log("开始鼠标拖动");
            }
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 dragDelta = (Vector2)Input.mousePosition - dragStartPosition;
            PanCamera(-dragDelta);
            dragStartPosition = Input.mousePosition;
            if (debugMode) 
            {
                // Debug.Log("鼠标拖动中: " + dragDelta);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (debugMode) 
            {
                // Debug.Log("结束鼠标拖动");
            }
        }
    }
    
    void PanCamera(Vector2 dragDelta)
    {
        if (framingTransposer != null)
        {
            // 计算适当的移动量
            float scale = Time.deltaTime * panSpeed * 0.1f;
            dragDelta *= scale;
            
            // 移动临时目标而不是修改偏移
            Vector3 right = virtualCamera.transform.right;
            Vector3 up = virtualCamera.transform.up;
            
            tempTarget.transform.position += right * dragDelta.x + up * dragDelta.y;
            
            if (debugMode) 
            {
                // Debug.Log("移动临时目标: " + dragDelta);
            }
        }
        else
        {
            Debug.LogWarning("framingTransposer为空，无法移动相机");
        }
    }
    
    void OnDestroy()
    {
        // 清理临时对象
        if (tempTarget != null)
        {
            Destroy(tempTarget);
        }
    }
} 