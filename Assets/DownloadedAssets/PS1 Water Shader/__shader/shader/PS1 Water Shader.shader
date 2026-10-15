Shader "PS1 Water Shader/Lit Water"
{  
	Properties  
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_Color("Color Tint", Color) = (1, 1, 1, 1)
		_SpecGlossMap("Specular Map", 2D) = "white" {}
		_SpecColor("Specular Color", Color) = (0, 0, 0, 1)
		_Glossiness("Smoothness", Range(0.01, 1.0)) = 0.5
		[HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
		[HDR] _EmissionMap("Emission Map", 2D) = "black" {}

		_VertJitter("Jitter", Range(0.0, 0.999)) = 0.95
		_AffineMapIntensity("Affine Texture Mapping", Range(0.0, 1.0)) = 1.0
		
	}

	SubShader  
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "ForceNoShadowCasting" = "True"}
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask RGB
		
		Pass
		{
			Tags { LightMode = Vertex }

			CGPROGRAM
			 
			#pragma vertex vert  
			#pragma fragment frag	
			#pragma multi_compile_fog
			#pragma shader_feature_local ENABLE_SCREENSPACE_JITTER			
			#pragma shader_feature_local USING_SPECULAR_MAP
			#pragma shader_feature_local EMISSION_ENABLED 
			#pragma shader_feature_local USING_EMISSION_MAP 
			#include "UnityCG.cginc"
			#include "./cginfiles/shadermetadata.cginc"

			ENDCG
		}
	}

	Fallback "VertexLit"
}