Shader "Vegetation/GrassUnlit" 
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
            // Zwrite off
			// ZTest Always

			HLSLPROGRAM
			#pragma multi_compile_instancing 
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			//#include "UnityCG.cginc"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex;
			float4 _MainTex_ST;

			half4 _Color;

			CBUFFER_END
			
			struct appdata 
            {
			  float3 pos : POSITION;
			  float3 uv : TEXCOORD0;
			  UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f 
            {
			  float2 uv : TEXCOORD0;
			  float4 pos : SV_POSITION;
			  UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// vertex shader
			v2f vert (appdata IN) 
			{
			  v2f o;
				UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN,o);
			  o.uv = IN.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
			  o.pos = TransformObjectToHClip(IN.pos);
			  return o;
			}

			// fragment shader
			half4 frag (v2f IN) : SV_Target 
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				half4 col;
				col = tex2D (_MainTex, IN.uv.xy) * _Color;
                col.a = saturate(col.a);
				return col;
			}
			ENDHLSL
		}
	}
 
Fallback "VertexLit"
}