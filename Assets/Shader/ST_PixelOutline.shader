
Shader "Standart/ST_PixelOutline" {
	Properties{
		_GridTex ("_MainTex", 2D) = "white" {}
		_ExplodedTex ("_ExplodedTex", 2D) = "white" {}
		_Bump("_bump", 2D) = "white" {}
		_Smudge("_smudge", 2D) = "white" {}
		_BumpEx("_bumpEx", 2D) = "white" {}
		_BumpDetail("_bumpDetail", 2D) = "white" {}
		//_ParticlesTex("Particles_Tex", 2D) = "white" {}
	}

	Category{
		Tags{ 
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
		}


		ColorMask RGB
		Cull Back

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#pragma multi_compile ___ USE_NOISE_TEXTURE
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"

				sampler2D _Bump;
				sampler2D _BumpDetail;
				sampler2D _GridTex; 
				sampler2D _BumpEx;
				sampler2D _ExplodedTex;
				sampler2D _Smudge;
				sampler2D _ParticlesTex;
				float4 _GridTex_TexelSize;
				uniform float4 _GridTex_ST;
				float4 _touchPoint;
				float4 _OutlineColor;
				sampler2D _Global_Noise_Lookup;

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;  
					float4 hold : TEXCOORD1;
					float2 perfuv : TEXCOORD2; 
				};


				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);    
					v.texcoord.zw = (1-v.texcoord.zw)*_GridTex_ST.xy+ _GridTex_ST.zw;


					o.perfuv.xy = (floor(v.texcoord.zw*_GridTex_TexelSize.z)+0.5)* _GridTex_TexelSize.x;

					o.texcoord.xy = (1-v.texcoord.xy)*_GridTex_ST.xy+ _GridTex_ST.zw;
					o.texcoord.zw = (1-_touchPoint.xy)*_GridTex_ST.xy+ _GridTex_ST.zw;


					float2 up = (v.texcoord.zw)*_GridTex_TexelSize.z;
					float2 bord = up;
					up = floor(up);
					bord = bord - up - 0.5;
					float2 hold = bord * 2;
					hold *= _GridTex_TexelSize.x;
					up = (up + 0.5)* _GridTex_TexelSize.x;

					float4 c = tex2Dlod(_GridTex, float4(up, 0, 0));
					float4 contact = tex2Dlod(_GridTex, float4(up + float2(hold.x, 0), 0, 0));
					float4 contact2 = tex2Dlod(_GridTex, float4(up + float2(0, hold.y), 0, 0));
					float4 contact3 = tex2Dlod(_GridTex, float4(up + float2(hold.x, hold.y), 0, 0));

					hold *= _GridTex_TexelSize.z /5.5;

					bord = abs(bord);

					float4 difff = abs(contact - c);
					float xsame = saturate((0.2 - (difff.r + difff.g + difff.b + difff.a)) * 165800);
					difff = abs(contact2 - c);
					float ysame = saturate((0.2 - (difff.r + difff.g + difff.b + difff.a)) * 165800);
					difff = abs(contact3 - c);
					float ddiff = saturate((0.1 - (difff.r + difff.g + difff.b + difff.a)) * 165800);

					float diag =saturate((1-ddiff)*xsame*ysame* 165800);

					o.hold.z = diag;

					o.hold.w = 1-diag;

					o.hold.x = 1-saturate(xsame);
					o.hold.y = 1-saturate(ysame);

				return o;
				}



				float4 frag(v2f i) : COLOR{


					//float2 fromFinger = (1.1-i.perfuv.xy) - _touchPoint.xy;

					//return length(fromFinger)* _touchPoint.z;

					float2 off = (i.texcoord.xy - i.perfuv.xy);

					float2 bumpUV = off*_GridTex_TexelSize.z;//+0.5; //bord - up;

					float4 c = tex2Dlod(_GridTex, float4(i.perfuv.xy, 0, 0));

					//c.rgb = normalize(c.rgb);

					float2 border =  (abs(float2(bumpUV.x,bumpUV.y))-0.4)*10;
					float bord = max(0,max(border.x*i.hold.x,border.y*i.hold.y)*i.hold.w+i.hold.z*min(border.x,border.y));

					bumpUV.x = bumpUV.x*max(i.hold.x,i.hold.z);
					bumpUV.y = bumpUV.y*max(i.hold.y,i.hold.z);

					bumpUV+=0.5;

					float2 nn = UnpackNormal(
						tex2Dlod(_Bump, float4(bumpUV,0,0)) *(1- i.hold.z)
						+tex2Dlod(_BumpEx, float4(bumpUV,0,0)) *(i.hold.z)).xy;

						float2 nn2 = UnpackNormal(tex2Dlod(_BumpDetail, float4(i.texcoord.xy*(4+nn.rg), 0, 0)));
						nn+=nn2*(_touchPoint.z*2+1-c.a)*0.05;

					float side = length(nn)*2;


					float smudge = tex2D(_Smudge, i.texcoord.xy * 2+nn*0.1).a;
					float deSmudge = 1 - smudge;

					float dist = length(i.texcoord.xy-i.texcoord.zw+nn*0.1);

					float2 sat = (abs(off) * 512);
					float2 pixuv = i.perfuv.xy + off*min(1,sat*0.03);

					float4 light =(tex2Dlod(_GridTex, float4(pixuv+(nn)*(0.1+(0.3* smudge)),0,0)));

					//float particles = tex2Dlod(_ParticlesTex, float4(i.texcoord.xy * 3,0,0));

					float4 col = (
		
						(c + _OutlineColor*(pow(dist,3)*_touchPoint.z)*smudge
						+ light*0.25*(1 + side) //* side
							)
						+ tex2Dlod(_ExplodedTex, float4(pixuv+(nn)*0.2,0,0)) * 2 * deSmudge //* particles
	
					)
						*(1-bord);

					float3 bgr = col.gbr+col.brg;
					bgr*=bgr;

					col.rgb+=bgr*0.1;
	
					/*
					#if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(i.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
					#ifdef UNITY_COLORSPACE_GAMMA
						col.rgb += col.rgb*(noise.rgb - 0.5)*0.1;
					#else
						col.rgb += col.rgb*(noise.rgb - 0.5)*0.75;
					#endif
					#endif
					*/
						return  col;
					;

				}
				ENDCG
			}
		}
	}
}
