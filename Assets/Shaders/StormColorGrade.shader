// Fullscreen post-process for the storm look: desaturation, contrast, cold/green tint,
// vignette, film grain, and a cheap depth-based defocus blur. Built-in Render Pipeline
// (this project has no URP package installed), driven via Camera.OnRenderImage in
// StormPostProcess.cs — not a URP Volume, since URP isn't present.
Shader "Hidden/StormColorGrade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _CameraDepthTexture;

            float _Saturation;
            float _Contrast;
            fixed4 _ColorTint;
            float _VignetteIntensity;
            float _VignetteSmoothness;
            float _GrainIntensity;
            float _Time01;

            float _FocusDistance;
            float _FocusRange;
            float _BlurStrength;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // --- Cheap depth-based defocus blur (subtle, not true bokeh) ---
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float eyeDepth = LinearEyeDepth(rawDepth);
                float defocus = saturate((abs(eyeDepth - _FocusDistance) - _FocusRange) / max(_FocusRange, 0.001));
                float blurAmount = defocus * _BlurStrength;

                fixed4 col;
                if (blurAmount > 0.001)
                {
                    float2 texel = _MainTex_TexelSize.xy * (1.0 + blurAmount * 4.0);
                    fixed4 sum = tex2D(_MainTex, i.uv) * 0.28;
                    sum += tex2D(_MainTex, i.uv + float2( texel.x,  0)) * 0.12;
                    sum += tex2D(_MainTex, i.uv + float2(-texel.x,  0)) * 0.12;
                    sum += tex2D(_MainTex, i.uv + float2( 0,  texel.y)) * 0.12;
                    sum += tex2D(_MainTex, i.uv + float2( 0, -texel.y)) * 0.12;
                    sum += tex2D(_MainTex, i.uv + float2( texel.x,  texel.y)) * 0.06;
                    sum += tex2D(_MainTex, i.uv + float2(-texel.x,  texel.y)) * 0.06;
                    sum += tex2D(_MainTex, i.uv + float2( texel.x, -texel.y)) * 0.06;
                    sum += tex2D(_MainTex, i.uv + float2(-texel.x, -texel.y)) * 0.06;
                    col = lerp(tex2D(_MainTex, i.uv), sum, saturate(blurAmount));
                }
                else
                {
                    col = tex2D(_MainTex, i.uv);
                }

                // --- Desaturate ---
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, luminance.xxx, _Saturation);

                // --- Contrast (crush blacks around mid-grey pivot) ---
                col.rgb = saturate((col.rgb - 0.5) * _Contrast + 0.5);

                // --- Cold/green storm tint ---
                col.rgb *= _ColorTint.rgb;

                // --- Vignette ---
                float2 center = i.uv - 0.5;
                float vig = 1 - dot(center, center) * _VignetteIntensity;
                vig = smoothstep(0, _VignetteSmoothness, vig);
                col.rgb *= vig;

                // --- Film grain ---
                float grain = hash(i.uv * _MainTex_TexelSize.zw + _Time01 * 137.0) - 0.5;
                col.rgb += grain * _GrainIntensity;

                return col;
            }
            ENDCG
        }
    }
}
