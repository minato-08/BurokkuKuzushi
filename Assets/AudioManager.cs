using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// 中央オーディオハブ（DESIGN.md 10）。シーンに 1 つ置き、各所から AudioManager.Instance?.PlayX() で発火する。
// 設計方針:
//   - AudioClip / AudioMixer はすべて任意（未割り当てでも null セーフに無音動作する）。発表用の音源は後から Inspector で差す。
//   - SE は AudioSource プールでラウンドロビン再生（PlayOneShot は timeScale=0 でも鳴る）。
//   - 音量は PlayerPrefs vol.master/bgm/se (0-100) を dB 変換して Mixer の Exposed Param に流す（Mixer 未割り当てなら何もしない）。
//   - ブロック衝突 SE はアリーナごとに 50ms クールダウン（連打抑制, DESIGN.md 10.3）。
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("ミキサー（任意。未割り当てでも動作）")]
    [SerializeField] private AudioMixer      mixer;
    [SerializeField] private AudioMixerGroup seGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    // Mixer の Exposed Parameter 名（Mixer 側で Expose しておく）
    [SerializeField] private string masterVolParam = "MasterVol";
    [SerializeField] private string bgmVolParam    = "BGMVol";
    [SerializeField] private string seVolParam     = "SEVol";

    [Header("SE プール")]
    [SerializeField] private int sePoolSize = 12;

    [Header("SE クリップ — ボール / ブロック")]
    [SerializeField] private AudioClip seBallWall;
    [SerializeField] private AudioClip seBallPaddle;
    [SerializeField] private AudioClip seBallLaunch;
    [SerializeField] private AudioClip seBlockHitNormal;
    [SerializeField] private AudioClip seBlockHitHard;
    [SerializeField] private AudioClip seBlockHitAbsorb;
    [SerializeField] private AudioClip seBlockBreak;
    [SerializeField] private AudioClip seBlockExplosive;

    [Header("SE クリップ — アイテム / スキル / 妨害")]
    [SerializeField] private AudioClip seItemDrop;
    [SerializeField] private AudioClip seItemBuff;
    [SerializeField] private AudioClip seItemAttack;
    [SerializeField] private AudioClip seItemTrap;
    [SerializeField] private AudioClip seSkillReady;
    [SerializeField] private AudioClip seSkillActivate;
    [SerializeField] private AudioClip seInterferenceRecv;
    [SerializeField] private AudioClip seAddRowLand;
    [SerializeField] private AudioClip sePoisonLoop;

    [Header("SE クリップ — ラウンド / マッチ / UI")]
    [SerializeField] private AudioClip seRoundStart;
    [SerializeField] private AudioClip seRoundWin;
    [SerializeField] private AudioClip seMatchWin;
    [SerializeField] private AudioClip seComboMilestone;
    [SerializeField] private AudioClip seUiMove;
    [SerializeField] private AudioClip seUiConfirm;

    [Header("BGM クリップ")]
    [SerializeField] private AudioClip bgmTitle;
    [SerializeField] private AudioClip bgmMatch;
    [SerializeField] private AudioClip bgmMatchTense;  // HP30% 帯で重ねる緊迫レイヤー
    [SerializeField] private AudioClip bgmResultJingle;

    [Header("SE ピッチ調整")]
    [SerializeField] private float ballWallPitchPerSpeed = 0.2f; // pitch = 1 + (speedRatio-1) * これ
    [SerializeField] private float blockHitSeCooldown    = 0.05f; // アリーナごと 50ms

    // ---- ランタイム状態 ----
    private AudioSource[] sePool;
    private int           seCursor;
    private AudioSource   poisonSource;   // ループ用専用ソース
    private int           poisonRefCount;
    private AudioSource   bgmPrimary;     // 通常 BGM レイヤー
    private AudioSource   bgmTense;       // 緊迫レイヤー
    private float         p1LastBlockSe, p2LastBlockSe;
    private bool          tenseActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // SE プール生成
        sePool = new AudioSource[Mathf.Max(1, sePoolSize)];
        for (int i = 0; i < sePool.Length; i++) sePool[i] = MakeSource("SE_" + i, seGroup, loop: false);

        poisonSource = MakeSource("PoisonLoop", seGroup, loop: true);
        bgmPrimary   = MakeSource("BGM_Primary", bgmGroup, loop: true);
        bgmTense     = MakeSource("BGM_Tense",   bgmGroup, loop: true);
        bgmTense.volume = 0f;

        ApplyVolumes();
    }

    private AudioSource MakeSource(string n, AudioMixerGroup group, bool loop)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake          = false;
        src.loop                 = loop;
        src.spatialBlend         = 0f;          // 2D
        src.outputAudioMixerGroup = group;
        src.ignoreListenerPause  = true;        // Pause を使わないが念のため
        return src;
    }

    // =====================================================
    // 音量（PlayerPrefs 0-100 → dB）
    // =====================================================

    // dB = 20*log10(v/100)。v<=0 は -80dB（実質ミュート）
    private static float ToDb(float value01to100)
    {
        if (value01to100 <= 0.01f) return -80f;
        return 20f * Mathf.Log10(Mathf.Clamp(value01to100, 0f, 100f) / 100f);
    }

    public void ApplyVolumes()
    {
        SetMixerDb(masterVolParam, PlayerPrefs.GetFloat("vol.master", 80f));
        SetMixerDb(bgmVolParam,    PlayerPrefs.GetFloat("vol.bgm",    80f));
        SetMixerDb(seVolParam,     PlayerPrefs.GetFloat("vol.se",     80f));
    }

    private void SetMixerDb(string param, float value)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;
        mixer.SetFloat(param, ToDb(value));
    }

    // =====================================================
    // SE 再生コア
    // =====================================================

    private void PlaySE(AudioClip clip, float pitch = 1f, float volScale = 1f)
    {
        if (clip == null || sePool == null) return;
        var src = sePool[seCursor];
        seCursor = (seCursor + 1) % sePool.Length;
        src.pitch = pitch;
        src.PlayOneShot(clip, volScale);
    }

    private static float Semitone(float n) => Mathf.Pow(2f, n / 12f);

    // =====================================================
    // SE 公開 API（DESIGN.md 10.4 のトリガー表に対応）
    // =====================================================

    public void PlayBallWall(float speedRatio)
        => PlaySE(seBallWall, 1f + (speedRatio - 1f) * ballWallPitchPerSpeed);

    public void PlayBallPaddle() => PlaySE(seBallPaddle);
    public void PlayBallLaunch() => PlaySE(seBallLaunch);

    // ブロック衝突。アリーナごと 50ms クールダウン。Hard は -2 半音。
    public void PlayBlockHit(int blockType, int arenaIndex)
    {
        float now = Time.unscaledTime;
        if (arenaIndex == 1)
        {
            if (now - p1LastBlockSe < blockHitSeCooldown) return;
            p1LastBlockSe = now;
        }
        else
        {
            if (now - p2LastBlockSe < blockHitSeCooldown) return;
            p2LastBlockSe = now;
        }

        switch ((BlockType)blockType)
        {
            case BlockType.Hard:   PlaySE(seBlockHitHard, Semitone(-2f)); break;
            case BlockType.Absorb: PlaySE(seBlockHitAbsorb);              break;
            default:               PlaySE(seBlockHitNormal);              break;
        }
    }

    public void PlayBlockBreak(bool explosive)
        => PlaySE(explosive ? seBlockExplosive : seBlockBreak);

    public void PlayItemDrop() => PlaySE(seItemDrop);

    public void PlayItemPickup(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Attack: PlaySE(seItemAttack); break;
            case ItemCategory.Trap:   PlaySE(seItemTrap);   break;
            default:                  PlaySE(seItemBuff);   break;
        }
    }

    public void PlaySkillReady()     => PlaySE(seSkillReady);
    public void PlaySkillActivate()  => PlaySE(seSkillActivate);
    public void PlayInterferenceRecv() => PlaySE(seInterferenceRecv);
    public void PlayAddRowLand()     => PlaySE(seAddRowLand);
    public void PlayRoundStart()     => PlaySE(seRoundStart);
    public void PlayRoundWin()       => PlaySE(seRoundWin);
    public void PlayMatchWin()       => PlaySE(seMatchWin);
    public void PlayUiMove()         => PlaySE(seUiMove);
    public void PlayUiConfirm()      => PlaySE(seUiConfirm);

    // マイルストーン番号でピッチを +N 半音（10→0, 20→+2, 30→+4 …）
    public void PlayComboMilestone(int milestone)
        => PlaySE(seComboMilestone, Semitone(Mathf.Max(0, (milestone / 10) - 1) * 2f));

    // 毒エリアのループ（参照カウントで複数同時に対応）
    public void StartPoisonLoop()
    {
        poisonRefCount++;
        if (sePoisonLoop != null && poisonSource != null && !poisonSource.isPlaying)
        {
            poisonSource.clip = sePoisonLoop;
            poisonSource.Play();
        }
    }

    public void StopPoisonLoop()
    {
        poisonRefCount = Mathf.Max(0, poisonRefCount - 1);
        if (poisonRefCount == 0 && poisonSource != null && poisonSource.isPlaying)
            poisonSource.Stop();
    }

    // =====================================================
    // BGM（クロスフェード, DESIGN.md 10.5）
    // =====================================================

    public void PlayTitleBGM() => CrossfadeTo(bgmTitle, 0.5f);
    public void PlayMatchBGM()
    {
        CrossfadeTo(bgmMatch, 0.5f);
        SetTenseLayer(false, 0f);
    }
    public void StopBGM(float fade = 0.5f) => CrossfadeTo(null, fade);

    public void PlayResultJingle()
    {
        StopBGM(1.0f);
        if (bgmResultJingle != null) PlaySE(bgmResultJingle); // ループなし単発、SE 扱いで上に乗せる
    }

    // HP30% 帯で緊迫レイヤーを重ねる/戻す（DESIGN.md 10.5）
    public void SetTenseLayer(bool on, float fade = 1.0f)
    {
        if (tenseActive == on) return;
        tenseActive = on;
        if (bgmTense == null) return;
        if (on && bgmTense.clip != bgmMatchTense)
        {
            bgmTense.clip = bgmMatchTense;
            if (bgmMatchTense != null && bgmPrimary != null && bgmPrimary.isPlaying)
            {
                bgmTense.time = bgmPrimary.time; // 主レイヤーと位相を合わせる
                bgmTense.Play();
            }
        }
        StartFade(bgmTense, on ? 1f : 0f, fade);
    }

    private void CrossfadeTo(AudioClip next, float fade)
    {
        if (bgmPrimary == null) return;
        StartCoroutine(CrossfadeRoutine(next, fade));
    }

    private IEnumerator CrossfadeRoutine(AudioClip next, float fade)
    {
        // 現レイヤーをフェードアウト
        yield return FadeRoutine(bgmPrimary, 0f, fade * 0.6f);
        bgmPrimary.Stop();
        if (next != null)
        {
            bgmPrimary.clip = next;
            bgmPrimary.Play();
            yield return FadeRoutine(bgmPrimary, 1f, fade);
        }
    }

    private void StartFade(AudioSource src, float target, float dur) => StartCoroutine(FadeRoutine(src, target, dur));

    private IEnumerator FadeRoutine(AudioSource src, float target, float dur)
    {
        if (src == null) yield break;
        float start = src.volume, t = 0f;
        if (dur <= 0f) { src.volume = target; yield break; }
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // メニュー（timeScale=0）でも進む
            src.volume = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        src.volume = target;
    }
}
