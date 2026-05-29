using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IFreezable
{
    [Header("プレイヤー設定")]
    [SerializeField] private int playerIndex = 1;
    [SerializeField] public float speed = 10f;

    [Header("移動制限（ローカル座標）")]
    [SerializeField] private float xLimit = 5.5f;
    [SerializeField] private float paddleLocalY = -5f;
    [SerializeField] private float paddleLocalZ = 0f;

    [Header("パドル衝突ヒットストップ（フレーム数・0=なし）")]
    [SerializeField] private int paddleBounceFrames = 0;

    [Header("アイテム取得フラッシュ（DESIGN.md 12.17）")]
    [SerializeField] private Color buffFlashColor   = new Color(0.306f, 0.765f, 1.000f); // Cyan
    [SerializeField] private Color attackFlashColor = new Color(1.000f, 0.298f, 0.235f); // Red
    [SerializeField] private Color trapFlashColor   = new Color(0.792f, 0.286f, 0.851f); // Purple
    [SerializeField] private float pickupFlashDuration = 0.1f;

    private Rigidbody rb;
    private bool frozen = false;
    private Vector3 originalScale;
    private Coroutine widthRoutine;
    private bool inputReversed = false;
    private Coroutine reverseRoutine;

    private Renderer paddleRenderer;
    private Color    originalColor;
    private Coroutine flashRoutine;

    public void Freeze()   => frozen = true;
    public void Unfreeze() => frozen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        originalScale = transform.localScale;

        paddleRenderer = GetComponent<Renderer>();
        if (paddleRenderer != null) originalColor = paddleRenderer.material.color;
    }

    // アイテム取得時にパドルを系統色で 0.1s フラッシュ（ItemDrop から呼ばれる）
    public void OnItemPickup(ItemCategory category)
    {
        if (paddleRenderer == null) return;
        Color flash = category switch
        {
            ItemCategory.Attack => attackFlashColor,
            ItemCategory.Trap   => trapFlashColor,
            _                   => buffFlashColor
        };
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(flash));
    }

    private System.Collections.IEnumerator FlashRoutine(Color flash)
    {
        paddleRenderer.material.color = flash;
        yield return new WaitForSeconds(pickupFlashDuration);
        paddleRenderer.material.color = originalColor;
        flashRoutine = null;
    }

    public void SetWidthTemporary(float multiplier, float duration)
    {
        if (widthRoutine != null) StopCoroutine(widthRoutine);
        widthRoutine = StartCoroutine(WidthRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator WidthRoutine(float multiplier, float duration)
    {
        transform.localScale = new Vector3(originalScale.x * multiplier, originalScale.y, originalScale.z);
        yield return new WaitForSeconds(duration);
        transform.localScale = originalScale;
        widthRoutine = null;
    }

    // TrapBall_Reversed: 左右入力を duration 秒反転（DESIGN.md 5.5.3 / 12.18）
    // 発射確定キーは LaunchAimer 側で処理されるため反転の影響を受けない
    public void SetInputReversedTemporary(float duration)
    {
        if (reverseRoutine != null) StopCoroutine(reverseRoutine);
        reverseRoutine = StartCoroutine(ReverseRoutine(duration));
    }

    private System.Collections.IEnumerator ReverseRoutine(float duration)
    {
        inputReversed = true;
        yield return new WaitForSeconds(duration);
        inputReversed = false;
        reverseRoutine = null;
    }

    // パドルとボールの衝突処理
    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("BallTag")) return;

        BallScript ball = collision.gameObject.GetComponent<BallScript>();

        // パドルバウンスヒットストップ（0フレームはスキップ）
        if (paddleBounceFrames > 0 && ball != null)
        {
            float mul = ball.GetHitStopMultiplier();
            GetArena()?.TriggerHitStop(Mathf.RoundToInt(paddleBounceFrames * mul));
        }
    }

    private ArenaController GetArena()
    {
        return (transform.parent ?? transform).GetComponentInChildren<ArenaController>();
    }

    void Update()
    {
        if (frozen) return;

        float move = 0f;

        if (Keyboard.current == null) return;

        if (playerIndex == 1)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                move = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                move = 1f;
        }
        else if (playerIndex == 2)
        {
            if (Keyboard.current.jKey.isPressed)
                move = -1f;
            if (Keyboard.current.lKey.isPressed)
                move = 1f;
        }

        if (inputReversed) move = -move;

        // ローカル座標で移動を管理（親Arenaの座標系で動く）
        Vector3 localPos = transform.localPosition;
        localPos.x += move * speed * Time.deltaTime;
        localPos.x = Mathf.Clamp(localPos.x, -xLimit, xLimit);
        localPos.y = paddleLocalY;
        localPos.z = paddleLocalZ;
        transform.localPosition = localPos;
    }
}
