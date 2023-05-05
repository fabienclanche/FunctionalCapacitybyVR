//Copyright 2020 Julie#8169 STREAM_DOGS#4199
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
//files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
//modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the 
//Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
//BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
//OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Shader "Handimator/LiquidSphere"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1) 
        _Specular ("Color", Color) = (1,1,1,1) 
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _LiquidLevel ("LiquidLevel", Range(-1,1)) = 0.0 
        _SurfaceTension("Surface Crispness", Float) = 0.05
        _Opacity("Opacity", Range(0,0.9999999)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "true" "ForceNoShadowCasting"="True" }
        LOD 0 

        CGPROGRAM
        #pragma surface surf Liquid alpha:fade vertex:vert
        #pragma target 3.0
           
        struct Input
        {
            float rayIntersection; 
            float3 debug;
        };
        
        half _Glossiness, _Metallic, _LiquidLevel, _SurfaceTension, _Opacity;
        fixed4 _Color;
 
        half4 LightingLiquid (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) 
        {
            half lscat = (1-s.Alpha) * pow(max(0, dot(lightDir, -viewDir)), 10/(s.Alpha));
            //half diffuse = max(lscat,max(0,dot (s.Normal, lightDir)));
            //float nh = max(0, dot(s.Normal, normalize (lightDir + viewDir)));
            //float spec = pow (nh, 48+1000*_Glossiness); 

            half4 c;
            //c.rgb = (diffuse * s.Albedo + lscat + spec * (1-s.Albedo)) * _LightColor0.rgb * atten;
            c.rgb = lscat * _LightColor0.rgb * atten;
            c.a = min(1,lscat);
            return c;
        }

        void vert (inout appdata_full v, out Input o)
        {
            #include "LiquidSphere.vert.cginc"
        }
               
        void surf (Input IN, inout SurfaceOutput o)
        { 
            o.Alpha = 1 - pow(1 - _Opacity, max(0, min(1, IN.rayIntersection)));
        }
        ENDCG

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha:fade vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {  
            float rayIntersection; 
            float3 debug;
        };

        half _Glossiness, _Metallic, _LiquidLevel, _SurfaceTension, _Opacity;
        fixed4 _Color;

        void vert (inout appdata_full v, out Input o)
        {
            #include "LiquidSphere.vert.cginc"
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        { 
            o.Albedo = pow(_Color, max(0, min(1, IN.rayIntersection)));      
            o.Alpha = 1 - pow(1 - _Opacity, max(0, min(1, IN.rayIntersection)));
            o.Smoothness = min(1,_Glossiness/(o.Alpha+0.0001));
            o.Metallic = _Metallic;
        }
        ENDCG

    }
    FallBack "Diffuse"
}
