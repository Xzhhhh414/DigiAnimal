Shader "Custom/PixelPerfectOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Float) = 1
        _AlphaCutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.1
        _IsSelected ("Is Selected", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float _AlphaCutoff;
            float _IsSelected;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // 如果未选中，直接返回原始颜色
                if (_IsSelected < 0.5) return col;
                
                // 如果是完全透明的像素，提前返回
                if (col.a < _AlphaCutoff) return fixed4(0, 0, 0, 0);

                // 检查周围是否有透明像素 - 只在边缘添加描边
                bool isOutlinePixel = false;
                
                // 精确的一像素偏移，保持像素风格
                float2 pixelSize = _MainTex_TexelSize.xy * _OutlineSize;
                
                // 采样上、右、下、左四个方向
                float alpha = 0;
                alpha += tex2D(_MainTex, i.uv + float2(0, pixelSize.y)).a;
                alpha += tex2D(_MainTex, i.uv + float2(pixelSize.x, 0)).a;
                alpha += tex2D(_MainTex, i.uv + float2(0, -pixelSize.y)).a;
                alpha += tex2D(_MainTex, i.uv + float2(-pixelSize.x, 0)).a;
                
                // 更强的像素风格，仅使用四个基本方向
                isOutlinePixel = (col.a > _AlphaCutoff) && (alpha < 4.0 * _AlphaCutoff);
                
                // 如果是边缘像素，使用描边颜色
                if (isOutlinePixel)
                {
                    return _OutlineColor;
                }
                
                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
} 