Shader "My/UnitHpFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorFull ("Full HP Color", Color) = (1, 0.75, 0.45, 1)
        _ColorEmpty ("Empty HP Color", Color) = (0.55, 0.35, 0.2, 1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
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
                float4 color : COLOR;
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
            fixed4 _ColorFull;
            fixed4 _ColorEmpty;
            float _FillAmount;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.texcoord);
                // uv.y: 0=底 1=顶；扣血时亮色从上往下消失，剩余血量留在下方
                float fill = saturate(_FillAmount);
                float edge = max(fwidth(i.texcoord.y) * 1.5, 0.001);
                float mask = 1.0 - smoothstep(fill - edge, fill + edge, i.texcoord.y);

                fixed4 fillColor = lerp(_ColorEmpty, _ColorFull, mask);
                fixed4 col = fillColor * i.color;
                col.a *= tex.a;
                col.rgb *= tex.rgb;
                return col;
            }
            ENDCG
        }
    }
}
