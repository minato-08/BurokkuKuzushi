using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// アリーナ内のヒットストップ（一時停止 + カメラシェイク）を管理する
// Time.timeScale は使わず IFreezable で個別に制御する
public class HitStopController : MonoBehaviour
{
    [Header("カメラシェイク強度")]
    [SerializeField] private float shakeIntensityNormal = 0.08f;
    [SerializeField] private float shakeIntensityStrong = 0.20f;

    private readonly List<IFreezable> freezables = new List<IFreezable>();
    private Camera arenaCamera;
    private Coroutine activeRoutine;
    private Vector3 cameraBaseLocalPos;

    public void SetCamera(Camera cam) => arenaCamera = cam;

    public void RegisterFreezable(IFreezable f)
    {
        if (f != null && !freezables.Contains(f))
            freezables.Add(f);
    }

    // frames: 停止フレーム数（60fps想定）、strong: 強シェイクか否か
    public void TriggerHitStop(int frames, bool strong = false)
    {
        if (frames <= 0) return;
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            UnfreezeAll();
        }
        float intensity = strong ? shakeIntensityStrong : shakeIntensityNormal;
        activeRoutine = StartCoroutine(HitStopRoutine(frames / 60f, intensity));
    }

    private IEnumerator HitStopRoutine(float duration, float intensity)
    {
        FreezeAll();
        if (arenaCamera != null)
            cameraBaseLocalPos = arenaCamera.transform.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (arenaCamera != null && intensity > 0f)
            {
                arenaCamera.transform.localPosition = cameraBaseLocalPos + new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0f
                );
            }
            yield return null;
        }

        if (arenaCamera != null)
            arenaCamera.transform.localPosition = cameraBaseLocalPos;

        UnfreezeAll();
        activeRoutine = null;
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
