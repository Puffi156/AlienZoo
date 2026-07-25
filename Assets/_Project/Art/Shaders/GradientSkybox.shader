Shader "AlienZoo/GradientSkybox"
{
    // Simple vertical gradient skybox: teal zenith -> peach horizon -> purple nadir.
    // Sells the alien-dusk mood without a texture. Add twin-moon / aurora billboards later.
    Properties
    {
        _TopColor     ("Top (zenith)",  Color) = (0.18, 0.80, 0.78, 1)
        _HorizonColor ("Horizon",       Color) = (0.97, 0.82, 0.60, 1)
        _BottomColor  ("Bottom (nadir)",Color) = (0.28, 0.18, 0.34, 1)
        _Exponent     ("Falloff",       Float) = 1.3
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            half4 _TopColor, _HorizonColor, _BottomColor;
            float _Exponent;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float d = normalize(i.dir).y; // -1 (down) .. 1 (up)
                if (d >= 0.0)
                    return lerp(_HorizonColor, _TopColor,    pow(saturate(d),  _Exponent));
                else
                    return lerp(_HorizonColor, _BottomColor, pow(saturate(-d), _Exponent));
            }
            ENDCG
        }
    }
}
