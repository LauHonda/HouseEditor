Shader "Custom/Grid" {
	Properties {
		_GridSpacing ("Grid Spacing", Float) = 1
		_GridThickness ("Grid Thickness", Range(0,0.1)) = 0.02
		_GridColor ("Grid Color", Color) = (0.5,0.5,0.5,1)
		_Opacity ("Opacity", Range(0,1)) = 1
		_MapWidth ("Map Width", Float) = 64
		_MapHeight ("Map Height", Float) = 64
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Lambert alpha

		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		half _GridSpacing;
		half _GridThickness;
		half _Opacity;
		fixed4 _GridColor;

		half _MapWidth;
		half _MapHeight;

		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

		fixed inRange(float value,float minValue, float maxValue) {
			return max(sign(value - minValue), 0.0)*max(sign(maxValue - value), 0.0);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			float xmod = fmod(abs(IN.worldPos.x)+_GridSpacing/2, _GridSpacing);
			float zmod = fmod(abs(IN.worldPos.z)+_GridSpacing/2, _GridSpacing);

			float gridLowerBound = _GridThickness - _GridSpacing;

			float halfWidth = _MapWidth / 2;
			float halfHeight = _MapHeight /2;

			float isGridCoord = inRange(IN.worldPos.x, halfWidth - _MapWidth, halfWidth) *
								inRange(IN.worldPos.z, halfHeight - _MapHeight, halfHeight) *
							((xmod < _GridThickness) + (xmod < gridLowerBound) +
							 (zmod < _GridThickness) + (zmod < gridLowerBound));

			 
			o.Albedo = _GridColor * isGridCoord;
			o.Alpha = isGridCoord*_Opacity;

		}
		ENDCG
	}
	FallBack "Diffuse"
}
