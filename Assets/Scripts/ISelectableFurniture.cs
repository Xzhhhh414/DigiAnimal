using UnityEngine;

/// <summary>
/// 可选中家具接口
/// 所有可选中的家具物件都需要实现这个接口
/// </summary>
public interface ISelectableFurniture
{
    /// <summary>
    /// 家具类型名称（用于UI显示和事件触发）
    /// </summary>
    string FurnitureType { get; }
    
    /// <summary>
    /// 家具实例名称
    /// </summary>
    string FurnitureName { get; }
    
    /// <summary>
    /// 是否被选中
    /// </summary>
    bool IsSelected { get; set; }
    
    /// <summary>
    /// 获取GameObject组件
    /// </summary>
    GameObject GameObject { get; }
    
    /// <summary>
    /// 选中家具时调用
    /// </summary>
    void OnSelected();
    
    /// <summary>
    /// 取消选中家具时调用
    /// </summary>
    void OnDeselected();
    
    /// <summary>
    /// 获取家具的图标（用于UI显示）
    /// </summary>
    Sprite GetIcon();
}