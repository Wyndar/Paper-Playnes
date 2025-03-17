Shader "Custom/MagicBarrier"
{
    Properties
    {
        _Color ("Base Color", Color) = (0, 1, 1, 0.5)
        _Glow ("Glow Intensity", Range(0, 5)) = 2
        _Transparency ("Transparency", Range(0, 1)) = 0.6
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3
        _Distortion ("Distortion Strength", Range(0, 1)) = 0.2
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseSpeed ("Noise Scroll Speed", Range(0, 5)) = 1
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Lighting Off

        Pass
        {
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
                float3 worldNormal : NORMAL;
                float3 viewDir : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            fixed4 _Color;
            float _Glow;
            float _Transparency;
            float _FresnelPower;
            float _Distortion;
            sampler2D _NoiseTex;
            float _NoiseSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                o.uv = v.uv + _Time.y * _NoiseSpeed;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fresnel glow effect
                float fresnel = pow(1.0 - saturate(dot(i.worldNormal, i.viewDir)), _FresnelPower);

                // Scrolling noise effect
                float noise = tex2D(_NoiseTex, i.uv).r;
                float distortion = lerp(1.0, noise, _Distortion);

                // Final transparency calculation
                float alpha = lerp(_Transparency, 1, fresnel * distortion);
                fixed4 finalColor = fixed4(_Color.rgb * (_Glow * fresnel), alpha);

                return finalColor;
            }
            ENDCG
        }
    }
}
