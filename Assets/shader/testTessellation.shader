Shader "shaders/testTessellation"
{
    Properties
    {
	
		[IntRange]_EdgeTess("EdgeTess", Range(1,63)) = 1
    }
    SubShader
    {
        Pass 
        {
            CGPROGRAM
			#include "HLSLSupport.cginc"
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"
			#include "Tessellation.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"


            #pragma vertex vert
            #pragma hull HS
            #pragma domain DS 
            #pragma fragment frag
			#pragma multi_compile_prepassfinal
			#pragma target 5.0
			
			float4 _HitPos = float4(0,0,0,0);
            
            struct app_data
            {
                float4 positionOS:POSITION;
				float3 normal0:NORMAL0;
				float2 uv0:TEXCOORD0;
				
            };
            struct VertexOut
            {
                float3 PosL:TEXCOORD;
				float3 normal0:NORMAL0;
				float2 uv0:TEXCOORD1;
            };
            VertexOut vert(app_data IN)
            {
                VertexOut o;
                o.PosL=IN.positionOS.xyz;
				o.normal0 = IN.normal0;
				o.uv0 = IN.uv0;
                return o;
            }
            
            struct PatchTess
            {
                float EdgeTess[3]:SV_TessFactor;
                float InsideTess:SV_InsideTessFactor;
            };
			
			float _EdgeTess;
            PatchTess ConstantHS(InputPatch<VertexOut,3> patch,uint patchID:SV_PrimitiveID)
            {
                PatchTess pt;

				float3 pos0 = mul(unity_ObjectToWorld ,float4(patch[0].PosL,1.0));
				float3 pos1 = mul(unity_ObjectToWorld ,float4(patch[1].PosL,1.0));
				float3 pos2 = mul(unity_ObjectToWorld ,float4(patch[2].PosL,1.0));
				

                pt.EdgeTess[0] = max(step(distance(pos0,_HitPos.xyz),0.15) * 32,1);
                pt.EdgeTess[1] = max(step(distance(pos1,_HitPos.xyz),0.15) * 32,1);
                pt.EdgeTess[2] = max(step(distance(pos2,_HitPos.xyz),0.15) * 32,1);
				pt.EdgeTess[0] = max(max(pt.EdgeTess[1],pt.EdgeTess[2]),pt.EdgeTess[0]);
				pt.EdgeTess[1] = pt.EdgeTess[2] = pt.EdgeTess[0];
                pt.InsideTess = (pt.EdgeTess[0] + pt.EdgeTess[1] +pt.EdgeTess[2])/3;
                return pt;
            }
            
            
            struct HullOut
            {
                float3 PosL:TEXCOORD0;
				float3 normal0:NORMAL0;
				float2 uv0:TEXCOORD1;
            };
            
            [domain("tri")]
            [partitioning("integer")]
            [outputtopology("triangle_cw")]
            [outputcontrolpoints(3)]
            [patchconstantfunc("ConstantHS")]
            [maxtessfactor(64.0f)]
            HullOut HS(InputPatch<VertexOut,3> p,uint i:SV_OutputControlPointID)
            {
                HullOut hout;
                hout.PosL=p[i].PosL;
				hout.normal0 = p[i].normal0;
				hout.uv0 = p[i].uv0;
                return hout;
            }
            
            struct DomainOut
            {
                float4 PosH:SV_POSITION;    
				float4 color:half4;
            };
            [domain("tri")]
            DomainOut DS(PatchTess patchTess,float3 baryCoords:SV_DomainLocation,const OutputPatch<HullOut,3> triangles)
            {
                DomainOut dout;              
                float3 p=triangles[0].PosL*baryCoords.x+triangles[1].PosL*baryCoords.y+triangles[2].PosL*baryCoords.z;

				float2 uv = triangles[0].uv0 * baryCoords.x + triangles[1].uv0 * baryCoords.y + triangles[2].uv0 * baryCoords.z;
				float3 normal = triangles[0].normal0 * baryCoords.x + triangles[1].normal0 * baryCoords.y + triangles[2].normal0 * baryCoords.z;
				//uv.x = lerp(uv.x,1-uv.x,step(0.5,uv.x));
				//uv.y = lerp(uv.y,1-uv.y,step(0.5,uv.y));

				//p += triangles[0].normal0 * sqrt( length(uv)) * -0.03;

				float3 pos = mul(unity_ObjectToWorld ,float4(p,1));
				float dist = distance(pos,_HitPos.xyz);
				p -= step(dist,0.1) * (0.1 - dist) * normal;

                dout.PosH=UnityObjectToClipPos(p.xyz);
				dout.color = half4(uv,0,1);  
                return dout;
            }
            half4 frag(DomainOut IN):SV_Target
            {
                return  IN.color; 
            }            
            ENDCG
        }
    }
}

