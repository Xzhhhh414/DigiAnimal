using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 过渡动画控制器 - 控制场景间的过渡动画效果
/// </summary>
public class TransitionController : MonoBehaviour
{
    [Header("动画控制")]
    [SerializeField] private Animator animator;
    
    [Header("事件回调")]
    public UnityEvent OnEnterComplete;   // 进入动画完成
    public UnityEvent OnExitComplete;    // 退出动画完成
    
    // 动画状态名称常量
    private const string TRIGGER_ENTER = "TriggerEnter";
    private const string TRIGGER_EXIT = "TriggerExit";
    private const string STATE_INIT = "TransitionOverlay_Init";
    private const string STATE_ENTER = "TransitionOverlay_Enter";
    private const string STATE_FULL = "TransitionOverlay_Full";
    private const string STATE_EXIT = "TransitionOverlay_Exit";
    
    // 当前状态跟踪
    private bool isTransitioning = false;
    
    // 启动时的目标状态
    private string startupTargetState = null;
    
    private void Awake()
    {
        // 如果没有设置Animator，自动获取
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogError("TransitionController: 未找到Animator组件！");
        }
    }
    
    private void Start()
    {
        // 如果有指定的启动状态，直接播放；否则设置为初始状态
        if (!string.IsNullOrEmpty(startupTargetState))
        {
            animator.Play(startupTargetState);
            
            // 如果启动状态是Enter，需要设置为过渡状态并监听完成
            if (startupTargetState == STATE_ENTER)
            {
                isTransitioning = true;
                StartCoroutine(WaitForAnimationComplete(STATE_FULL, OnEnterAnimationComplete));
            }
        }
        else
        {
            // 确保初始状态正确
            SetInitState();
        }
    }
    
    #region 公共接口
    
    /// <summary>
    /// 播放进入动画（从空白到全屏）
    /// </summary>
    public void PlayEnterAnimation()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("TransitionController: 动画正在进行中，忽略新的动画请求");
            return;
        }
        
        // Debug.Log("TransitionController: 开始播放进入动画");
        isTransitioning = true;
        animator.SetTrigger(TRIGGER_ENTER);
        
        // 启动协程监听动画完成
        StartCoroutine(WaitForAnimationComplete(STATE_FULL, OnEnterAnimationComplete));
    }
    
    /// <summary>
    /// 播放退出动画（从全屏到空白）
    /// </summary>
    public void PlayExitAnimation()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("TransitionController: 动画正在进行中，忽略新的动画请求");
            return;
        }
        
        // Debug.Log("TransitionController: 开始播放退出动画");
        isTransitioning = true;
        animator.SetTrigger(TRIGGER_EXIT);
        
        // 启动协程监听动画完成
        StartCoroutine(WaitForAnimationComplete(STATE_INIT, OnExitAnimationComplete));
    }
    
    /// <summary>
    /// 设置为静止状态（全屏显示）
    /// </summary>
    public void SetStaticState()
    {
        // Debug.Log("TransitionController: 设置为静止状态");
        isTransitioning = false;
        
        // 直接跳转到Full状态
        animator.Play(STATE_FULL);
    }
    
    /// <summary>
    /// 设置为初始状态（空白）
    /// </summary>
    public void SetInitState()
    {
        // Debug.Log("TransitionController: 设置为初始状态");
        isTransitioning = false;
        
        // 直接跳转到Init状态
        animator.Play(STATE_INIT);
    }
    
    /// <summary>
    /// 设置启动时直接播放的状态（需要在Start之前调用）
    /// </summary>
    public void SetStartupState(string stateName)
    {
        startupTargetState = stateName;
    }
    
    /// <summary>
    /// 立即开始进入动画（创建时调用，避免初始化延迟）
    /// </summary>
    public void StartWithEnterAnimation()
    {
        SetStartupState(STATE_ENTER);
    }
    
    /// <summary>
    /// 立即设置为静止状态（创建时调用）
    /// </summary>
    public void StartWithStaticState()
    {
        SetStartupState(STATE_FULL);
    }
    
    /// <summary>
    /// 检查是否正在进行动画
    /// </summary>
    public bool IsTransitioning => isTransitioning;
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 等待动画完成
    /// </summary>
    private IEnumerator WaitForAnimationComplete(string targetState, System.Action onComplete)
    {
        // 等待一帧确保动画开始
        yield return null;
        
        // 等待动画到达目标状态
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(targetState))
        {
            yield return null;
        }
        
        // 如果目标状态不是循环状态，等待动画播放完成
        if (targetState == STATE_EXIT || targetState == STATE_ENTER)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            while (stateInfo.normalizedTime < 1.0f)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            }
        }
        
        // 动画完成
        isTransitioning = false;
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// 进入动画完成回调
    /// </summary>
    private void OnEnterAnimationComplete()
    {
        // Debug.Log("TransitionController: 进入动画完成");
        OnEnterComplete?.Invoke();
    }
    
    /// <summary>
    /// 退出动画完成回调
    /// </summary>
    private void OnExitAnimationComplete()
    {
        // Debug.Log("TransitionController: 退出动画完成");
        OnExitComplete?.Invoke();
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("测试播放进入动画")]
    public void DebugPlayEnter()
    {
        PlayEnterAnimation();
    }
    
    [ContextMenu("测试播放退出动画")]
    public void DebugPlayExit()
    {
        PlayExitAnimation();
    }
    
    [ContextMenu("测试设置静止状态")]
    public void DebugSetStatic()
    {
        SetStaticState();
    }
    
    #endregion
} 