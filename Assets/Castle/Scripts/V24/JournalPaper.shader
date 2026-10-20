Shader "Castle/UI/JournalPaper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Book", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            sampler2D _MainTex;
            fixed4 _Color;
            Output vert(Input v)
            {
                Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;
            }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 original=tex2D(_MainTex,i.uv);
                // 仅道具/记录页启用；从同一素材左页采样纸纹，羽化覆盖合成在底图中的便笺。
                float mask=smoothstep(.526,.546,i.uv.x)*(1-smoothstep(.874,.888,i.uv.x));
                mask*=smoothstep(.145,.165,i.uv.y)*(1-smoothstep(.623,.643,i.uv.y));
                fixed4 paper=tex2D(_MainTex,float2(i.uv.x-.435,i.uv.y));
                return lerp(original,paper,mask)*i.color;
            }
            ENDCG
        }
    }
}
