Shader "Custom/WaterShader"
{
    Properties
    {
        _DeepColor ("Deep Water Color", Color) = (0, 0, 0.8, 1)
        _ShallowColor ("Shallow Water Color", Color) = (0, 0.8, 1, 1)
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.5
        _WaveHeight ("Wave Height", Range(0, 1)) = 0.2
        _WaveFrequency ("Wave Frequency", Range(0, 10)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            float _WaveSpeed;
            float _WaveHeight;
            float _WaveFrequency;
            float4 _DeepColor;
            float4 _ShallowColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                float wave = sin(v.vertex.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveHeight;
                v.vertex.y += wave;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = i.worldPos.y;
                float4 color = lerp(_ShallowColor, _DeepColor, depth);
                return color;
            }
            ENDCG
        }
    }
}
