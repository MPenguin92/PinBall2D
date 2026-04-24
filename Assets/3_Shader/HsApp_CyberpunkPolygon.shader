// 黑底贴图赛博朋克呼吸 Shader
// 核心逻辑：亮度转 Alpha（黑→透明，白→不透明）
// 扩展效果：对 alpha<1 的羽化区域做颜色/亮度呼吸，核心白边保持不变，呈现赛博朋克霓虹辉光感
Shader "HsApp/CyberpunkPolygon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Core Protect)]
        _CoreThreshold ("Core Threshold (Alpha Cut)", Range(0.3, 1.0)) = 0.85
        _CoreSoftness ("Core Softness", Range(0.01, 0.5)) = 0.1

        [Header(Halo Neon Colors)]
        [HDR] _NeonColorA ("Halo Color A", Color) = (1.0, 0.1, 0.6, 1)
        [HDR] _NeonColorB ("Halo Color B", Color) = (0.1, 0.9, 1.0, 1)

        [Header(Breathing)]
        _BreathSpeed ("Brightness Breath Speed", Range(0, 10)) = 2.0
        _BreathAmount ("Brightness Breath Amount", Range(0, 1)) = 0.4
        _ColorShiftSpeed ("Color Shift Speed", Range(0, 10)) = 0.8
        _HaloBoost ("Halo Overall Boost", Range(0.5, 3)) = 1.2

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _CoreThreshold;
            float _CoreSoftness;

            fixed4 _NeonColorA;
            fixed4 _NeonColorB;

            float _BreathSpeed;
            float _BreathAmount;
            float _ColorShiftSpeed;
            float _HaloBoost;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 亮度转 Alpha：RGB 最大分量作为蒙版
                float alpha = max(col.r, max(col.g, col.b));

                // 区分"核心白边"和"羽化辉光区"：
                // core=1 表示原始白（保持不变），core=0 表示完全属于 halo（吃呼吸与染色）
                float core = smoothstep(
                    _CoreThreshold - _CoreSoftness,
                    _CoreThreshold + _CoreSoftness,
                    alpha
                );

                // 时间驱动的双路波形（0..1）
                float t = _Time.y;
                float breathWave = sin(t * _BreathSpeed) * 0.5 + 0.5;
                float colorWave = sin(t * _ColorShiftSpeed) * 0.5 + 0.5;

                // 亮度呼吸：1 - amount 到 1 + amount
                float brightness = lerp(1.0 - _BreathAmount, 1.0 + _BreathAmount, breathWave);

                // 颜色在 A/B 之间循环插值
                fixed3 neon = lerp(_NeonColorA.rgb, _NeonColorB.rgb, colorWave);

                // 合成：halo 区用霓虹色 + 呼吸亮度，core 区保留原 RGB
                fixed3 haloRgb = neon * brightness * _HaloBoost;
                fixed3 rgb = lerp(haloRgb, col.rgb, core);

                // halo 区的 alpha 也做轻微呼吸，core 区保持稳定
                float alphaOut = alpha * lerp(brightness, 1.0, core);

                fixed4 result = fixed4(rgb, alphaOut) * i.color;
                // 预乘 Alpha（与 Sprites/Default 渲染管线一致）
                result.rgb *= result.a;
                return result;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
