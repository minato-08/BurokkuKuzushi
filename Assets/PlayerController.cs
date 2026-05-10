using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("プレイヤー設定")]
    [SerializeField] private int playerIndex = 1;
    [SerializeField] public float speed = 10f;

    [Header("移動制限（ローカル座標）")]
    [SerializeField] private float xLimit = 5.5f;
    [SerializeField] private float paddleLocalY = -5f;
    [SerializeField] private float paddleLocalZ = 0f;

    private Rigidbody rb;

    // ArenaController からサイズを受け取って自動設定する
    public void ConfigureFromArena(float halfWidth, float halfHeight, float paddleMargin)
    {
        xLimit       = halfWidth;
        paddleLocalY = -(halfHeight - paddleMargin);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Rigidbodyをkinematicにする
        // → 物理演算で動かされない（ボールがぶつかっても押されない）
        // → ただし衝突判定は通常通り行われ、ボールはちゃんと跳ね返る
        // ワールド座標前提のRigidbodyConstraintsを使う必要がなくなる
        rb.isKinematic = true;
    }

    void Update()
    {
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
