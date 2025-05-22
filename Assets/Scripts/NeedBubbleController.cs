using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PetNeedType
{
    None = 0,
    Hungry = 1,
    Tired = 2,
    // 后续可添加更多需求类型
}

public class NeedBubbleController : MonoBehaviour
{
    [Header("气泡引用")]
    [SerializeField] private GameObject bubbleObject;
    [SerializeField] private SpriteRenderer statusIconRenderer;
    
    [Header("状态图标")]
    [SerializeField] private Sprite hungryIcon;
    [SerializeField] private Sprite tiredIcon;
    // 其他状态图标...
    
    [Header("浮动效果设置")]
    [SerializeField] private float floatSpeed = 1.0f;
    [SerializeField] private float floatAmount = 0.1f;
    
    private PetNeedType currentNeed = PetNeedType.None;
    private Vector3 startPosition;
    
    // 优先级配置，数值越高优先级越高
    private Dictionary<PetNeedType, int> needPriorities = new Dictionary<PetNeedType, int>
    {
        { PetNeedType.None, 0 },
        { PetNeedType.Hungry, 100 },
        { PetNeedType.Tired, 90 },
        // 可添加更多需求类型及其优先级
    };
    
    private void Start()
    {
        // 初始时隐藏气泡并保存初始位置
        if (bubbleObject != null)
        {
            startPosition = bubbleObject.transform.localPosition;
            
            // 确保气泡初始隐藏
            currentNeed = PetNeedType.None;
            bubbleObject.SetActive(false);
        }
    }
    
    private void Update()
    {
        // 如果气泡可见，应用浮动效果
        if (bubbleObject != null && bubbleObject.activeSelf)
        {
            // 使用正弦函数创建上下浮动效果
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            bubbleObject.transform.localPosition = new Vector3(
                startPosition.x, 
                newY, 
                startPosition.z);
        }
    }
    
    /// <summary>
    /// 显示指定类型的需求气泡
    /// </summary>
    /// <param name="needType">需求类型</param>
    public void ShowNeed(PetNeedType needType)
    {
        // 安全检查
        if (needType == PetNeedType.None) return;
        
        // 如果当前没有需求显示，或者新需求优先级更高
        if (currentNeed == PetNeedType.None || 
            needPriorities[needType] > needPriorities[currentNeed])
        {
            // 更新当前需求
            currentNeed = needType;
            
            // 根据需求类型设置相应的图标
            if (statusIconRenderer != null)
            {
                switch (needType)
                {
                    case PetNeedType.Hungry:
                        statusIconRenderer.sprite = hungryIcon;
                        break;
                    case PetNeedType.Tired:
                        statusIconRenderer.sprite = tiredIcon;
                        break;
                    // 其他需求类型...
                }
            }
            
            // 显示气泡 - 确保气泡可见
            if (bubbleObject != null)
            {
                bubbleObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 隐藏指定类型的需求气泡
    /// </summary>
    /// <param name="needType">需求类型</param>
    public void HideNeed(PetNeedType needType)
    {
        // 如果要隐藏的正是当前显示的需求
        if (needType == currentNeed)
        {
            currentNeed = PetNeedType.None;
            
            // 隐藏气泡
            if (bubbleObject != null)
            {
                bubbleObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 隐藏所有需求气泡
    /// </summary>
    public void HideAllNeeds()
    {
        currentNeed = PetNeedType.None;
        
        // 隐藏气泡
        if (bubbleObject != null)
        {
            bubbleObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 获取当前显示的需求类型
    /// </summary>
    public PetNeedType GetCurrentNeed()
    {
        return currentNeed;
    }
} 