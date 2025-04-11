using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetManager : MonoBehaviour
{
    // 当前选中的宠物
    private CharacterController2D selectedPet;
    
    // 是否有UI交互正在发生 - 暂未使用
    #pragma warning disable 0414
    private bool isUIInteraction = false;
    #pragma warning restore 0414
    
    void Update()
    {
        // 检测点击/触摸
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
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
            CharacterController2D clickedPet = hit.collider.GetComponent<CharacterController2D>();
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
        
        // 如果点击到了地面，且有宠物被选中，让宠物移动到点击位置
        if (selectedPet != null)
        {
            // 将触摸/点击位置转换为世界坐标
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0; // 确保z坐标为0
            
            // 让选中的宠物移动到点击位置
            selectedPet.MoveTo(worldPosition);
        }
    }
    
    void SelectPet(CharacterController2D pet)
    {
        // 如果之前有选中的宠物，先取消选中
        if (selectedPet != null)
        {
            selectedPet.SetSelected(false);
        }
        
        // 选中新的宠物
        selectedPet = pet;
        selectedPet.SetSelected(true);
    }
    
    void UnselectCurrentPet()
    {
        if (selectedPet != null)
        {
            selectedPet.SetSelected(false);
            selectedPet = null;
        }
    }
    
    // 获取当前选中的宠物，供其他脚本访问
    public CharacterController2D GetSelectedPet()
    {
        return selectedPet;
    }
} 