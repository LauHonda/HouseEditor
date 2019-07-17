//This shader makes the windows and open doors see through
Shader "Custom/DepthMask" {
	SubShader {
		Tags {"Queue" = "Geometry+10" }
		ColorMask 0
		ZWrite On

		Pass {}
	}
}
