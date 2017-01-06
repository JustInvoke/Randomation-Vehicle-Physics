// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Tires Bumped"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Occlusion ("Occlusion", 2D) = "white" {}
		_Normal ("Normal", 2D) = "bump" {}
		_Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.0
		_DeformMap ("Deform Map", 2D) = "white" {}
		_DeformNormal ("Deform Normal",Vector) = (0,0,0,0)
	}

	SubShader
	{   
        Tags
        {
        	"Queue" = "Geometry"
            "RenderType" = "Opaque"
        }
         
		CGINCLUDE
 		#define _GLOSSYENV 1
		ENDCG
         
		CGPROGRAM
		#pragma target 3.0
 		#include "UnityPBSLighting.cginc"
		#pragma surface surf Standard vertex:vert addshadow
		#pragma exclude_renderers gles

		sampler2D _MainTex;
		sampler2D _Occlusion;
		sampler2D _Normal;
		fixed4 _Color;
		fixed _Smoothness;

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			fixed4 occ = tex2D(_Occlusion, IN.uv_MainTex);
			fixed3 normal = UnpackScaleNormal(tex2D(_Normal, IN.uv_MainTex), 1);
			o.Albedo = c.rgb;
			o.Occlusion = occ.rgb;
			o.Smoothness = _Smoothness;
			o.Normal = normal;
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