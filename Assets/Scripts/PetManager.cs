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
    
    // 在Awake中自动选择一个初始宠物
    void Awake()
    {
        // 在下一帧选择初始宠物，确保所有宠物已经被初始化
        StartCoroutine(SelectRandomPetAtStart());
    }
    
    // 协程在游戏开始时选择随机宠物
    private IEnumerator SelectRandomPetAtStart()
    {
        // 等待一帧，确保所有宠物都已经初始化
        yield return null;
        
        // 查找所有标签为"Pet"的游戏对象
        GameObject[] pets = GameObject.FindGameObjectsWithTag("Pet");
        
        // 如果找到了宠物
        if (pets != null && pets.Length > 0)
        {
            // 随机选择一个宠物
            int randomIndex = Random.Range(0, pets.Length);
            GameObject randomPet = pets[randomIndex];
            
            // 获取宠物的CharacterController2D组件
            CharacterController2D petController = randomPet.GetComponent<CharacterController2D>();
            
            // 如果有CharacterController2D组件，选择它
            if (petController != null)
            {
                Debug.Log($"游戏开始时随机选择了宠物: {randomPet.name}");
                SelectPet(petController);
            }
        }
        else
        {
            Debug.LogWarning("未找到任何标记为'Pet'的宠物！");
        }
    }
    
    void Update()
    {
        // 检测点击/触摸 - 仅用于选择宠物，不再用于移动
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
        
        // 新增：点击空白处取消选中当前宠物
        if (selectedPet != null)
        {
            UnselectCurrentPet();
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