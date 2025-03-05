Shader "Custom/ShockwaveEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionStrength ("Distortion Strength", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float _DistortionStrength;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 distortion = float2(sin(i.uv.y * 10), cos(i.uv.x * 10)) * _DistortionStrength;
                fixed4 col = tex2D(_MainTex, i.uv + distortion);
                return col;
            }
            ENDCG
        }
    }
}
