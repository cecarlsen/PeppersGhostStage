Shader "FX/ReflectiveFoil"
{
	Properties
	{
		_Color ("Color", Color) = ( 0.5, 0.5, 0.5, 0.1 )
		[HideInInspector] _ReflectionTex ("", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha
 
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			sampler2D _ReflectionTex;

			struct v2f
			{
				float4 refl : TEXCOORD0;
				float4 pos : SV_POSITION;
			};


			v2f vert( float4 pos : POSITION )
			{
				v2f o;
				o.pos = UnityObjectToClipPos( pos.xyz );
				o.refl = ComputeScreenPos( o.pos );
				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 refl = tex2Dproj( _ReflectionTex, UNITY_PROJ_COORD( i.refl ) );
				refl.rgb *= _Color.rgb;
				refl.a = _Color.a;
				return refl;
			}
			ENDCG
	    }
	}
}