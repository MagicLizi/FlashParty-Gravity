Shader "UI/HollowRectUI"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _HoleCenter ("Hole Center (0-1 UV)", Vector) = (0.5, 0.5, 0, 0)
        _HoleSize ("Hole Size (0-1 UV)", Vector) = (0.2, 0.2, 0, 0)
		_HoleRadius ("Hole Corner Radius (0-1 UV)", Range(0,0.5)) = 0.05
        _ClipRect ("Clip Rect", Vector) = (0,0,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _HoleCenter;
            float4 _HoleSize;
			float4 _ClipRect;
			float _HoleRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                float2 screenUV : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;

                float4 world = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = world.xy;

                float2 ndc = o.vertex.xy / o.vertex.w; // -1..1
                o.screenUV = ndc * 0.5 + 0.5;          // 0..1
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // UGUI RectMask2D clipping
                float clip = UnityGet2DClipping(i.worldPos, _ClipRect);
                if (clip <= 0.0)
                {
                    discard;
                }

                fixed4 c = tex2D(_MainTex, i.uv) * i.color;

			// Hollow rounded-rectangle using SDF (transparent inside)
			float2 halfSize = _HoleSize.xy * 0.5;
			float r = min(min(halfSize.x, halfSize.y), _HoleRadius);
			float2 q = abs(i.screenUV - _HoleCenter.xy) - (halfSize - r);
			float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
			if (dist <= 0.0)
			{
				c.a = 0.0;
			}
                return c;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}


