Shader "Castle/UI/MapPaper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Map", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        // 白底地图与纸张相乘，白色区域保留纸张底色。
        Blend DstColor Zero
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            sampler2D _MainTex;
            float4 _Color;
            Output vert(Input v)
            {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;
            }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                return fixed4(lerp(fixed3(1,1,1),c.rgb,c.a),1);
            }
            ENDCG
        }
    }
}
