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

    private Rigidbody rb;
    private bool frozen = false;
    private Vector3 originalScale;
    private Coroutine widthRoutine;

    public void Freeze()   => frozen = true;
    public void Unfreeze() => frozen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        originalScale = transform.localScale;
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

        // SkillForceCatch: パドルにボールが当たった瞬間を検出して強制キャッチ
        SkillController sc = (transform.parent ?? transform).GetComponentInChildren<SkillController>();
        if (sc == null || !sc.IsForceCatchActive) return;
        if (ball == null || ball.isExtraBall) return;

        sc.ConsumeForceCatch();
        Vector3 catchLocalPos = transform.localPosition + new Vector3(0f, 0.7f, 0f);
        ball.PrepareRespawn(catchLocalPos);
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

        // ローカル座標で移動を管理（親Arenaの座標系で動く）
        Vector3 localPos = transform.localPosition;
        localPos.x += move * speed * Time.deltaTime;
        localPos.x = Mathf.Clamp(localPos.x, -xLimit, xLimit);
        localPos.y = paddleLocalY;
        localPos.z = paddleLocalZ;
        transform.localPosition = localPos;
    }
}
