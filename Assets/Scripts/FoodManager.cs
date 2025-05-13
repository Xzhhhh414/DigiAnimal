using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodManager : MonoBehaviour
{
    // 当前选中的食物
    private FoodController selectedFood;
    
    // 引用PetManager，用于取消宠物选择
    [SerializeField] private PetManager petManager;
    
    // 引用CameraController，用于控制摄像机状态
    [SerializeField] private CameraController cameraController;
    
    void Awake()
    {
        // 自动查找PetManager
        if (petManager == null)
        {
            petManager = FindObjectOfType<PetManager>();
        }
        
        // 自动查找CameraController
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }
    }
    
    void Update()
    {
        // 检测点击/触摸 - 用于选择食物
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // 检查是否点击在UI上，如果是则不处理食物选择
            if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
            {
                return; // 如果点击在UI上，则直接返回不处理
            }
            
            Vector2 touchPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            HandleFoodSelection(touchPosition);
        }
    }
    
    void HandleFoodSelection(Vector2 screenPosition)
    {
        // 从屏幕坐标发射射线
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // 如果点击到了食物
        if (hit.collider != null)
        {
            FoodController clickedFood = hit.collider.GetComponent<FoodController>();
            if (clickedFood != null)
            {
                // 如果点击的是已选中的食物，取消选中
                if (selectedFood == clickedFood)
                {
                    UnselectCurrentFood();
                    return;
                }
                
                // 选中新的食物
                SelectFood(clickedFood);
                return;
            }
        }
        
        // 点击空白处取消选中当前食物
        if (selectedFood != null)
        {
            UnselectCurrentFood();
        }
    }
    
    void SelectFood(FoodController food)
    {
        // 如果之前有选中的食物，先取消选中
        if (selectedFood != null)
        {
            selectedFood.SetSelected(false);
        }
        
        // 如果有选中的宠物，取消选中
        if (petManager != null && petManager.GetSelectedPet() != null)
        {
            // 这里直接调用PetManager中的UnselectCurrentPet方法或者触发PetUnselected事件
            // 因为PetManager中UnselectCurrentPet是private的，我们通过事件系统间接触发
            EventManager.Instance.TriggerEvent(CustomEventType.PetUnselected);
        }
        
        // 选中新的食物
        selectedFood = food;
        selectedFood.SetSelected(true);
        
        // 触发食物选中事件
        EventManager.Instance.TriggerEvent(CustomEventType.FoodSelected, selectedFood);
    }
    
    void UnselectCurrentFood()
    {
        if (selectedFood != null)
        {
            selectedFood.SetSelected(false);
            selectedFood = null;
            
            // 触发食物取消选中事件
            EventManager.Instance.TriggerEvent(CustomEventType.FoodUnselected);
        }
    }
    
    // 获取当前选中的食物，供其他脚本访问
    public FoodController GetSelectedFood()
    {
        return selectedFood;
    }
} 