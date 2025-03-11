Shader "SDCN/Depth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InvertY ("Invert", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D_float _CameraDepthTexture;
            float4 _CameraDepthTexture_TexelSize;
            float _Invert;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv      : TEXCOORD0;
                float4 vertex  : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv).x;
                float linearDepth = rawDepth;
                if (_Invert > 0.5) linearDepth = 1.0 - linearDepth;
                return float4(linearDepth, linearDepth, linearDepth, 1.0);
            }
            ENDCG
        }
    }
}
