using UnityEngine;
using Cinemachine;

public class CameraBounds : MonoBehaviour, ICameraBounds
{
    // 相机引用
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    // 边界参数
    [Header("相机边界设置")]
    [SerializeField] private float boundLeft = -10f;
    [SerializeField] private float boundRight = 10f;
    [SerializeField] private float boundBottom = -10f;
    [SerializeField] private float boundTop = 10f;
    
    // 调试用
    [Header("调试设置")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color boundsColor = Color.yellow;
    [SerializeField] private Color viewportColor = Color.green;
    
    private void Start()
    {
        // 验证组件
        if (virtualCamera == null)
        {
            virtualCamera = GetComponentInParent<CinemachineVirtualCamera>();
            if (virtualCamera == null)
            {
                Debug.LogError("未设置虚拟相机引用，请在Inspector中分配");
                enabled = false;
                return;
            }
        }
    }
    
    private void LateUpdate()
    {
        // 不在LateUpdate中自动检测边界
        // 让CameraController控制边界检测的时机
    }
    
    // 检查给定的正交大小是否在边界范围内
    public bool IsOrthoSizeValid(float orthoSize, Transform target)
    {
        if (target == null || virtualCamera == null) return true;
        
        // 计算视口宽度
        float aspect = (float)Screen.width / Screen.height;
        float horizontalSize = orthoSize * aspect;
        
        // 检查水平方向
        float boundWidth = boundRight - boundLeft;
        if (horizontalSize * 2 > boundWidth)
        {
            return false;
        }
        
        // 检查垂直方向
        float boundHeight = boundTop - boundBottom;
        if (orthoSize * 2 > boundHeight)
        {
            return false;
        }
        
        return true;
    }
    
    // 计算当前位置下的最大允许正交大小
    public float GetMaxOrthoSize(Transform target)
    {
        if (target == null || virtualCamera == null) 
            return float.MaxValue;
            
        float aspect = (float)Screen.width / Screen.height;
        
        // 计算水平和垂直方向的约束
        float horizontalConstraint = (boundRight - boundLeft) / (2 * aspect);
        float verticalConstraint = (boundTop - boundBottom) / 2;
        
        // 返回两个约束中较小的一个
        return Mathf.Min(horizontalConstraint, verticalConstraint);
    }
    
    // 强制相机目标在边界内 - 接受目标参数
    public void EnforceBounds(Transform target)
    {
        if (target == null || virtualCamera == null) return;
        
        // 计算当前视口大小
        float orthoSize = virtualCamera.m_Lens.OrthographicSize;
        float aspect = (float)Screen.width / Screen.height;
        float horizontalSize = orthoSize * aspect;
        
        // 计算可移动范围
        float minX = boundLeft + horizontalSize;
        float maxX = boundRight - horizontalSize;
        float minY = boundBottom + orthoSize;
        float maxY = boundTop - orthoSize;
        
        // 确保边界合法（相机视口不超过边界总大小）
        if (minX > maxX)
        {
            // 水平方向边界太小，无法容纳当前视口
            // 将相机置于边界中心
            float centerX = (boundLeft + boundRight) / 2f;
            target.position = new Vector3(centerX, target.position.y, target.position.z);
        }
        else
        {
            // 限制相机水平位置
            Vector3 targetPos = target.position;
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            target.position = targetPos;
        }
        
        if (minY > maxY)
        {
            // 垂直方向边界太小，无法容纳当前视口
            // 将相机置于边界中心
            float centerY = (boundBottom + boundTop) / 2f;
            target.position = new Vector3(target.position.x, centerY, target.position.z);
        }
        else
        {
            // 限制相机垂直位置
            Vector3 targetPos = target.position;
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
            target.position = targetPos;
        }
    }
    
    // 显示调试边界
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // 绘制边界矩形
        Gizmos.color = boundsColor;
        Gizmos.DrawLine(new Vector3(boundLeft, boundBottom, 0), new Vector3(boundRight, boundBottom, 0));
        Gizmos.DrawLine(new Vector3(boundRight, boundBottom, 0), new Vector3(boundRight, boundTop, 0));
        Gizmos.DrawLine(new Vector3(boundRight, boundTop, 0), new Vector3(boundLeft, boundTop, 0));
        Gizmos.DrawLine(new Vector3(boundLeft, boundTop, 0), new Vector3(boundLeft, boundBottom, 0));
        
        // 如果设置了相机，绘制当前视口
        if (virtualCamera != null && Application.isPlaying)
        {
            Gizmos.color = viewportColor;
            
            float orthoSize = virtualCamera.m_Lens.OrthographicSize;
            float aspect = (float)Screen.width / Screen.height;
            float horizontalSize = orthoSize * aspect;
            
            Transform currentTarget = virtualCamera.Follow;
            if (currentTarget != null)
            {
                Vector3 cameraPos = currentTarget.position;
                Gizmos.DrawWireCube(cameraPos, new Vector3(horizontalSize * 2, orthoSize * 2, 0.1f));
            }
        }
    }
    
    // 公开属性，用于在编辑器中或通过脚本调整设置
    public float BoundLeft { get { return boundLeft; } set { boundLeft = value; } }
    public float BoundRight { get { return boundRight; } set { boundRight = value; } }
    public float BoundBottom { get { return boundBottom; } set { boundBottom = value; } }
    public float BoundTop { get { return boundTop; } set { boundTop = value; } }
} 