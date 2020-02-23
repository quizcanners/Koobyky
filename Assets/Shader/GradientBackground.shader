﻿Shader "Playtime Painter/Effects/Screen-SpaceGradient" {
	Properties{
		[PerRendererData]_MainTex("Mask (RGB)", 2D) = "white" {}
		//_Center("Center Position", Range(0,2)) = 0
		//_CenterSharpness("Center Sharpness", Range(0,5)) = 0
	}

	Category{
		Tags{
			"Queue" = "Background"
			"PreviewType" = "Plane"
			"IgnoreProjector" = "True"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off

		SubShader{
			Pass{

				CGPROGRAM
				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma multi_compile ______ USE_NOISE_TEXTURE
				#pragma target 3.0

				struct v2f {
					float4 pos : SV_POSITION;
					float4 screenPos : TEXCOORD1;
				};

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.screenPos = ComputeScreenPos(o.pos);
					return o;
				}

			//	float _Center;
			//	float _CenterSharpness;
				sampler2D _Global_Noise_Lookup;
				float4 _BG_CENTER_COL;
				float4 _BG_GRAD_COL_1;
				float4 _BG_GRAD_COL_2;

				float4 frag(v2f i) : COLOR{

					float2 duv = i.screenPos.xy / i.screenPos.w;

				

					//float up = saturate((_Center - duv.y)*(1 + _CenterSharpness));

					//float center = pow(saturate((0.5 - abs(_Center - duv.y))*_BG_CENTER_COL.a * 2), _CenterSharpness + 1);

					//center += center * (1 - center);

					#ifdef UNITY_COLORSPACE_GAMMA

					float4 col = _BG_GRAD_COL_1 * _BG_GRAD_COL_1 *(1 - duv.y) + _BG_GRAD_COL_2 * _BG_GRAD_COL_2 *(duv.y);
					//col = col * (1 - center) + _BG_CENTER_COL * _BG_CENTER_COL*center;
					col.rgb = sqrt(col.rgb);
					#else
					float4 col = _BG_GRAD_COL_1  *(1 - up) +  _BG_GRAD_COL_2 *(up);
					col = col * (1 - center) +  _BG_CENTER_COL*center;
					#endif

					#if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(duv.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						#ifdef UNITY_COLORSPACE_GAMMA
							col.rgb += (noise.rgb - 0.5)*0.02;
						#else
							col.rgb += (noise.rgb - 0.5)*0.0075;
						#endif
					#endif

					return col;
				}
				ENDCG

			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
