using UnityEngine;

// InterferencePoison（AttackPoison 取得時）で生成される毒エリア。
// アリーナ上から落下し、パドル付近で停止。接触中のパドルに毎秒 HP ダメージを与える。
public class ZonePoison : MonoBehaviour
{
    [SerializeField] private float fallSpeed       = 10f;
    [SerializeField] private float duration        = 5f;
    [SerializeField] private float detectionRadius = 1.0f;

    private int   playerIndex;
    private float targetWorldY;
    private bool  landed;
    private bool  loopStarted;

    private readonly Collider[] _overlapBuffer = new Collider[4];

    public void Setup(int playerIndex, float targetWorldY)
    {
        this.playerIndex  = playerIndex;
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
                loopStarted = true;
                AudioManager.Instance?.StartPoisonLoop(); // 滞在中ループ SE（DESIGN.md 10.4）
                Destroy(gameObject, duration);
            }
            return;
        }

        if (GameManager.Instance?.GetCurrentState() != GameManager.GameState.Playing) return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (_overlapBuffer[i].GetComponent<PlayerController>() != null)
            {
                GameManager.Instance.OnPoisonTick(playerIndex, Time.deltaTime);
                break;
            }
        }
    }

    void OnDestroy()
    {
        if (loopStarted) AudioManager.Instance?.StopPoisonLoop();
    }
}
