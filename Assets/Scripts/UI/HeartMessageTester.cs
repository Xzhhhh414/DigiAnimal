using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// 爱心消息测试器 - 用于测试HeartMessageManager功能
/// </summary>
public class HeartMessageTester : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private KeyCode testKey = KeyCode.H; // 按H键测试
    [SerializeField] private KeyCode checkKey = KeyCode.J; // 按J键检查配置
    [SerializeField] private bool enableDebugGUI = true;
    
    private void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            TestHeartMessage();
        }
        
        if (Input.GetKeyDown(checkKey))
        {
            CheckConfiguration();
        }
    }
    
    [ContextMenu("测试爱心消息")]
    public void TestHeartMessage()
    {
        Debug.Log("=== 开始测试爱心消息 ===");
        
        // 查找场景中的宠物
        PetController2D pet = FindObjectOfType<PetController2D>();
        if (pet == null)
        {
            Debug.LogWarning("场景中未找到宠物！");
            return;
        }
        
        Debug.Log($"找到宠物: {pet.name}");
        
        // 查找HeartMessageManager
        HeartMessageManager heartManager = FindObjectOfType<HeartMessageManager>();
        if (heartManager == null)
        {
            Debug.LogWarning("场景中未找到HeartMessageManager！请添加HeartMessageManager组件。");
            return;
        }
        
        Debug.Log($"找到HeartMessageManager: {heartManager.name}");
        
        // 显示爱心获得消息
        heartManager.ShowHeartGainMessage(pet, 1);
        Debug.Log("测试爱心消息已发送");
    }
    
    [ContextMenu("检查配置")]
    public void CheckConfiguration()
    {
        Debug.Log("=== 检查HeartMessage系统配置 ===");
        
        // 检查HeartMessageManager
        HeartMessageManager heartManager = FindObjectOfType<HeartMessageManager>();
        if (heartManager == null)
        {
            Debug.LogError("❌ 未找到HeartMessageManager！请在场景中添加。");
            return;
        }
        else
        {
            Debug.Log("✅ 找到HeartMessageManager");
        }
        
        // 检查预制体配置
        var heartMessagePrefabField = typeof(HeartMessageManager).GetField("heartMessagePrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (heartMessagePrefabField != null)
        {
            GameObject prefab = heartMessagePrefabField.GetValue(heartManager) as GameObject;
            if (prefab == null)
            {
                Debug.LogError("❌ HeartMessage预制体未设置！请在Inspector中拖拽预制体。");
            }
            else
            {
                Debug.Log($"✅ 预制体已设置: {prefab.name}");
                
                // 检查预制体组件
                if (prefab.GetComponent<RectTransform>() == null)
                {
                    Debug.LogError("❌ 预制体缺少RectTransform组件！");
                }
                else
                {
                    Debug.Log("✅ 预制体有RectTransform组件");
                }
                
                if (prefab.GetComponentInChildren<Text>() == null)
                {
                    Debug.LogWarning("⚠️ 预制体中未找到Text组件，建议添加用于显示文本");
                }
                else
                {
                    Debug.Log("✅ 预制体有Text组件");
                }
                
                if (prefab.GetComponent<CanvasGroup>() == null)
                {
                    Debug.LogWarning("⚠️ 预制体缺少CanvasGroup组件，将自动添加");
                }
                else
                {
                    Debug.Log("✅ 预制体有CanvasGroup组件");
                }
            }
        }
        
        // 检查Canvas配置
        var targetCanvasField = typeof(HeartMessageManager).GetField("targetCanvas", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (targetCanvasField != null)
        {
            Canvas canvas = targetCanvasField.GetValue(heartManager) as Canvas;
            if (canvas == null)
            {
                Canvas foundCanvas = FindObjectOfType<Canvas>();
                if (foundCanvas == null)
                {
                    Debug.LogError("❌ 场景中未找到Canvas！");
                }
                else
                {
                    Debug.LogWarning($"⚠️ targetCanvas未设置，但场景中有Canvas: {foundCanvas.name}");
                }
            }
            else
            {
                Debug.Log($"✅ targetCanvas已设置: {canvas.name}");
            }
        }
        
        // 检查宠物
        PetController2D pet = FindObjectOfType<PetController2D>();
        if (pet == null)
        {
            Debug.LogError("❌ 场景中未找到宠物！");
        }
        else
        {
            Debug.Log($"✅ 找到宠物: {pet.name}");
        }
        
        // 检查Camera
        if (Camera.main == null)
        {
            Debug.LogError("❌ 未找到主摄像机！需要设置Camera的Tag为MainCamera。");
        }
        else
        {
            Debug.Log("✅ 找到主摄像机");
        }
        
        Debug.Log("=== 配置检查完成 ===");
    }
    
    private void OnGUI()
    {
        if (!enableDebugGUI) return;
        
        GUILayout.BeginArea(new Rect(10, 200, 300, 150));
        GUILayout.Label("爱心消息测试工具");
        
        if (GUILayout.Button("测试爱心获得提示"))
        {
            TestHeartMessage();
        }
        
        if (GUILayout.Button("检查系统配置"))
        {
            CheckConfiguration();
        }
        
        GUILayout.Label($"按 {testKey} 键快速测试");
        GUILayout.Label($"按 {checkKey} 键检查配置");
        
        GUILayout.EndArea();
    }
} 