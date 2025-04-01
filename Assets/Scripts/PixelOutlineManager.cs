using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PixelOutlineManager : MonoBehaviour
{
    [Header("描边设置")]
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField] private float outlineSize = 1f;
    [SerializeField] private float alphaCutoff = 0.1f;
    
    [Header("高级设置")]
    [SerializeField] private Shader pixelOutlineShader;
    [SerializeField] private bool outlineOnAwake = false;
    
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material outlineMaterial;
    private bool isSelected = false;
    
    private static readonly int IsSelectedProperty = Shader.PropertyToID("_IsSelected");
    private static readonly int OutlineSizeProperty = Shader.PropertyToID("_OutlineSize");
    private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
    private static readonly int AlphaCutoffProperty = Shader.PropertyToID("_AlphaCutoff");
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
        
        // 初始化描边材质
        InitializeOutlineMaterial();
        
        // 如果需要，在Awake时激活描边
        if (outlineOnAwake)
        {
            SetOutlineActive(true);
        }
    }
    
    private void InitializeOutlineMaterial()
    {
        // 如果没有指定shader，尝试查找项目中的shader
        if (pixelOutlineShader == null)
        {
            pixelOutlineShader = Shader.Find("Custom/PixelPerfectOutline");
            if (pixelOutlineShader == null)
            {
                Debug.LogError("未找到像素描边Shader，请确保'Custom/PixelPerfectOutline'存在于项目中");
                return;
            }
        }
        
        // 创建描边材质
        outlineMaterial = new Material(pixelOutlineShader);
        
        // 设置材质属性
        UpdateShaderProperties();
    }
    
    private void OnDestroy()
    {
        // 清理创建的材质
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
    }
    
    /// <summary>
    /// 设置描边是否激活
    /// </summary>
    public void SetOutlineActive(bool active)
    {
        isSelected = active;
        
        // 根据状态切换材质
        if (spriteRenderer != null && outlineMaterial != null)
        {
            if (isSelected)
            {
                spriteRenderer.material = outlineMaterial;
                outlineMaterial.SetFloat(IsSelectedProperty, 1.0f);
            }
            else
            {
                spriteRenderer.material = originalMaterial;
            }
        }
    }
    
    /// <summary>
    /// 更新所有shader属性
    /// </summary>
    private void UpdateShaderProperties()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor(OutlineColorProperty, outlineColor);
            outlineMaterial.SetFloat(OutlineSizeProperty, outlineSize);
            outlineMaterial.SetFloat(AlphaCutoffProperty, alphaCutoff);
            outlineMaterial.SetFloat(IsSelectedProperty, isSelected ? 1.0f : 0.0f);
        }
    }
    
    /// <summary>
    /// 设置描边颜色
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        UpdateShaderProperties();
    }
    
    /// <summary>
    /// 设置描边大小
    /// </summary>
    public void SetOutlineSize(float size)
    {
        outlineSize = size;
        UpdateShaderProperties();
    }
    
    /// <summary>
    /// 在编辑器中更改属性时更新shader
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && outlineMaterial != null)
        {
            UpdateShaderProperties();
        }
    }
} 