Shader "AlienZoo/Toon"
{
    // Cel-shaded look for the alien farm: flat banded lighting + a bold ink outline + emission.
    // Outline is an inverted-hull pass (great on the greybox's domes/cubes/cylinders).
    Properties
    {
        _Color         ("Color",            Color) = (1,1,1,1)
        _EmissionColor ("Emission",         Color) = (0,0,0,1)
        _ShadowTint    ("Shadow Tint",      Color) = (0.55,0.50,0.62,1)
        _RampThreshold ("Ramp Threshold",   Range(0,1)) = 0.5
        _RampSmooth    ("Ramp Smoothness",  Range(0.001,0.5)) = 0.06
        _OutlineColor  ("Outline Color",    Color) = (0.07,0.05,0.10,1)
        _OutlineWidth  ("Outline Width",    Range(0,0.06)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // ---- Inverted-hull outline ----
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float  _OutlineWidth;
            half4  _OutlineColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f     { float4 pos    : SV_POSITION; };

            v2f vert (appdata v)
            {
                v.vertex.xyz += normalize(v.normal) * _OutlineWidth;
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            half4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        // ---- Toon lit surface ----
        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        half4  _Color, _EmissionColor, _ShadowTint;
        float  _RampThreshold, _RampSmooth;

        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir) * 0.5 + 0.5;         // 0..1
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
            o.Albedo   = _Color.rgb;
            o.Emission = _EmissionColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
