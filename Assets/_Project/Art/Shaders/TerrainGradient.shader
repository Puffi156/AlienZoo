Shader "AlienZoo/TerrainGradient"
{
    // Height-tinted, toon-banded terrain: shadowed purple valleys -> lit mauve ridges,
    // with the same flat cel shading as the props so the whole map reads as one style.
    Properties
    {
        _LowColor      ("Valley Color",     Color) = (0.30, 0.22, 0.40, 1)
        _HighColor     ("Ridge Color",      Color) = (0.62, 0.46, 0.66, 1)
        _MinH          ("Min Height",       Float) = -4
        _MaxH          ("Max Height",       Float) =  4
        _ShadowTint    ("Shadow Tint",      Color) = (0.60, 0.55, 0.66, 1)
        _RampThreshold ("Ramp Threshold",   Range(0,1)) = 0.5
        _RampSmooth    ("Ramp Smoothness",  Range(0.001,0.5)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        half4 _LowColor, _HighColor, _ShadowTint;
        float _MinH, _MaxH, _RampThreshold, _RampSmooth;

        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir) * 0.5 + 0.5;
            half lit = smoothstep(_RampThreshold - _RampSmooth,
                                  _RampThreshold + _RampSmooth, ndl);
            half3 shaded = lerp(s.Albedo * _ShadowTint.rgb, s.Albedo, lit);

            half4 c;
            c.rgb = shaded * _LightColor0.rgb * atten;
            c.a   = s.Alpha;
            return c;
        }

        struct Input { float3 worldPos; };

        void surf (Input IN, inout SurfaceOutput o)
        {
            float t = saturate((IN.worldPos.y - _MinH) / max(0.0001, (_MaxH - _MinH)));
            o.Albedo = lerp(_LowColor, _HighColor, t).rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
