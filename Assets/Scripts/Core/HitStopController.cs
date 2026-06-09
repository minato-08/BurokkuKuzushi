using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アリーナ内のヒットストップ（一時停止 + シェイク）を管理する
// Time.timeScale は使わず IFreezable で個別に制御する
// シェイク対象は単カメラ運用に合わせてアリーナ Transform 自体（カメラではない）
public class HitStopController : MonoBehaviour
{
    [Header("シェイク強度")]
    [SerializeField] private float shakeIntensityNormal = 0.08f;
    [SerializeField] private float shakeIntensityStrong = 0.20f;
    [SerializeField] private float shakeFrequency       = 25f; // 連続ノイズの周波数
    [SerializeField] private float frameShakeMultiplier = 1f;  // アリーナ枠の揺れ倍率（中身に対する比）

    private readonly List<IFreezable> freezables = new List<IFreezable>();
    private Transform shakeTarget;
    private Coroutine activeRoutine;
    private Vector3 shakeBaseLocalPos;
    private bool activeFroze;   // 進行中ルーチンがフリーズを伴うか（割り込み時に未フリーズ対象を Unfreeze しないため）

    // アリーナ枠（P{N}ArenaFrame, UI キャンバス上の SpriteRenderer）も同じワールド変位で揺らす。
    // キャンバスのスケールに依存しないよう localPosition ではなく world position をオフセットする。
    private Transform frameTarget;
    private Vector3 frameBasePos;

    public void SetShakeTarget(Transform t) => shakeTarget = t;
    public void SetFrameShakeTarget(Transform t) => frameTarget = t;

    void Awake() => ApplySharedConfig();

    // 共有設定があればシェイク強度を一元値で上書き（null セーフ）。
    private void ApplySharedConfig()
    {
        var c = ArenaSharedConfig.Instance;
        if (c == null) return;
        shakeIntensityNormal = c.shakeIntensityNormal;
        shakeIntensityStrong = c.shakeIntensityStrong;
        shakeFrequency       = c.shakeFrequency;
        frameShakeMultiplier = c.frameShakeMultiplier;
    }

    public void RegisterFreezable(IFreezable f)
    {
        if (f != null && !freezables.Contains(f))
            freezables.Add(f);
    }

    // frames: 停止フレーム数（60fps想定）、strong: 強シェイクか否か、shake: カメラシェイク有無、
    // freeze: フリーズを伴うか（false=シェイクのみ。底到達/スライド着地などボール衝突でないイベント用, DESIGN.md 5.x）
    public void TriggerHitStop(int frames, bool strong = false, bool shake = true, bool freeze = true)
    {
        if (frames <= 0) return;
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            RestoreShakeTarget();
            if (activeFroze) UnfreezeAll();   // 前ルーチンが未フリーズ（shake-only）なら Unfreeze しない（速度復元で壊れる）
        }
        activeFroze = freeze;
        float intensity = !shake ? 0f : (strong ? shakeIntensityStrong : shakeIntensityNormal);
        activeRoutine = StartCoroutine(HitStopRoutine(frames / 60f, intensity, freeze));
    }

    private IEnumerator HitStopRoutine(float duration, float intensity, bool freeze)
    {
        if (freeze) FreezeAll();
        if (shakeTarget != null) shakeBaseLocalPos = shakeTarget.localPosition;
        if (frameTarget != null) frameBasePos     = frameTarget.position;

        // ノイズの開始位置を毎回ずらして、揺れのパターンを発火ごとに変える。
        float seedX = Random.value * 100f;
        float seedY = Random.value * 100f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (intensity > 0f)
            {
                // 毎フレームのランダム瞬間ワープではなく、時間で連続サンプルする Perlin ノイズ＋
                // 終端へ向けた減衰で滑らかに揺らす（細い Bloom 枠のストロボ状チラつき対策）。
                float decay = 1f - Mathf.Clamp01(elapsed / duration);
                float n  = elapsed * shakeFrequency;
                float ox = Mathf.PerlinNoise(seedX, n) * 2f - 1f; // -1..1
                float oy = Mathf.PerlinNoise(seedY, n) * 2f - 1f;
                Vector3 offset = new Vector3(ox, oy, 0f) * (intensity * decay);

                // アリーナ（local）と枠（world）へ同じ offset を適用＝同期。枠は倍率で個別に弱められる。
                if (shakeTarget != null) shakeTarget.localPosition = shakeBaseLocalPos + offset;
                if (frameTarget != null) frameTarget.position      = frameBasePos     + offset * frameShakeMultiplier;
            }
            yield return null;
        }

        RestoreShakeTarget();

        if (freeze) UnfreezeAll();
        activeRoutine = null;
    }

    private void RestoreShakeTarget()
    {
        if (shakeTarget != null) shakeTarget.localPosition = shakeBaseLocalPos;
        if (frameTarget != null) frameTarget.position      = frameBasePos;
    }

    private void FreezeAll()
    {
        for (int i = freezables.Count - 1; i >= 0; i--)
        {
            if (freezables[i] == null) { freezables.RemoveAt(i); continue; }
            freezables[i].Freeze();
        }
    }

    private void UnfreezeAll()
    {
        for (int i = freezables.Count - 1; i >= 0; i--)
        {
            if (freezables[i] == null) { freezables.RemoveAt(i); continue; }
            freezables[i].Unfreeze();
        }
    }
}
