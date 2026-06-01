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

    public void SetShakeTarget(Transform t) => shakeTarget = t;

    public void RegisterFreezable(IFreezable f)
    {
        if (f != null && !freezables.Contains(f))
            freezables.Add(f);
    }

    // frames: 停止フレーム数（60fps想定）、strong: 強シェイクか否か、shake: カメラシェイク有無
    public void TriggerHitStop(int frames, bool strong = false, bool shake = true)
    {
        if (frames <= 0) return;
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            RestoreShakeTarget();
            UnfreezeAll();
        }
        float intensity = !shake ? 0f : (strong ? shakeIntensityStrong : shakeIntensityNormal);
        activeRoutine = StartCoroutine(HitStopRoutine(frames / 60f, intensity));
    }

    private IEnumerator HitStopRoutine(float duration, float intensity)
    {
        FreezeAll();
        if (shakeTarget != null)
            shakeBaseLocalPos = shakeTarget.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (shakeTarget != null && intensity > 0f)
            {
                shakeTarget.localPosition = shakeBaseLocalPos + new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0f
                );
            }
            yield return null;
        }

        RestoreShakeTarget();

        UnfreezeAll();
        activeRoutine = null;
    }

    private void RestoreShakeTarget()
    {
        if (shakeTarget != null)
            shakeTarget.localPosition = shakeBaseLocalPos;
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
