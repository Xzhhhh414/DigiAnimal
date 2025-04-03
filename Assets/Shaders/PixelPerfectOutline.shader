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
        Blend SrcAlpha OneMinusSrcAlpha

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
                
                // 如果当前像素是不透明的（属于精灵内部），保持原始颜色不变
                if (col.a >= _AlphaCutoff)
                {
                    return col;
                }

                // 检查周围是否有不透明像素（是否靠近精灵边缘）
                float2 pixelSize = _MainTex_TexelSize.xy * _OutlineSize;
                
                // 采样周围像素的alpha值（上、右、下、左四个方向）
                float neighborAlpha = 0;
                neighborAlpha += tex2D(_MainTex, i.uv + float2(0, pixelSize.y)).a;
                neighborAlpha += tex2D(_MainTex, i.uv + float2(pixelSize.x, 0)).a;
                neighborAlpha += tex2D(_MainTex, i.uv + float2(0, -pixelSize.y)).a;
                neighborAlpha += tex2D(_MainTex, i.uv + float2(-pixelSize.x, 0)).a;
                
                // 如果周围有不透明像素，但当前像素是透明的，则显示描边
                if (neighborAlpha > _AlphaCutoff)
                {
                    // 返回描边颜色，确保Alpha值为1
                    return fixed4(_OutlineColor.rgb, 1.0);
                }
                
                // 完全透明（不渲染任何东西）
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
} 