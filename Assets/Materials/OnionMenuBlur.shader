Shader "UI/OnionMenuBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Strength", Range(0, 4)) = 2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _BlurSize;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 result = tex2D(_MainTex, input.uv) * 4;
                result += tex2D(_MainTex, input.uv + float2(offset.x, 0));
                result += tex2D(_MainTex, input.uv - float2(offset.x, 0));
                result += tex2D(_MainTex, input.uv + float2(0, offset.y));
                result += tex2D(_MainTex, input.uv - float2(0, offset.y));
                result += tex2D(_MainTex, input.uv + offset);
                result += tex2D(_MainTex, input.uv - offset);
                result += tex2D(_MainTex, input.uv + float2(offset.x, -offset.y));
                result += tex2D(_MainTex, input.uv + float2(-offset.x, offset.y));
                return (result / 12) * input.color;
            }
            ENDCG
        }
    }
}
