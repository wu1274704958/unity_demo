Shader "SurfaceEff/Immerse"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_PowN("PowN", Range(1, 10)) = 2.0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			float _PowN;
			fixed4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
				float2 uv = abs(i.uv - float2(0.5,0.5));
				float m = pow(length(uv), _PowN);
				float4 u = float4(m,m,m,1.0);
				return col - u;
			}
			ENDCG
		}
	}
}
