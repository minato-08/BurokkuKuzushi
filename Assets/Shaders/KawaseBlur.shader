// Kawase ブラー。Graphics.Blit(src, dst, mat) で複数回呼び、_Offset を 0,1,2... と増やすと
// 軽量できれいなブラーになる。BackdropBlur.cs から使う想定（メニュー背景の磨りガラス用）。
// Graphics.Blit 用の素朴なイメージエフェクトシェーダ（URP でも手動 Blit なら問題なく動く）。
Shader "Hidden/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset  ("Offset", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize; // Blit が自動設定 (x,y = 1/幅, 1/高さ)
            float     _Offset;

            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 o = _MainTex_TexelSize.xy * (_Offset + 0.5);
                fixed4 c = 0;
                c += tex2D(_MainTex, i.uv + float2( o.x,  o.y));
                c += tex2D(_MainTex, i.uv + float2(-o.x,  o.y));
                c += tex2D(_MainTex, i.uv + float2( o.x, -o.y));
                c += tex2D(_MainTex, i.uv + float2(-o.x, -o.y));
                return c * 0.25;
            }
            ENDCG
        }
    }
}
