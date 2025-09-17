using UnityEngine;

public class BedController : MonoBehaviour
{
    // 床是否正在被使用
    [SerializeField]
    private bool _isUsing = false;
    
    // 公开的属性，用于获取和设置床的使用状态
    public bool IsUsing
    {
        get { return _isUsing; }
        set { _isUsing = value; }
    }
} 