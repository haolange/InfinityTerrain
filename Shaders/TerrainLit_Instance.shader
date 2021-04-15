Shader "Terrain/TerrainLit_Instance"
{
	Properties
	{
		_MainTex ("BaseColor", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_Cubemap ("Reflection Cubemap", Cube) = "Gray" {}
	}
	
	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		
		Pass
		{
			Name "VTFeedback"
			Tags{"LightMode" = "VTFeedback"}
			//Conservative True

			HLSLPROGRAM
				#pragma vertex FeedbackVert
				#pragma fragment FeedbackPixel
				#pragma enable_d3d11_debug_symbols
				#include "TerrainFeedbackPass.hlsl"
			ENDHLSL
		}

		Pass
		{
            Name "TerrainLit"
            Tags{"LightMode" = "TerrainLit"}
			//Conservative True

			HLSLPROGRAM
				#pragma vertex ShadingVert
				#pragma fragment ShadingPixel
				#pragma enable_d3d11_debug_symbols
				#pragma multi_compile _ _SHADOWS_SOFT
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
				#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
				#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
				#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
				#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
				#include "TerrainShadingPass.hlsl"
			ENDHLSL
		}
	}
}
