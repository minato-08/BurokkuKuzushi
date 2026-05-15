using UnityEngine;
using System.Collections.Generic;

// InterferenceSlow で生成されるボール減速エリア。
// アリーナ上から落下し、アリーナ中央付近で停止。内部のボールを slowFactor 倍に減速する。
public class ZoneSlow : MonoBehaviour
{
    [SerializeField] private float fallSpeed   = 8f;
    [SerializeField] private float duration    = 8f;
    [SerializeField] private float zoneRadius  = 1.5f;
    [SerializeField] private float slowFactor  = 0.5f;  // 内部のボール速度倍率

    private float targetWorldY;
    private bool  landed;

    private readonly Collider[]       _buffer     = new Collider[8];
    private readonly List<BallScript> slowedBalls = new List<BallScript>(4);

    public void Setup(float targetWorldY)
    {
        this.targetWorldY = targetWorldY;
    }

    void Update()
    {
        if (!landed)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            if (transform.position.y <= targetWorldY)
            {
                Vector3 p = transform.position;
                p.y = targetWorldY;
                transform.position = p;
                landed = true;
                Destroy(gameObject, duration);
            }
            return;
        }

        // 前フレームで減速していたボールをすべてリセットしてから再適用する
        foreach (var b in slowedBalls)
            if (b != null) b.slowZoneMul = 1f;
        slowedBalls.Clear();

        int count = Physics.OverlapSphereNonAlloc(transform.position, zoneRadius, _buffer);
        for (int i = 0; i < count; i++)
        {
            BallScript ball = _buffer[i].GetComponent<BallScript>();
            if (ball != null)
            {
                ball.slowZoneMul = slowFactor;
                slowedBalls.Add(ball);
            }
        }
    }

    void OnDestroy()
    {
        // ResetForNewRound による即時破棄でも slowZoneMul を確実に戻す
        foreach (var b in slowedBalls)
            if (b != null) b.slowZoneMul = 1f;
    }
}
