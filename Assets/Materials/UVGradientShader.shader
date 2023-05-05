Shader "Custom/UVGradientShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Alpha ("Alpha", Range(0,1)) = 0.0
		_Emission ("Glow Strength", Range(0,1)) = 0.0 

		_Exp("Gradient Smoothing", Range(0,100)) = 1

		_UVCenterX("UV Center U", Range(0,1)) = 0.5
		_UVCenterY("UV Center V", Range(0,1)) = 0.5

		_StrengthX("Strength U", Range(0,1)) = 1
		_StrengthY("Strength V", Range(0,1)) = 1

    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "ForceNoShadowCasting"="True" }  
        LOD 200

        CGPROGRAM 
        #pragma surface surf Standard alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
		half _Alpha;
		half _Emission;
        fixed4 _Color;
		float _UVCenterX;
		float _UVCenterY;
		float _StrengthX;
		float _StrengthY;
		float _Exp;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        { 
			half dx = abs(_UVCenterX - IN.uv_MainTex.x) / max(_UVCenterX, abs(1 - _UVCenterX));
			half dy = abs(_UVCenterY - IN.uv_MainTex.y) / max(_UVCenterY, abs(1 - _UVCenterY));
			half rimLighting =  pow(max(dx * dx * _StrengthX, dy * dy * _StrengthY), _Exp);

            o.Metallic = _Metallic * rimLighting;
			o.Smoothness = _Glossiness * rimLighting;
			o.Alpha = rimLighting * _Alpha; 
			o.Emission = _Color * rimLighting * rimLighting * _Emission;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
