// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Elumenati/StereoCombiner"
{
	Properties
	{
		_Left("Left (RGB)", 2D) = "white" {}
		_Right("Right (RGB)", 2D) = "white" {}
		gApparentDepth("gApparentDepth", float) = 4.001  // replace with NvAPI_Stereo_GetConvergenceDepth()
		_ViewportOffset("_ViewportOffset", float) = 0  // .5 if offset by .5
		_Debug("_Debug", float) = 0.25  // .5 if offset by .5
	}
		Category
		{
			Tags {"Queue" = "Geometry+1" }
			Alphatest Greater 0
			ZWrite ON
			ColorMask RGB
			Cull Off
			Ztest Always
			SubShader
			{
				Pass
				{
				Blend SrcAlpha OneMinusSrcAlpha  // alpha blend
				CGPROGRAM
				#include "UnityCG.cginc"
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 5.0
				struct v2f
				{
			//float3	normal:NORMAL;
			float4 pos : SV_POSITION;
			float4 posnormal :FOG;
			float2	uv: TEXCOORD0;
			float2	uv2: TEXCOORD1;
		};

		uniform float4 _Left_ST;
		uniform float4 _Right_ST;
		uniform float gApparentDepth;

		v2f vert(appdata_base v)
		{
			v2f o;
			o.uv = TRANSFORM_TEX(v.texcoord, _Left);
			o.uv2 = TRANSFORM_TEX(v.texcoord, _Right);
			o.posnormal = o.pos = UnityObjectToClipPos(v.vertex);
			o.pos *= (gApparentDepth / o.pos.w);
			return o;
		}

		sampler2D _Left;
		sampler2D _Right;
		uniform float _ViewportOffset = 0;
		uniform float _Debug = .25;

		half4 frag(v2f i) : COLOR
		{
			float p1 = (i.posnormal.x + 1) / 2;
			float p2 = i.pos.x / _ScreenParams.x - (_ViewportOffset * 2);

			float WhichEye = (p2 - p1) * 100000000;
			/*
			if(p2 > _Debug)
			WhichEye = 1;else
			WhichEye = 0;
			*/

			return	lerp(tex2D(_Left, frac(i.uv.xy)),tex2D(_Right, frac(i.uv2.xy)),WhichEye > .5);
		}
		ENDCG
		}
	}
		}
}