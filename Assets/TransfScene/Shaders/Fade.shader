Shader "TransfScene/Fade" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
 
	SubShader 
	{ 
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

		Pass 
		{
            Blend SrcAlpha OneMinusSrcAlpha
            Zwrite off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			//#include "UnityCG.cginc"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "./fade.hlsl"

			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex;
			float4 _MainTex_ST;
			half4 _Color;

			CBUFFER_END
			
			struct appdata {
			  float3 pos : POSITION;
			  float2 uv : TEXCOORD0;
			};

			struct v2f {
			  float2 uv : TEXCOORD0;
			  float4 pos : SV_POSITION;
			};

			// vertex shader
			v2f vert (appdata IN) 
			{
			  v2f o;
			  o.uv = IN.uv;
			  o.pos = TransformObjectToHClip(IN.pos);
			  return o;
			}

			// fragment shader
			half4 frag (v2f IN) : SV_Target 
			{
			  half4 col;
              col.rgb = float3(1,1,1);
			  float2 scr_uv = IN.pos.xy / _ScreenParams.xy;
			  CalculateFadeAlpha(scr_uv,col.a);
			  return col;
			}
			ENDHLSL
		}
	}
 
Fallback "VertexLit"
}