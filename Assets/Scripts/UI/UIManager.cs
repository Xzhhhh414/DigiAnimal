using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class UIManager : MonoBehaviour
{
    [Header("UI引用")]
    public Canvas gameCanvas;
    
    [Header("UI面板")]
    [SerializeField] private SelectedPetInfo selectedPetInfoPanel;
    
    // 单例模式
    public static UIManager Instance { get; private set; }
    
    private void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化UI组件
        InitializeUI();
    }
    
    public void Start()
    {
        // 确保GameCanvas存在
        if (gameCanvas == null)
        {
            Debug.LogError("UIManager: GameCanvas未设置！请在Inspector中设置GameCanvas引用。");
            gameCanvas = FindObjectOfType<Canvas>();
        }
    }
    
    // 初始化UI组件
    private void InitializeUI()
    {
        // 查找SelectedPetInfo面板（如果没有通过Inspector设置）
        if (selectedPetInfoPanel == null)
        {
            selectedPetInfoPanel = FindObjectOfType<SelectedPetInfo>();
            
            if (selectedPetInfoPanel == null)
            {
                Debug.LogWarning("UIManager: 未找到SelectedPetInfo面板，某些功能可能无法正常工作。");
            }
        }
    }
    
    // 显示选中宠物信息面板
    public void ShowSelectedPetInfo(CharacterController2D pet)
    {
        if (selectedPetInfoPanel != null)
        {
            selectedPetInfoPanel.gameObject.SetActive(true);
        }
    }
    
    // 隐藏选中宠物信息面板
    public void HideSelectedPetInfo()
    {
        if (selectedPetInfoPanel != null)
        {
            selectedPetInfoPanel.gameObject.SetActive(false);
        }
    }
}
