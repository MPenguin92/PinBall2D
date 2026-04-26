Shader "My/DashedLine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Dash Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollSpeed ("Scroll Speed", Float) = 1.2
        _DashLength ("Dash Length", Range(0.01, 5)) = 0.55
        _GapLength ("Gap Length", Range(0.01, 5)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _ScrollSpeed;
            float _DashLength;
            float _GapLength;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float x = uv.x - _Time.y * _ScrollSpeed;
                float dashLength = max(_DashLength, 0.0001);
                float gapLength = max(_GapLength, 0.0001);
                float period = dashLength + gapLength;
                float segment = fmod(x, period);
                if (segment < 0)
                    segment += period;

                float dashMask = 1.0 - step(dashLength, segment);
                fixed4 color = tex2D(_MainTex, float2(0.25, uv.y)) * i.color;
                color.a *= dashMask;
                return color;
            }
            ENDCG
        }
    }
}
