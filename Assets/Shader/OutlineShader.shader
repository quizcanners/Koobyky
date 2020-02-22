Shader "Custom/OutlineShader" {
	Properties{
		//_Color ("Color", Color) = (1,1,1,1)
	}
		Category{
		Tags{ "Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"LightMode" = "ForwardBase"
	}

	Blend SrcAlpha OneMinusSrcAlpha
		ColorMask RGB
		Cull Back

		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc"



	//float4 _Color;
	float4 _OutlineColor;
	float4 _touchPoint;
	struct v2f {
		float4 pos : POSITION;
		float2 texcoord : TEXCOORD0;  
	};


	v2f vert(appdata_full v) {
		v2f o;

		o.pos = UnityObjectToClipPos(v.vertex);    
		o.texcoord.xy = v.texcoord;

		return o;
	}



	float4 frag(v2f i) : COLOR{

	float dist = length(i.texcoord.xy-_touchPoint.xy);

	i.texcoord.xy -= 0.5;

	i.texcoord = (abs(i.texcoord)-0.45)*20;
	float a = max(max(i.texcoord.x, 0), i.texcoord.y);
	a = min (a,1-a)*2;
	_OutlineColor*=a*(1+(1-dist)*_touchPoint.z);

	return  _OutlineColor;


	}
		ENDCG
	}
	}
	}
}
