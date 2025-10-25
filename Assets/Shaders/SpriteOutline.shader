Shader "Universal Render Pipeline/2D/Sprite-Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
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
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 采样原始纹理
                fixed4 c = tex2D(_MainTex, i.texcoord);
                
                // 如果当前像素不透明，直接返回原色
                if (c.a > 0.01)
                {
                    c *= i.color;
                    c.rgb *= c.a;
                    return c;
                }
                
                // 当前像素透明，检查周围是否有不透明像素来绘制描边
                float2 pixelSize = _MainTex_TexelSize.xy * _OutlineSize;
                
                fixed alphaSum = 0;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(0, pixelSize.y)).a;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(0, -pixelSize.y)).a;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(-pixelSize.x, 0)).a;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(pixelSize.x, 0)).a;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(-pixelSize.x, pixelSize.y)).a;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(pixelSize.x, pixelSize.y)).a;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(-pixelSize.x, -pixelSize.y)).a;
                alphaSum += tex2D(_MainTex, i.texcoord + float2(pixelSize.x, -pixelSize.y)).a;
                
                // 如果周围有不透明像素，绘制描边
                if (alphaSum > 0.01)
                {
                    fixed4 outline = _OutlineColor;
                    outline.rgb *= outline.a;
                    return outline;
                }
                
                // 完全透明
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}

