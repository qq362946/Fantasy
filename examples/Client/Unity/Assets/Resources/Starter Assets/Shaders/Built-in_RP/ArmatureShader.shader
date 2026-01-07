Shader "Starter Assets/ArmatureShader"
{
    Properties
    {
        //Maps
        _BaseTex("Albedo (RGB)", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _MetallicMap("MetallicMap", 2D) = "black"{}
        _MaterialMask("Mask", 2D) = "black" {}

        //base - mask texture R
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _BaseMetallic("Base Metallic", Range(0,1)) = 1
        _BaseGlossiness("Base Smoothness", Range(0,1)) = 0.5

        //layer 1 - mask texture G
        _Layer1Color("Layer1 Color", Color) = (1,1,1,1)
        _Layer1Glossiness("Layer1 Smoothness", Range(0,1)) = 0.5
        _Layer1Metallic("Layer1 Metallic", Range(0,1)) = 0.0

        //layer 2 - mask texture B
        _Layer2Color("Layer2 Color", Color) = (1,1,1,1)
        _Layer2Glossiness("Layer2 Smoothness", Range(0,1)) = 0.5
        _Layer2Metallic("Layer2 Metallic", Range(0,1)) = 0.0
        
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard fullforwardshadows

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            sampler2D _BaseTex;
            sampler2D _BumpMap;
            sampler2D _MaterialMask;

            struct Input
            {
                float2 uv_BaseTex;
            };

            half _BaseMetallic;
            half _BaseGlossiness;

            half _Layer1Glossiness;
            half _Layer1Metallic;

            half _Layer2Glossiness;
            half _Layer2Metallic;

            fixed4 _BaseColor;
            fixed4 _Layer1Color;
            fixed4 _Layer2Color;

            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)


            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo map
                fixed4 mainCall = tex2D(_BaseTex, IN.uv_BaseTex);
                fixed4 materialMask = tex2D(_MaterialMask, IN.uv_BaseTex);

                //setup colors for each layer
                fixed4 baseColor = mainCall * _BaseColor;
                fixed4 layer1Color = mainCall * _Layer1Color;
                fixed4 layer2Color = mainCall * _Layer2Color;

                fixed4 color = lerp(lerp( baseColor, layer1Color, materialMask.g), layer2Color ,materialMask.b);
                o.Albedo = color.rgb;
                
                // Normal Map
                fixed3 normalMap = UnpackNormal (tex2D(_BumpMap, IN.uv_BaseTex));
                o.Normal = normalMap;

                // Metallic
                fixed4 baseMetallic = _BaseMetallic;
                fixed4 layer1Metallic = _Layer1Metallic;
                fixed4 layer2Metallic = _Layer2Metallic;

                fixed4 metallic = lerp(lerp(baseMetallic, layer1Metallic, materialMask.g), layer2Metallic, materialMask.b);
                o.Metallic = metallic.r;

                // Smoothness
                fixed4 g = mainCall.a;
                fixed4 baseGlossiness = g * _BaseGlossiness;
                fixed4 layer1Glossiness = g * _Layer1Glossiness;
                fixed4 layer2Glossiness = g * _Layer2Glossiness;

                fixed4 glossiness = lerp(lerp(baseGlossiness, layer1Glossiness, materialMask.g), layer2Glossiness, materialMask.b);
                o.Smoothness = glossiness.r;

                o.Alpha = color.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}
