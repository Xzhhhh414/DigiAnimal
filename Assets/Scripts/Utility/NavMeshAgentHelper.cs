using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent 安全操作工具类
/// </summary>
public static class NavMeshAgentHelper
{
    /// <summary>
    /// 安全地重置 NavMeshAgent 路径
    /// 避免在游戏停止或组件被销毁时出现错误
    /// </summary>
    /// <param name="agent">要重置的 NavMeshAgent</param>
    /// <returns>是否成功重置</returns>
    public static bool SafeResetPath(NavMeshAgent agent)
    {
        if (agent == null)
            return false;
            
        // 检查游戏对象是否有效
        if (agent.gameObject == null)
            return false;
            
        // 检查游戏对象是否激活
        if (!agent.gameObject.activeInHierarchy)
            return false;
            
        // 检查组件是否启用
        if (!agent.enabled)
            return false;
            
        // 检查是否在 NavMesh 上
        if (!agent.isOnNavMesh)
            return false;
            
        try
        {
            agent.ResetPath();
            return true;
        }
        catch (System.Exception e)
        {
            // 在编辑器中显示警告，但不影响游戏运行
            Debug.LogWarning($"NavMeshAgent ResetPath failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 安全地设置 NavMeshAgent 目标位置
    /// </summary>
    /// <param name="agent">NavMeshAgent</param>
    /// <param name="destination">目标位置</param>
    /// <returns>是否成功设置</returns>
    public static bool SafeSetDestination(NavMeshAgent agent, Vector3 destination)
    {
        if (agent == null)
            return false;
            
        // 检查游戏对象是否有效
        if (agent.gameObject == null)
            return false;
            
        // 检查游戏对象是否激活
        if (!agent.gameObject.activeInHierarchy)
            return false;
            
        // 检查组件是否启用
        if (!agent.enabled)
            return false;
            
        // 检查是否在 NavMesh 上
        if (!agent.isOnNavMesh)
            return false;
            
        try
        {
            return agent.SetDestination(destination);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"NavMeshAgent SetDestination failed: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 检查 NavMeshAgent 是否处于有效状态
    /// </summary>
    /// <param name="agent">要检查的 NavMeshAgent</param>
    /// <returns>是否有效</returns>
    public static bool IsValid(NavMeshAgent agent)
    {
        return agent != null && 
               agent.gameObject != null && 
               agent.gameObject.activeInHierarchy && 
               agent.enabled && 
               agent.isOnNavMesh;
    }
} 