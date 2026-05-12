Shader "Custom/SpriteCooldownSweep"
{
    Properties
    {
        [PerRendererData] [HideInInspector] _MainTex("Sprite Texture", 2D) = "white" {}
        _Progress("Progress", Range(0, 1)) = 0
        _OverlayColor("Overlay Color", Color) = (0, 0, 0, 0.65)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _OverlayColor;
            float _Progress;
        CBUFFER_END

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.color = input.color;
            return output;
        }

        float4 frag(Varyings input) : SV_Target
        {
            float4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            float spriteAlpha = sprite.a;

            if (spriteAlpha <= 0.0)
                discard;

            // Clockwise angle from top (12 o'clock), normalized to [0, 1]
            float2 centered = input.uv - 0.5;
            float angle = fmod(atan2(centered.x, centered.y) / TWO_PI + 1.0, 1.0);

            if (angle > _Progress)
                discard;

            float3 overlayRgb = _OverlayColor.rgb * sprite.rgb;
            return float4(overlayRgb, _OverlayColor.a * spriteAlpha * input.color.a);
        }
        ENDHLSL

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}