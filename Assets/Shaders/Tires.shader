// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Tires"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_DeformMap ("Deform Map", 2D) = "white" {}
		_DeformNormal ("Deform Normal",Vector) = (0,0,0,0)
	}

	SubShader
	{
		Tags {"RenderType"="Opaque"}
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert addshadow
		#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
		}

		sampler2D _DeformMap;
		float4 _DeformNormal;
				    
		//The deformation
		void vert (inout appdata_full v)
		{
			float4 tex = tex2Dlod (_DeformMap, float4(v.texcoord.xy,0,0));
			float _SquishDot = saturate(dot(-v.normal,normalize(mul(unity_WorldToObject,_DeformNormal).xyz)));
			v.vertex.xyz += (mul(unity_WorldToObject, _DeformNormal).xyz * _SquishDot * 1.1 + v.normal * pow(_SquishDot, 1.5) * length(mul(unity_WorldToObject, _DeformNormal).xyz) * (1 - saturate(dot(v.normal, normalize(mul(unity_WorldToObject, _DeformNormal).xyz))))) * tex.rgb;
		}

		ENDCG
	}

	Fallback "Standard"
}