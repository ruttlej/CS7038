﻿Shader "Sprites/Pie"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1, 1, 1, 1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[MaterialToggle] Clockwise ("Clockwise", Float) = 1
		Value ("Value", Range(0, 1)) = 0
		AxisX ("Direction X", Float) = 0
		AxisY ("Direction Y", Float) = -1
		PivotX ("Pivot X", Float) = 0.5
		PivotY ("Pivot Y", Float) = 0.5
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
			
		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile DUMMY PIXELSNAP_ON
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color;

			v2f vert(appdata_t IN) {
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				#ifndef DUMMY
					OUT.vertex = UnityPixelSnap (OUT.vertex); 
				#endif
				return OUT;
			}

			sampler2D _MainTex;
			float PivotX;
			float PivotY;
			float AxisX;
			float AxisY;
			float Value;
			float Clockwise;

			fixed4 frag(v2f IN) : COLOR
			{
				float4 OUT = tex2D(_MainTex, IN.texcoord) * IN.color;
				//Direction of the pie edge
				float2 axis = normalize(float2(AxisX, AxisY));
				//Direction from origin to pixel
				float2 dir = normalize(IN.texcoord - float2(PivotX, PivotY));
				//The sign of z determines which half of the circle the dot product is for
				float z = normalize(cross(float3(axis, 0), float3(dir, 0)).z);
				//The dot product is the cosine of the angle between two vectors:
				float prod = dot(axis, dir);
				//Do some weird math magic to deal with angles > 180
				prod = 0.25 * z * (1 - prod) + 0.5;
				//Apply clockwise modifier
				prod = Clockwise * (1 - 2 * prod) + prod;
				
				//What follows is equivalent to this if statement:
				//if ( Value > 0.5 && prod  > Value || Value <= 0.5 && prod >= Value) {
				//	OUT.a = 0;
				//}
				float mod = normalize(clamp(Value-0.5, 0, 1));
				float diff = normalize(prod - Value);
				OUT.a = OUT.a * ((1 - clamp(diff, 0, 1) * mod
					+ (clamp(-diff, 0, 1) - 1) * (1-mod)));
				
				return OUT;
			}
		ENDCG
		}
	}
}
	