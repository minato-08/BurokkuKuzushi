using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// メニュー背景の「磨りガラス」演出の仕組み（止め画キャプチャ → ブラー → 暗転）。
// ゲームはメニュー中 Time.timeScale=0 で止まっているので、開いた瞬間に 1 回生成すれば足りる（激軽）。
//
// セットアップ:
//   1) いずれかの常駐 GameObject（例: _UI/_CameraSpace/_Panels）に本コンポーネントを 1 個付ける。
//   2) パネル群の最背面に全画面 RawImage を 1 枚置き、backdropImage にバインド。
//   3) KawaseBlur.shader からマテリアルを作り（Create > Material → Shader: Custom/KawaseBlur）、blurMaterial にバインド。
//   4) メニューを開く瞬間に Capture()、閉じる時に Clear() を呼ぶ（各 UI スクリプトから）。
//
// 呼び出し例（TitleUI など）:
//   [SerializeField] BackdropBlur backdrop;
//   ... Open 時:  backdrop?.Capture();
//   ... Close 時: backdrop?.Clear();
//
// 注意: Capture() は「その瞬間に画面に映っているもの」を取り込む。パネルの前景が既に表示されていると
//       それも写り込むので、Capture() は前景を出す前（メニュー遷移の瞬間）に呼ぶこと。backdropImage は
//       キャプチャ中だけ自動で隠すので自分自身は写り込まない。
// 何もバインドされていなくても安全（警告を 1 回だけ出して何もしない）。
[DisallowMultipleComponent]
public class BackdropBlur : MonoBehaviour
{
    [Header("バインド")]
    [SerializeField] private RawImage backdropImage;   // パネル最背面の全画面 RawImage
    [SerializeField] private Material blurMaterial;     // Custom/KawaseBlur から作ったマテリアル

    [Header("品質 / 見た目")]
    [SerializeField, Range(0, 4)] private int   downsample = 2;     // 解像度を 1/2^n に（大きいほど軽く・ぼける）
    [SerializeField, Range(0, 10)] private int  iterations = 5;     // ブラー反復回数（多いほど強い）
    [SerializeField, Range(0f, 1f)] private float darken   = 0.5f;  // 暗転度（0=そのまま, 1=真っ黒）

    private RenderTexture current;
    private bool warned;

    public bool IsShown => backdropImage != null && backdropImage.enabled;

    // メニューを開く瞬間に呼ぶ。現在の画面を取り込み、ぼかし・暗転して backdropImage に表示する。
    public void Capture()
    {
        if (backdropImage == null || blurMaterial == null) { WarnOnce(); return; }
        if (!isActiveAndEnabled) return;
        StartCoroutine(CaptureRoutine());
    }

    // メニューを閉じる時に呼ぶ。背景を隠して RT を解放する。
    public void Clear()
    {
        if (backdropImage != null) backdropImage.enabled = false;
        ReleaseCurrent();
    }

    private IEnumerator CaptureRoutine()
    {
        // 自分自身を写さないようキャプチャ中は隠す
        backdropImage.enabled = false;

        // 描画完了後でないと画面を取得できない（timeScale=0 でもフレームは進むので発火する）
        yield return new WaitForEndOfFrame();

        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

        int w = Mathf.Max(1, shot.width  >> downsample);
        int h = Mathf.Max(1, shot.height >> downsample);

        var a = RenderTexture.GetTemporary(w, h, 0);
        var b = RenderTexture.GetTemporary(w, h, 0);

        Graphics.Blit(shot, a);                 // 縮小しつつ取り込み
        RenderTexture from = a, to = b;
        for (int i = 0; i < iterations; i++)    // Kawase ブラー ping-pong
        {
            blurMaterial.SetFloat("_Offset", i);
            Graphics.Blit(from, to, blurMaterial);
            (from, to) = (to, from);
        }

        // 表示用 RT に確定（メニュー表示中ずっと保持するので GetTemporary ではなく専用に確保）
        ReleaseCurrent();
        current = new RenderTexture(w, h, 0);
        current.Create();
        Graphics.Blit(from, current);

        RenderTexture.ReleaseTemporary(a);
        RenderTexture.ReleaseTemporary(b);
        Destroy(shot);

        backdropImage.texture = current;
        float v = 1f - Mathf.Clamp01(darken); // 頂点カラー乗算で暗転
        backdropImage.color = new Color(v, v, v, 1f);
        backdropImage.enabled = true;
    }

    private void ReleaseCurrent()
    {
        if (current != null) { current.Release(); Destroy(current); current = null; }
        if (backdropImage != null) backdropImage.texture = null;
    }

    private void OnDisable() => ReleaseCurrent();

    private void WarnOnce()
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning("[BackdropBlur] backdropImage / blurMaterial が未設定のため何もしません。");
    }
}
