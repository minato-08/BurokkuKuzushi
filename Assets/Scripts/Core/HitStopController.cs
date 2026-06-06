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

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (intensity > 0f)
            {
                // 同一オフセットをアリーナ（local）とアリーナ枠（world）の両方へ適用＝同期して揺れる
                Vector3 offset = new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0f
                );
                if (shakeTarget != null) shakeTarget.localPosition = shakeBaseLocalPos + offset;
                if (frameTarget != null) frameTarget.position      = frameBasePos     + offset;
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
