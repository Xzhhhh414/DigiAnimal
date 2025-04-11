using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

// 直接在此定义CameraBounds接口，确保引用正确
public interface ICameraBounds
{
    void EnforceBounds(Transform target);
    bool IsOrthoSizeValid(float orthoSize, Transform target);
    float GetMaxOrthoSize(Transform target);
}

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
    
    // 相机目标 - 可以在Inspector中配置而不是自动创建
    [SerializeField] private Transform cameraTarget;
    
    // 边界系统 - 使用组件引用而不是类型引用
    [SerializeField] private MonoBehaviour boundaryController;
    private ICameraBounds cameraBounds;
    
    // 缩放相关参数
    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minOrthoSize = 3f;
    [SerializeField] private float maxOrthoSize = 9f;
    [SerializeField] private float defaultOrthoSize = 6f;
    
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
    
    // 临时目标对象（用于平滑过渡）- 如果在Inspector中没有配置则自动创建
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
        
        // 处理相机目标
        SetupCameraTarget();
        
        // 确保初始状态为自由移动
        currentState = CameraState.Free;
        virtualCamera.Follow = cameraTarget;
        virtualCamera.LookAt = cameraTarget;
        
        // 初始化边界控制器
        if (boundaryController != null)
        {
            cameraBounds = boundaryController as ICameraBounds;
        }
        else
        {
            // 尝试获取CameraBounds组件
            cameraBounds = GetComponent<ICameraBounds>();
        }
        
        // 初始化缩放
        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.OrthographicSize = defaultOrthoSize;
        }
    }
    
    private void SetupCameraTarget()
    {
        // 如果在Inspector中没有设置相机目标，则创建一个
        if (cameraTarget == null) 
        {
            // 创建一个临时目标对象
            tempTarget = new GameObject("CameraTarget");
            tempTarget.transform.position = virtualCamera.transform.position + virtualCamera.transform.forward * 10f;
            cameraTarget = tempTarget.transform;
        }
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
        
        // 更新目标位置
        UpdateCameraTargetPosition(selectedPet);
        
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
        
        // 处理缩放
        HandleZoom();
    }
    
    private void UpdateCameraTargetPosition(CharacterController2D selectedPet)
    {
        if (selectedPet != null && currentState == CameraState.FollowingPet)
        {
            // 无论宠物是否移动都跟随它
            Vector3 targetPos = selectedPet.transform.position;
            targetPos.z = cameraTarget.position.z; // 保持z轴不变
            
            // 平滑移动目标
            cameraTarget.position = Vector3.Lerp(
                cameraTarget.position, 
                targetPos, 
                Time.deltaTime * followSpeed);
            
            // 应用边界限制，确保相机不超过边界
            if (cameraBounds != null)
            {
                cameraBounds.EnforceBounds(cameraTarget);
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
        
        // 切换前更新目标位置到当前视图中心
        UpdateCameraTargetToCameraView();
        
        // 设置相机跟随目标而不是直接跟随宠物
        virtualCamera.Follow = cameraTarget;
        virtualCamera.LookAt = cameraTarget;
        
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
        
        // 切换前更新目标位置到当前视图中心
        UpdateCameraTargetToCameraView();
        
        // 设置相机跟随目标
        virtualCamera.Follow = cameraTarget;
        virtualCamera.LookAt = cameraTarget;
        
        // 记录当前状态
        currentState = CameraState.Free;
        
        // 确保我们有FramingTransposer组件
        if (framingTransposer != null)
        {
            // 保持当前偏移
            targetFollowOffset = framingTransposer.m_TrackedObjectOffset;
        }
    }
    
    private void UpdateCameraTargetToCameraView()
    {
        // 将目标移动到当前相机视图中心点
        Vector3 cameraForward = virtualCamera.transform.forward;
        Vector3 position = virtualCamera.transform.position + cameraForward * 10f;
        cameraTarget.position = position;
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
                PanCamera(dragDelta);
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
            PanCamera(dragDelta);
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
            
            // 移动目标而不是修改偏移
            Vector3 right = virtualCamera.transform.right;
            Vector3 up = virtualCamera.transform.up;
            
            cameraTarget.position += right * dragDelta.x + up * dragDelta.y;
            
            // 拖动后确保在边界内
            if (cameraBounds != null)
            {
                cameraBounds.EnforceBounds(cameraTarget);
            }
            
            if (debugMode) 
            {
                // Debug.Log("移动目标: " + dragDelta);
            }
        }
        else
        {
            Debug.LogWarning("framingTransposer为空，无法移动相机");
        }
    }
    
    void HandleZoom()
    {
        // PC模式下的鼠标滚轮缩放
        float mouseScrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(mouseScrollDelta) > 0.01f)
        {
            ZoomCamera(mouseScrollDelta * zoomSpeed * 10f);
        }
        
        // 移动端的双指缩放
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            
            // 获取两个触摸点的当前位置和之前位置
            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            
            // 计算之前和当前的距离
            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;
            
            // 计算距离差异
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;
            
            // 应用缩放
            ZoomCamera(deltaMagnitudeDiff * 0.01f);
        }
    }
    
    void ZoomCamera(float zoomAmount)
    {
        if (virtualCamera != null)
        {
            // 获取当前正交大小
            float currentSize = virtualCamera.m_Lens.OrthographicSize;
            
            // 计算初步的新正交大小（考虑用户设置的范围限制）
            float newSize = Mathf.Clamp(currentSize - zoomAmount, minOrthoSize, maxOrthoSize);
            
            // 检查边界限制
            if (cameraBounds != null)
            {
                // 如果是放大（orthoSize减小），直接允许
                if (newSize < currentSize)
                {
                    virtualCamera.m_Lens.OrthographicSize = newSize;
                }
                else // 如果是缩小（orthoSize增大），检查是否会超出边界
                {
                    // 获取边界允许的最大orthoSize
                    float maxAllowedSize = cameraBounds.GetMaxOrthoSize(cameraTarget);
                    
                    // 确保不超过边界允许的最大值
                    newSize = Mathf.Min(newSize, maxAllowedSize);
                    
                    // 应用最终的正交大小
                    virtualCamera.m_Lens.OrthographicSize = newSize;
                }
                
                // 缩放后确保相机目标在边界内
                cameraBounds.EnforceBounds(cameraTarget);
            }
            else
            {
                // 没有边界限制，直接应用缩放
                virtualCamera.m_Lens.OrthographicSize = newSize;
            }
        }
    }
    
    void OnDestroy()
    {
        // 清理自动创建的临时对象
        if (tempTarget != null)
        {
            Destroy(tempTarget);
        }
    }
}