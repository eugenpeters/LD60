Shader "Rewind/CRT"
{
    Properties
    {
        [PerRendererData] _MainTex ("Scene", 2D) = "black" {}
        _Curvature ("Curvature", Range(0, 0.3)) = 0.1
        _Scanlines ("Scanline Intensity", Range(0, 1)) = 0.35
        _ScanlineCount ("Scanline Count", Float) = 270
        _Mask ("RGB Mask Intensity", Range(0, 1)) = 0.25
        _Aberration ("Chromatic Aberration", Range(0, 0.02)) = 0.0015
        _Vignette ("Vignette", Range(0, 2)) = 0.6
        _Noise ("Noise", Range(0, 1)) = 0.04
        _Flicker ("Flicker", Range(0, 0.2)) = 0.02
        _Glitch ("Glitch", Range(0, 1)) = 0
        _Invert ("Invert", Range(0, 1)) = 0
        _Brightness ("Brightness", Float) = 1.1
        _Flash ("White Flash", Range(0, 1)) = 0
        _Lens ("Lens (xy pos, z radius, w strength)", Vector) = (0.5, 0.5, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off ZWrite Off ZTest Always
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Curvature, _Scanlines, _ScanlineCount, _Mask, _Aberration, _Vignette;
            float _Noise, _Flicker, _Glitch, _Invert, _Brightness, _Flash;
            float4 _Lens;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            float2 curve(float2 uv)
            {
                uv = uv * 2 - 1;
                float2 off = uv.yx * uv.yx * _Curvature;
                uv += uv * off;
                return uv * 0.5 + 0.5;
            }

            float3 sampleScene(float2 uv)
            {
                float2 dir = uv - 0.5;
                float r = tex2D(_MainTex, uv + dir * _Aberration * 2).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - dir * _Aberration * 2).b;
                return float3(r, g, b);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = curve(i.uv);
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return fixed4(0, 0, 0, 1);

                float band = floor(uv.y * 24 + floor(t * 13));
                float tear = step(1 - _Glitch * 0.35, hash(float2(band, floor(t * 17))));
                uv.x += tear * (hash(float2(band, t)) - 0.5) * 0.15 * _Glitch;
                uv.y = frac(uv.y + _Glitch * 0.02 * sin(t * 3));

                if (_Lens.w > 0)
                {
                    float2 aspect = float2(16.0 / 9.0, 1);
                    float2 d = (uv - _Lens.xy) * aspect;
                    float dist = length(d);
                    float outside = smoothstep(_Lens.z, _Lens.z * 1.3, dist);
                    float pull = _Lens.w * _Lens.z * _Lens.z / max(dist * dist, 1e-4) * outside;
                    uv -= normalize(d + 1e-5) / aspect * min(pull * 0.05, dist);
                    float ang = _Lens.w * 0.6 * saturate(1 - dist / (_Lens.z * 4)) * outside;
                    float2 c = uv - _Lens.xy;
                    float s = sin(ang), co = cos(ang);
                    uv = _Lens.xy + float2(c.x * co - c.y * s, c.x * s + c.y * co);
                }

                float3 col = sampleScene(uv);

                if (_Lens.w > 0)
                {
                    float2 d = (i.uv - _Lens.xy) * float2(16.0 / 9.0, 1);
                    float dist = length(d);
                    col += float3(1, 0.6, 0.25) * min(_Lens.w, 1.5) * 0.5 * exp(-abs(dist - _Lens.z * 1.08) * 80);
                }

                float scan = sin(uv.y * _ScanlineCount * 3.14159 * 2) * 0.5 + 0.5;
                col *= lerp(1, scan * 0.6 + 0.4, _Scanlines);
                float px = fmod(floor(i.uv.x * _ScreenParams.x), 3);
                float3 mask = px < 1 ? float3(1, 0.7, 0.7) : (px < 2 ? float3(0.7, 1, 0.7) : float3(0.7, 0.7, 1));
                col *= lerp(1, mask, _Mask);

                col += (hash(i.uv * _ScreenParams.xy + t) - 0.5) * _Noise;
                col *= 1 - _Flicker * (sin(t * 60) * 0.5 + 0.5);
                float2 v = uv * (1 - uv.yx);
                col *= pow(saturate(v.x * v.y * 15), 0.25 * _Vignette);
                col *= _Brightness;
                col += _Flash * (0.6 - 0.3 * length(i.uv - 0.5));

                col = lerp(col, 1 - col, _Invert);
                return fixed4(saturate(col), 1);
            }
            ENDCG
        }
    }
}
