using UnityEngine;

// BlockSpike 破壊時または InterferencePoison で生成される毒エリア。
// アリーナ上から落下し、パドル付近で停止。接触中のパドルに毎秒 HP ダメージを与える。
public class ZonePoison : MonoBehaviour
{
    [SerializeField] private float fallSpeed       = 6f;
    [SerializeField] private float duration        = 10f;
    [SerializeField] private float detectionRadius = 1.0f;

    private int   playerIndex;
    private float targetWorldY;
    private bool  landed;

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
                Destroy(gameObject, duration);
            }
            return;
        }

        if (GameManager.Instance?.GetCurrentState() != GameManager.GameState.Playing) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var col in hits)
        {
            if (col.GetComponent<PlayerController>() != null)
            {
                GameManager.Instance.OnPoisonTick(playerIndex, Time.deltaTime);
                break;
            }
        }
    }
}
