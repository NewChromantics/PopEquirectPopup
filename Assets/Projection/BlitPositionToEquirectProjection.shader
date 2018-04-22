Shader "New Chromantics/Blit Position To Equirect Projection"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		EquirectTexture("EquirectTexture", 2D ) = "black" {}
		EyePosition("EyePosition", VECTOR ) = (0,0,0,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../PopUnityCommon/PopCommon.cginc"




			struct appdata
			{
				float4 LocalPos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 ScreenPos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			#define PositionTexture	_MainTex
			sampler2D EquirectTexture;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float3 EyePosition;

			float3 GetEyePosition()
			{
				return EyePosition;
			}
		

			v2f vert (appdata v)
			{
				v2f o;
				o.ScreenPos = UnityObjectToClipPos(v.LocalPos);
				o.uv = v.uv;
				return o;
			}


			fixed4 frag (v2f i) : SV_Target
			{
				float4 WorldPos = tex2D( PositionTexture, i.uv );
				float3 ViewDir = GetEyePosition() - WorldPos.xyz;
				float2 EquirectUv = ViewToEquirect( ViewDir );

				//	gr: why upside down?
				EquirectUv.y = 1 - EquirectUv.y;

				float4 Colour = tex2D( EquirectTexture, EquirectUv );
				/*
				if ( WorldPos.w < 1 )
					Colour = float4(0,0,0,0);
					*/
				return Colour;
			}
			ENDCG
		}
	}
}
