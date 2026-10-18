Shader "Rewind/PS1Lit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _Emission ("Emission", Color) = (0,0,0,0)
        _SnapResolution ("Snap Resolution (xy)", Vector) = (320, 180, 0, 0)
        _Affine ("Affine Warp", Range(0,1)) = 1
        _Ambient ("Ambient", Color) = (0.35, 0.33, 0.42, 1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _Emission;
                float4 _SnapResolution;
                float _Affine;
                half4 _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uvw : TEXCOORD0;
                float2 uvPersp : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 cs = TransformObjectToHClip(v.positionOS.xyz);
                float2 grid = _SnapResolution.xy * 0.5;
                cs.xy = floor(cs.xy / cs.w * grid + 0.5) / grid * cs.w;
                o.positionCS = cs;

                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvw = float3(uv * cs.w, cs.w);
                o.uvPersp = uv;

                float3 n = TransformObjectToWorldNormal(v.normalOS);
                Light main = GetMainLight();
                half ndl = saturate(dot(n, main.direction));
                half3 lit = _Ambient.rgb + main.color * ndl;
                o.color = half4(v.color.rgb * lit, v.color.a);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 affineUV = i.uvw.xy / i.uvw.z;
                float2 uv = lerp(i.uvPersp, affineUV, _Affine);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 c = tex * _Color * i.color;
                c.rgb += _Emission.rgb;
                return c;
            }
            ENDHLSL
        }
    }
}
