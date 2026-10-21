Shader "Castle/UI/WaveScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Wave", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        // 单位为每秒循环次数；正值使波形向左移动。
        _ScrollSpeed ("Leftward Cycles Per Second", Float) = 0.03
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float4 localPosition:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _ScrollSpeed;
            Output vert(Input v)
            {
                Output o;o.localPosition=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;
            }
            fixed4 frag(Output i):SV_Target
            {
                // 只循环水平采样，图片矩形、透明背景和独立白点保持原样。
                float2 uv=float2(frac(i.uv.x+frac(_Time.y*_ScrollSpeed)),i.uv.y);
                fixed4 color=(tex2D(_MainTex,uv)+_TextureSampleAdd)*i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.localPosition.xy,_ClipRect);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
