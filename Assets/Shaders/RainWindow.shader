// Rain-streaked window, cel-shaded to match the rest of the environment (same LightingToon model
// as WetSurface.shader) instead of photorealistic PBR. Keeps the original _MainTex/_Color
// (WetSurfaceApplier.cs copies them over) and overlays procedural scrolling rivulets + static
// droplet highlights via Emission — no textures needed, just noise/scroll math, so it works on
// any window material regardless of its original texture.
Shader "Custom/RainWindow"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowTint ("Shadow Tint", Color) = (0.5, 0.54, 0.62, 1)
        _RampSoftness ("Band Softness", Range(0.001, 0.5)) = 0.08
        _SpecSize ("Spec Highlight Size", Range(0.8, 0.999)) = 0.94
        _StreakSpeed ("Streak Speed", Float) = 0.4
        _StreakScale ("Streak Scale", Float) = 15
        _DropletIntensity ("Droplet Intensity", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        CGPROGRAM
        #pragma surface surf Toon alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _ShadowTint;
        float _RampSoftness;
        half _SpecSize;
        float _StreakSpeed;
        float _StreakScale;
        half _DropletIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        half4 LightingToon(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            half band = smoothstep(0.0, _RampSoftness, NdotL);
            half3 diffuse = lerp(_ShadowTint.rgb, half3(1, 1, 1), band) * s.Albedo;

            half3 halfDir = normalize(lightDir + viewDir);
            half NdotH = dot(s.Normal, halfDir);
            half spec = smoothstep(_SpecSize, _SpecSize + 0.02, NdotH);

            // Note: don't add s.Emission here — Unity's surface shader wrapper adds it
            // automatically after this function returns; doing it here too would double it.
            half4 c;
            c.rgb = diffuse * _LightColor0.rgb * atten + spec * _LightColor0.rgb * atten;
            c.a = s.Alpha;
            return c;
        }

        float hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Scrolling vertical rivulets, offset per-column so it doesn't look like one uniform scroll.
            float2 streakUV = IN.uv_MainTex * _StreakScale;
            float colOffset = hash21(floor(float2(streakUV.x, 0))) * 10.0;
            float streak = frac(streakUV.y - _Time.y * _StreakSpeed - colOffset);
            float streakMask = smoothstep(0.95, 1.0, streak) * 0.4;

            // Static per-cell droplet highlights.
            float2 cell = floor(streakUV);
            float droplet = hash21(cell);
            float dropletMask = smoothstep(0.9, 1.0, droplet) * _DropletIntensity;

            o.Albedo = c.rgb;
            o.Emission = (streakMask + dropletMask) * half3(0.6, 0.65, 0.7);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
