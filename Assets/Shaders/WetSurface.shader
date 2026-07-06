// Wet/reflective surface for rain-soaked ground and metal. Keeps whatever _MainTex/_Color the
// original material had (WetSurfaceApplier.cs copies them over) and adds high smoothness/
// metallic for real reflection-probe reflections, plus a cheap animated ripple (panning sine
// waves perturbing the normal — not a real puddle normal map, but reads as shimmer at zero
// texture cost).
Shader "Custom/WetSurface"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Wetness ("Wetness", Range(0,1)) = 0.8
        _RippleSpeed ("Ripple Speed", Float) = 0.5
        _RippleScale ("Ripple Scale", Float) = 20
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Wetness;
        float _RippleSpeed;
        float _RippleScale;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            float2 rippleUV = IN.worldPos.xz * 0.1 * _RippleScale;
            float rippleX = sin(rippleUV.x + _Time.y * _RippleSpeed);
            float rippleY = cos(rippleUV.y - _Time.y * _RippleSpeed * 0.7);
            float3 n = normalize(float3(rippleX * 0.12, rippleY * 0.12, 1));

            o.Albedo = c.rgb;
            o.Normal = n;
            o.Smoothness = lerp(0.15, 0.95, _Wetness);
            o.Metallic = lerp(0.0, 0.35, _Wetness);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
