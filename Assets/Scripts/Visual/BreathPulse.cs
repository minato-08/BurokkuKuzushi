using UnityEngine;

// Material の HDR カラー Intensity を Sin 波で脈動させる
// SpriteRenderer / UI.Image どちらにもアタッチ可能
// Bloom Threshold を超えたり下回ったりすることで「光が呼吸する」演出になる
public class BreathPulse : MonoBehaviour
{
    [Header("HDR Intensity 範囲")]
    [SerializeField] private float minIntensity = 1f;
    [SerializeField] private float maxIntensity = 3f;

    [Header("周期（秒）")]
    [SerializeField] private float cycleSeconds = 2f;

    [Header("Material プロパティ")]
    [Tooltip("HDRUnlit シェーダーなら _BaseColor / UI/HDRTint なら _TintColor")]
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Tooltip("色相と彩度。Intensity だけ Sin で変化、RGB の比率はこの色")]
    [SerializeField] private Color baseColor = Color.white;

    [Header("オプション")]
    [Tooltip("Time.time の代わりに unscaledTime を使う（HitStop 中も止まらない）")]
    [SerializeField] private bool useUnscaledTime = false;

    [Tooltip("開始位相をランダム化（複数の枠で同期させたくないとき）")]
    [SerializeField] private bool randomizePhase = false;

    private Material instanceMaterial;
    private float phaseOffset;

    void Awake()
    {
        // SpriteRenderer 優先
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            instanceMaterial = sr.material; // SpriteRenderer.material は自動インスタンス化
        }
        else
        {
            var img = GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                // UI.Image の場合は明示的にインスタンス化
                instanceMaterial = Instantiate(img.material);
                img.material = instanceMaterial;
            }
        }

        if (randomizePhase) phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (instanceMaterial == null) return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float wave = Mathf.Sin(t * Mathf.PI * 2f / cycleSeconds + phaseOffset) * 0.5f + 0.5f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, wave);

        Color c = baseColor * intensity;
        c.a = baseColor.a;
        instanceMaterial.SetColor(colorPropertyName, c);
    }

    void OnDestroy()
    {
        if (instanceMaterial != null) Destroy(instanceMaterial);
    }
}
