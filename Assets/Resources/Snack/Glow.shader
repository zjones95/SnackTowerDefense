// Additive billboard flare used for the tower tier markers.
//
// It lives in Resources/ so the build always includes it. (The Standard
// shader's transparent variant can be stripped from a player build, which is
// why the earlier alpha-blended glow turned into a solid shell in-game.)
// Additive blending is what makes this read as light rather than a solid shape.
Shader "Snack/Glow"
{
    Properties
    {
        _MainTex ("Glow", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha One   // additive
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                c.rgb *= _Color.rgb;
                c.a *= _Color.a;
                return c;
            }
            ENDCG
        }
    }

    Fallback Off
}
