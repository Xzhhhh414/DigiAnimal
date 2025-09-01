using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetManager : MonoBehaviour
{
    // 单例模式
    private static PetManager _instance;
    public static PetManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PetManager>();
            }
            return _instance;
        }
    }
    // 当前选中的宠物
    private PetController2D selectedPet;
    
    // 是否有UI交互正在发生 - 暂未使用
    #pragma warning disable 0414
    private bool isUIInteraction = false;
    #pragma warning restore 0414
    
    void Awake()
    {
        // 单例模式设置
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Update()
    {
        // 检测点击/触摸 - 仅用于选择宠物，不再用于移动
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // 检查是否点击在UI上，如果是则不处理宠物选择
            if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
            {
                return; // 如果点击在UI上，则直接返回不处理
            }
            
            Vector2 touchPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            HandlePetSelection(touchPosition);
        }
    }
    
    void HandlePetSelection(Vector2 screenPosition)
    {
        // 从屏幕坐标发射射线
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // 如果点击到了宠物
        if (hit.collider != null)
        {
            PetController2D clickedPet = hit.collider.GetComponent<PetController2D>();
            if (clickedPet != null)
            {
                // 如果点击的是已选中的宠物，取消选中
                if (selectedPet == clickedPet)
                {
                    UnselectCurrentPet();
                    return;
                }
                
                // 选中新的宠物
                SelectPet(clickedPet);
                return;
            }
        }
        
        // 如果没有点击到宠物，取消当前宠物选中
        if (selectedPet != null)
        {
            UnselectCurrentPet();
        }
    }
    
    void SelectPet(PetController2D pet)
    {
        // 如果之前有选中的宠物，先取消选中
        if (selectedPet != null)
        {
            selectedPet.SetSelected(false);
            // 不在这里触发取消选中事件，避免连续触发两个事件
        }
        
        // 如果有选中的家具，取消选中
        if (FurnitureManager.Instance != null && FurnitureManager.Instance.GetSelectedFurniture() != null)
        {
            FurnitureManager.Instance.UnselectCurrentFurniture();
        }
        
        // 选中新的宠物
        selectedPet = pet;
        selectedPet.SetSelected(true);
        
        // 触发宠物选中事件
        EventManager.Instance.TriggerEvent(CustomEventType.PetSelected, selectedPet);
    }
    
    void UnselectCurrentPet()
    {
        if (selectedPet != null)
        {
            selectedPet.SetSelected(false);
            selectedPet = null;
            
            // 触发宠物取消选中事件
            EventManager.Instance.TriggerEvent(CustomEventType.PetUnselected);
        }
    }
    
    // 获取当前选中的宠物，供其他脚本访问
    public PetController2D GetSelectedPet()
    {
        return selectedPet;
    }
} 