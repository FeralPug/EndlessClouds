// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Clouds"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _NoiseTex ("NoiseTex", 2D) = "white" {}
        _NoiseTexST1("Noise Tex ST 1", vector) = (1, 1, 1, 1)
        _NoiseTexST2("Noise Tex ST 2", vector) = (1, 1, 1, 1)
        _NoiseDir1 ("Noise Dir1 (xy)", vector) = (0, 0, 0, 0)
        _NoiseDir2 ("Noise Dir2 (xy)", vector) = (0, 0, 0, 0)
        _NoiseSpeed1 ("Noise Speed 1", float) = 0
        _NoiseSpeed2 ("Noise Speed 2", float) = 0
        _CloudShadowAmount ("Cloud Shadow Amount", float) = .1
        _CloudShadowValue ("Cloud Shadow Value", float) = .5
        _CloudColorLB("Cloud Color LB", Range(0, 1)) = .5
        _AlphaThresh("Alpha Thresh", Range(0, 1)) = .15
        _SubSurfaceSize("SubSurface Size", Range(0, 1)) = .9
        _WorldBendingAmount("World Bending Amount", Range(0.000001, 0.01)) = 0.00001
        _FadeMin("Fade Min", float) = 900
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        Cull off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"

            struct DrawVertex
            {
                float3 worldPos;
                float normHeight;
            };

            struct DrawTriangle
            {
                DrawVertex verts[3];
            };

            StructuredBuffer<DrawTriangle> DrawTriangles;

            struct VertexOutput
            {
                float4 worldPosAndHeight : TEXCOORD0;
                float4 screenPos : TEXCOORD1;

                float4 clipPos : SV_POSITION;
            };

        sampler2D _CameraDepthTexture;

                    //properties

        fixed4 _Color;
        sampler2D _NoiseTex;

        float4 _NoiseTexST1, _NoiseTexST2;
        float4 _NoiseDir1, _NoiseDir2;
        float _NoiseSpeed1, _NoiseSpeed2;
        float _CloudShadowAmount;
        float _CloudShadowValue;
        
        float _CloudColorLB;
        float _AlphaThresh;
        float _SubSurfaceSize;
        float _WorldBendingAmount;  
        float _FadeMin;       



        

            VertexOutput vert (uint vertex_ID : SV_VERTEXID)
            {
                VertexOutput v = (VertexOutput)0;

                DrawTriangle tri = DrawTriangles[vertex_ID / 3];
                DrawVertex vert = tri.verts[vertex_ID % 3];

                v.worldPosAndHeight.xyz = vert.worldPos;
                v.worldPosAndHeight.w = vert.normHeight;

                //bending
                float3 dist = abs(v.worldPosAndHeight.xyz - _WorldSpaceCameraPos.xyz);
                
                float y = dist.z;
                y = pow(y, 2);
                y *= -_WorldBendingAmount;
        

                float x = dist.x;
                x = pow(x, 2);
                x *= -_WorldBendingAmount;
    
                float amount = x + y;

                v.clipPos = mul(UNITY_MATRIX_VP, float4(v.worldPosAndHeight.x, amount + v.worldPosAndHeight.y, v.worldPosAndHeight.z, 1));

                v.screenPos = ComputeScreenPos(v.clipPos);

                return v;
            }

        float InverseLerp(float x, float min, float max){
            return saturate((x - min) / (max - min));
        }

        float Remap(float x, float oldMin, float oldMax, float newMin, float newMax){
            float f = InverseLerp(x, oldMin, oldMax);
            return lerp(newMin, newMax, f);
        }

            fixed4 frag (VertexOutput i) : SV_Target
            {

            float linearEyeDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.screenPos.xy / i.screenPos.w));
            clip(linearEyeDepth - 1);
                
            float2 worldPos = i.worldPosAndHeight.xz;

            float heightGradient = sin(i.worldPosAndHeight.w * UNITY_PI);

            float noise1 = tex2D(_NoiseTex, worldPos * _NoiseTexST1.xy + _NoiseDir1.xy * _Time.y * _NoiseSpeed1 + _NoiseTexST1.zw).r;
            float noise2 = tex2D(_NoiseTex, worldPos * _NoiseTexST2.xy + _NoiseDir2.xy * _Time.y * _NoiseSpeed2 + _NoiseTexST2.zw).r;

            float shadow1 = tex2D(_NoiseTex, worldPos * _NoiseTexST1.xy + _NoiseDir1.xy * _Time.y * _NoiseSpeed1 + _NoiseTexST1.zw - _WorldSpaceLightPos0.xz * _CloudShadowAmount).r;
            float shadow2 = tex2D(_NoiseTex, worldPos * _NoiseTexST2.xy + _NoiseDir2.xy * _Time.y * _NoiseSpeed2 + _NoiseTexST2.zw - _WorldSpaceLightPos0.xz * _CloudShadowAmount).r;

            float shadow = shadow1 * shadow2;
            float nightValue = 1 - (dot(-_WorldSpaceLightPos0.xyz, float3(0, 1, 0)) * .5 + .5);

            float noise = noise1 * noise2;

            float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPosAndHeight.xyz);
            float SubSurface = dot(-viewDir, _WorldSpaceLightPos0.xyz);
            SubSurface = smoothstep(_SubSurfaceSize, 1, SubSurface);

            float cloudColorMod = Remap(noise, 0, 1, _CloudColorLB, 1);
    
            float4 col;
            col.rgb = _Color * cloudColorMod;
            col.rgb *= saturate(shadow + _CloudShadowValue);
            col.rgb = saturate(col.rgb + SubSurface) * nightValue;

            col.a = smoothstep(_AlphaThresh, 1, noise * heightGradient);

            float eyeDistance = length(float3(_WorldSpaceCameraPos.x, 0, _WorldSpaceCameraPos.z) - float3(i.worldPosAndHeight.x, 0, i.worldPosAndHeight.z));

            float distanceFade = InverseLerp(eyeDistance, _FadeMin, _ProjectionParams.z);
            col.a = lerp(col.a, 0, distanceFade);

            return col;
            }
            ENDCG
        }
    }
}
