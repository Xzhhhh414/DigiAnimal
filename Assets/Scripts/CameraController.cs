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
    [SerializeField] [Tooltip("控制相机初始选中宠物时的过渡时间，值越大过渡越快")] private float followSpeed = 2f;
    
    // 引用PetManager
    [SerializeField] private PetManager petManager;
    
    // 引用FoodManager
    [SerializeField] private FoodManager foodManager;
    
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
    private PetController2D previousSelectedPet = null;
    
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
    
    // 添加新变量，标记是否是初次选中宠物
    private bool isInitialSelection = false;
    // 添加计时器，用于平滑过渡
    private float selectionTransitionTimer = 0f;
    
    void Start()
    {
        // 初始化相机设置
        InitializeCamera();
        
        // 如果没有指定PetManager，尝试在场景中查找
        if (petManager == null)
        {
            petManager = FindObjectOfType<PetManager>();
        }
        
        // 如果没有指定FoodManager，尝试在场景中查找
        if (foodManager == null)
        {
            foodManager = FindObjectOfType<FoodManager>();
        }
        
        // 保存默认跟随目标
        defaultTarget = virtualCamera.Follow;
        
        // 确保cameraTarget已设置
        if (cameraTarget == null)
        {
            // 创建相机目标
            GameObject targetObj = new GameObject("CameraTarget");
            cameraTarget = targetObj.transform;
            
            // 初始位置设为相机当前位置前方
            cameraTarget.position = virtualCamera.transform.position + virtualCamera.transform.forward * 10f;
        }

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
        
        // 设置初始相机位置 - 使用场景中心或预设位置
        Vector3 initialPosition = new Vector3(0, 0, cameraTarget.position.z);
        cameraTarget.position = initialPosition;
        
        // 如果有边界限制，确保初始位置在边界内
        if (cameraBounds != null)
        {
            cameraBounds.EnforceBounds(cameraTarget);
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
        PetController2D selectedPet = petManager?.GetSelectedPet();
        
        // 检查是否有选中的食物
        FoodController selectedFood = foodManager?.GetSelectedFood();
        
        // 处理相机状态切换
        // 如果有选中的食物，确保相机处于自由模式
        if (selectedFood != null)
        {
            // 只有当前不在自由模式时，才执行状态切换
            if (currentState != CameraState.Free)
            {
                SwitchToFreeMode();
            }
            
            // 如果之前有选中的宠物，清除它，避免下方的选中宠物逻辑被执行
            if (previousSelectedPet != null)
            {
                previousSelectedPet = null;
            }
        }
        // 如果没有选中的食物，处理宠物选中逻辑
        else if (selectedPet != previousSelectedPet)
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
    
    private void UpdateCameraTargetPosition(PetController2D selectedPet)
    {
        if (selectedPet != null && currentState == CameraState.FollowingPet)
        {
            Vector3 targetPos = selectedPet.transform.position;
            targetPos.z = cameraTarget.position.z; // 保持z轴不变
            
            if (isInitialSelection)
            {
                // 使用followSpeed来计算过渡时间：值越大，过渡越快
                float transitionDuration = 1.0f / followSpeed;
                
                // 初次选中时使用平滑移动
                selectionTransitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(selectionTransitionTimer / transitionDuration);
                
                // 使用平滑过渡函数
                float smoothT = Mathf.SmoothStep(0, 1, t);
                
                // 平滑移动目标
                cameraTarget.position = Vector3.Lerp(
                    cameraTarget.position, 
                    targetPos, 
                    smoothT);
                
                // 过渡完成后，切换到紧跟模式
                if (selectionTransitionTimer >= transitionDuration)
                {
                    isInitialSelection = false;
                }
            }
            else
            {
                // 宠物移动时，相机紧跟宠物位置
                cameraTarget.position = targetPos;
            }
            
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
        
        // 设置为初次选中状态，并重置计时器
        isInitialSelection = true;
        selectionTransitionTimer = 0f;
        
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
            //Debug.Log("切换到自由浏览模式");
        }
        
        // 暂时禁用虚拟相机，防止其在切换过程中自动更新位置
        virtualCamera.enabled = false;
        
        // 获取主相机当前世界位置坐标
        Vector3 currentCameraPosition = Camera.main.transform.position;
        Quaternion currentCameraRotation = Camera.main.transform.rotation;
        
        // 记录当前状态
        currentState = CameraState.Free;
        
        // 将相机目标直接放置在主相机当前位置前方
        // 计算相机前方的位置（使用相机的forward方向，但保持z深度）
        Vector3 forward = currentCameraRotation * Vector3.forward;
        forward.z = 0; // 确保在2D平面上
        
        // 设置目标位置 - 直接位于相机当前位置的平面投影处
        cameraTarget.position = new Vector3(
            currentCameraPosition.x,
            currentCameraPosition.y,
            cameraTarget.position.z
        );
        
        // 确保相机跟随目标
        virtualCamera.Follow = cameraTarget;
        virtualCamera.LookAt = cameraTarget;
        
        // 重置帧转换器的偏移量
        if (framingTransposer != null)
        {
            framingTransposer.m_TrackedObjectOffset = Vector3.zero;
        }
        
        // 重新启用虚拟相机
        virtualCamera.enabled = true;
        
        // 强制更新Brain，确保即时应用更改
        var brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null)
        {
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.FixedUpdate;
            brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.FixedUpdate;
        }
        
        // 启动协程确保相机位置稳定
        StartCoroutine(StabilizeCameraPosition(currentCameraPosition));
    }
    
    private IEnumerator StabilizeCameraPosition(Vector3 targetPosition)
    {
        // 等待一帧让Cinemachine更新
        yield return null;
        
        // 确保相机目标位置正确
        Vector3 newTargetPos = new Vector3(
            targetPosition.x,
            targetPosition.y,
            cameraTarget.position.z
        );
        cameraTarget.position = newTargetPos;
        
        // 如果相机位置偏移较大，强制修正
        if (Vector3.Distance(new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.y), 
                             new Vector2(targetPosition.x, targetPosition.y)) > 0.1f)
        {
            // 直接设置相机位置
            Camera.main.transform.position = new Vector3(
                targetPosition.x,
                targetPosition.y,
                Camera.main.transform.position.z
            );
        }
    }
    
    // 保留该方法但我们不会在SwitchToFreeMode中使用它
    private void UpdateCameraTargetToCameraView()
    {
        if (virtualCamera == null) return;
        
        // 使用当前相机位置作为参考点
        Vector3 cameraPosition = virtualCamera.transform.position;
        
        // 计算准确的前方位置
        Vector3 cameraForward = virtualCamera.transform.forward.normalized;
        
        // 将目标位置设置在相机前方的特定距离
        cameraTarget.position = cameraPosition + cameraForward * 10f;
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