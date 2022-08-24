Shader "Custom/Unlit"
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
 
	SubShader 
	{ 
		Tags 
        { 
            // "RenderType" = "Transparent" 
            // "Queue" = "Transparent" 
            // "RenderType" = "Transparent" 
            // "Queue" = "Transparent" 
        }

		Pass 
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			//#include "UnityCG.cginc"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex;
			float4 _MainTex_ST;
			half4 _Color;
			half _BlenderFactor;
			CBUFFER_END
			
			struct appdata 
            {
			  float3 pos : POSITION;
			  float2 uv : TEXCOORD0;
			};

			struct v2f 
            {
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
			  col.rgb = tex2D (_MainTex, IN.uv.xy).rgb * _Color.rgb * 2;
              col.a = 1;
			  return col;
			}
			ENDHLSL
		}
	}
 
Fallback "VertexLit"
}