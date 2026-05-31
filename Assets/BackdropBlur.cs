using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// メニュー背景の「磨りガラス」演出（止め画キャプチャ → ブラー → 暗転 → フェード）。
// ゲームはメニュー中 Time.timeScale=0 で止まっているので、メニューに入った時に 1 回だけ撮れば足りる。
//
// 自動ドライブ(autoDriveByGameState):
//   非メニュー(Playing/Countdown) → メニュー(Title/Settings/SkillSelect/RoundOver/MatchOver) に入った時だけ Capture し、
//   フェードインで表示。メニュー間の遷移では撮り直さない(暗さの色だけ更新)＝素のゲーム画面が挟まらない。
//   メニュー → 非メニュー でフェードアウトして消す。
//
// セットアップ: 常駐 GameObject(例 _Panels)に付け、パネル最背面の全画面 RawImage を backdropImage に、
//   Custom/KawaseBlur マテリアルを blurMaterial にバインド。未バインドでも安全。
[DisallowMultipleComponent]
public class BackdropBlur : MonoBehaviour
{
    [Header("バインド")]
    [SerializeField] private RawImage backdropImage;
    [SerializeField] private Material blurMaterial;

    [Header("品質 / 見た目")]
    [SerializeField, Range(0, 4)] private int   downsample = 2;
    [SerializeField, Range(0, 10)] private int  iterations = 5;
    [SerializeField, Range(0f, 1f)] private float darken   = 0.35f;

    [Header("状態別の暗さ（0=そのまま 1=真っ黒）")]
    [SerializeField, Range(0f, 1f)] private float darkenTitle       = 0.30f;
    [SerializeField, Range(0f, 1f)] private float darkenSettings    = 0.40f;
    [SerializeField, Range(0f, 1f)] private float darkenSkillSelect = 0.40f;
    [SerializeField, Range(0f, 1f)] private float darkenRoundOver   = 0.25f;
    [SerializeField, Range(0f, 1f)] private float darkenMatchOver   = 0.55f;

    [Header("フェード（秒・unscaled）")]
    [SerializeField] private float fadeInSeconds  = 0.22f;
    [SerializeField] private float fadeOutSeconds = 0.18f;

    [Header("自動ドライブ")]
    [SerializeField] private bool autoDriveByGameState = true;

    private RenderTexture current;
    private bool warned;
    private GameManager.GameState? prevState;
    private bool  menuActive;     // 背景を出すべき状態か
    private float alpha;          // 現在のフェード値
    private float targetAlpha;    // 目標フェード値

    private void Update()
    {
        if (autoDriveByGameState && GameManager.Instance != null)
        {
            var s = GameManager.Instance.GetCurrentState();
            if (!prevState.HasValue || prevState.Value != s)
            {
                prevState = s;
                bool isMenu = IsMenu(s);
                darken = DarkenFor(s); // 暗さは毎状態で更新（メニュー間でも色は変わる）

                if (isMenu && !menuActive)       { menuActive = true;  targetAlpha = 1f; Capture(); } // 入場：撮影＋フェードイン
                else if (!isMenu && menuActive)  { menuActive = false; targetAlpha = 0f; }            // 退場：フェードアウト
                // メニュー間(isMenu && menuActive)は撮り直さない（暗さ色のみ上で更新済み）
            }
        }

        ApplyFade();
    }

    private void ApplyFade()
    {
        if (backdropImage == null) return;

        float speed = targetAlpha > alpha
            ? (fadeInSeconds  > 0f ? Time.unscaledDeltaTime / fadeInSeconds  : 1f)
            : (fadeOutSeconds > 0f ? Time.unscaledDeltaTime / fadeOutSeconds : 1f);
        alpha = Mathf.MoveTowards(alpha, targetAlpha, speed);

        if (alpha <= 0.001f && targetAlpha <= 0f)
        {
            if (backdropImage.enabled) backdropImage.enabled = false;
            ReleaseCurrent();
            return;
        }

        if (!backdropImage.enabled) backdropImage.enabled = true;
        float v = 1f - Mathf.Clamp01(darken);
        backdropImage.color = new Color(v, v, v, alpha);
    }

    private static bool IsMenu(GameManager.GameState s) =>
        s == GameManager.GameState.Title
     || s == GameManager.GameState.Settings
     || s == GameManager.GameState.SkillSelect
     || s == GameManager.GameState.RoundOver
     || s == GameManager.GameState.MatchOver;

    private float DarkenFor(GameManager.GameState s) => s switch
    {
        GameManager.GameState.Title       => darkenTitle,
        GameManager.GameState.Settings    => darkenSettings,
        GameManager.GameState.SkillSelect => darkenSkillSelect,
        GameManager.GameState.RoundOver   => darkenRoundOver,
        GameManager.GameState.MatchOver   => darkenMatchOver,
        _ => darken,
    };

    public bool IsShown => backdropImage != null && backdropImage.enabled;

    // 現在の画面を取り込み、ぼかして表示する（フェードインは ApplyFade が担当）。
    public void Capture()
    {
        if (backdropImage == null || blurMaterial == null) { WarnOnce(); return; }
        if (!isActiveAndEnabled) return;
        targetAlpha = 1f;
        StartCoroutine(CaptureRoutine());
    }

    // フェードアウトして消す（実際の無効化・解放は ApplyFade が alpha=0 で行う）。
    public void Clear()
    {
        menuActive = false;
        targetAlpha = 0f;
    }

    private IEnumerator CaptureRoutine()
    {
        alpha = 0f;                      // 透明から開始（フェードイン）
        backdropImage.enabled = false;   // 自分を写さない
        yield return new WaitForEndOfFrame();

        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        int w = Mathf.Max(1, shot.width  >> downsample);
        int h = Mathf.Max(1, shot.height >> downsample);

        var a = RenderTexture.GetTemporary(w, h, 0);
        var b = RenderTexture.GetTemporary(w, h, 0);
        Graphics.Blit(shot, a);
        RenderTexture from = a, to = b;
        for (int i = 0; i < iterations; i++)
        {
            blurMaterial.SetFloat("_Offset", i);
            Graphics.Blit(from, to, blurMaterial);
            (from, to) = (to, from);
        }

        ReleaseCurrent();
        current = new RenderTexture(w, h, 0);
        current.Create();
        Graphics.Blit(from, current);

        RenderTexture.ReleaseTemporary(a);
        RenderTexture.ReleaseTemporary(b);
        Destroy(shot);

        backdropImage.texture = current;
        float v = 1f - Mathf.Clamp01(darken);
        backdropImage.color = new Color(v, v, v, alpha);
        backdropImage.enabled = true; // 以降は ApplyFade が alpha を上げる
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
