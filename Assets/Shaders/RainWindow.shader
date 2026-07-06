// Rain-streaked window: keeps the original _MainTex/_Color (RainWindow_Applier copies them
// over) and overlays procedural scrolling rivulets + static droplet highlights via Emission —
// no textures needed, just noise/scroll math, so it works on any window material regardless of
// its original texture.
Shader "Custom/RainWindow"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _StreakSpeed ("Streak Speed", Float) = 0.4
        _StreakScale ("Streak Scale", Float) = 15
        _DropletIntensity ("Droplet Intensity", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _StreakSpeed;
        float _StreakScale;
        half _DropletIntensity;

        struct Input
        {
            float2 uv_MainTex;
        };

        float hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
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
            o.Smoothness = 0.9;
            o.Metallic = 0.1;
            o.Emission = (streakMask + dropletMask) * float3(0.6, 0.65, 0.7);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
