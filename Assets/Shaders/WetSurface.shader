// Cel-shaded wet/reflective surface for ground, walls, and metal — matches the character's
// stylized look (quantized toon lighting) instead of the previous photorealistic PBR (Standard)
// shading. Keeps whatever _MainTex/_Color the original material had (WetSurfaceApplier.cs copies
// them over) and adds a tight toon specular band + reflection-probe-driven "wet" reflection +
// a cheap animated ripple (panning sine waves perturbing the normal — not a real puddle normal
// map, but reads as shimmer at zero texture cost).
Shader "Custom/WetSurface"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowTint ("Shadow Tint", Color) = (0.5, 0.54, 0.62, 1)
        _RampSoftness ("Band Softness", Range(0.001, 0.5)) = 0.08
        _Wetness ("Wetness", Range(0,1)) = 0.8
        _RippleSpeed ("Ripple Speed", Float) = 0.5
        _RippleScale ("Ripple Scale", Float) = 20
        _SpecSize ("Spec Highlight Size", Range(0.8, 0.999)) = 0.96
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _ShadowTint;
        float _RampSoftness;
        half _Wetness;
        float _RippleSpeed;
        float _RippleScale;
        half _SpecSize;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
            INTERNAL_DATA
        };

        // Quantized (soft-edged) diffuse band, like the rest of the environment/character, plus a
        // tight toon specular blob whose strength scales with wetness (dry = almost no highlight,
        // wet = a hard bright glint, like light catching a puddle).
        half4 LightingToon(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            half band = smoothstep(0.0, _RampSoftness, NdotL);
            half3 diffuse = lerp(_ShadowTint.rgb, half3(1, 1, 1), band) * s.Albedo;

            half3 halfDir = normalize(lightDir + viewDir);
            half NdotH = dot(s.Normal, halfDir);
            half spec = smoothstep(_SpecSize, _SpecSize + 0.02, NdotH) * lerp(0.05, 1.0, _Wetness);

            half3 col = diffuse * _LightColor0.rgb * atten + spec * _LightColor0.rgb * atten;

            half4 c;
            c.rgb = col;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            float2 rippleUV = IN.worldPos.xz * 0.1 * _RippleScale;
            float rippleX = sin(rippleUV.x + _Time.y * _RippleSpeed);
            float rippleY = cos(rippleUV.y - _Time.y * _RippleSpeed * 0.7);
            float3 n = normalize(float3(rippleX * 0.12 * _Wetness, rippleY * 0.12 * _Wetness, 1));

            o.Albedo = c.rgb;
            o.Normal = n;
            o.Alpha = c.a;

            // Fake puddle reflection via the nearest reflection probe/skybox, blended by wetness —
            // SurfaceOutput (needed for the custom Toon lighting model above) has no Metallic/
            // Smoothness like SurfaceOutputStandard does, so this goes through Emission instead.
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            float3 worldRefl = reflect(-normalize(IN.viewDir), worldNormal);
            half3 reflection = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldRefl).rgb;
            o.Emission = reflection * _Wetness * 0.3;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
