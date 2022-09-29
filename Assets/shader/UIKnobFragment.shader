Shader "UI/KnobFragment"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
//		_Origin("Origin",Vector) = (0,0,0,0)
//		_Point("Point",Vector) = (0,0,0,0)
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
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
				Ref[_Stencil]
				Comp[_StencilComp]
				Pass[_StencilOp]
				ReadMask[_StencilReadMask]
				WriteMask[_StencilWriteMask]
			}

			Cull Off
			Lighting Off
			ZWrite Off
			ZTest[unity_GUIZTestMode]
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask[_ColorMask]

			Pass
			{
				Name "Default"
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0

				#include "UnityCG.cginc"
				#include "UnityUI.cginc"

				#pragma multi_compile __ UNITY_UI_ALPHACLIP

				struct appdata_t
				{
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
					float2 texcoord1 : TEXCOORD1;
					float3 normal : NORMAL;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 vertex   : SV_POSITION;
					fixed4 color : COLOR;
					float2 texcoord  : TEXCOORD0;
					float4 localPos : TEXCOORD1;
					float2 texcoord1 : TEXCOORD2;
					float3 normal : NORMAL;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				fixed4 _Color;
				fixed4 _TextureSampleAdd;
				float4 _ClipRect;
				// float4 _Origin;
				// float4 _Point;

				v2f vert(appdata_t IN)
				{
					v2f OUT;
					UNITY_SETUP_INSTANCE_ID(IN);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
					OUT.localPos = IN.vertex;
					OUT.vertex = UnityObjectToClipPos(IN.vertex);
					OUT.texcoord1 = IN.texcoord1;
					OUT.texcoord = IN.texcoord;
					OUT.normal = IN.normal;
					OUT.color = IN.color * _Color;
					return OUT;
				}

				sampler2D _MainTex;

				fixed4 frag(v2f IN) : SV_Target
				{
					half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

					color.a *= UnityGet2DClipping(IN.vertex.xy, _ClipRect);
					
					// float2 pos = UnityObjectToViewPos(float3(IN.localPos.xy,0));
					// float2 o = UnityObjectToViewPos(float3(_Origin.xy,0));
					// float2 near = UnityObjectToViewPos(float3(_Point.xy,0));
					// float2 far = UnityObjectToViewPos(float3(_Point.zw,0));
					// float n = length(near - o);
					// float f = length(far - o);
					// float angle = radians(_Origin.z * 0.5f);
					
					#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
					#endif
					// if(abs(length(pos - o) - n) <= 1.f)
					//return fixed4(IN.texcoord1,0,color.a);
					if(abs(length(IN.texcoord1)) >= 1.0f)
						return fixed4(0,0,0,color.a);
					// if(abs(acos(dot(normalize(IN.localPos.xy - o),down)) - angle) <= 0.009f)
					//return fixed4(IN.texcoord1,0,color.a);
					
					return color;
				}
			ENDCG
			}
		}
}

