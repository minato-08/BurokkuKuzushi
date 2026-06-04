using UnityEngine;

// 1つのアリーナをまとめて管理するコントローラ
public class ArenaController : MonoBehaviour
{
    [Header("プレイヤー紐付け")]
    [SerializeField] public int playerIndex = 1;

    [Header("アリーナサイズ（SpawnItem の底面計算に使用）")]
    [SerializeField] public float arenaHalfWidth  = 5f;
    [SerializeField] public float arenaHalfHeight = 4.5f;

    [Header("アリーナ内の主要オブジェクト")]
    [SerializeField] private BallScript ball;
    [SerializeField] private BlockSpawner spawner;

    [Header("ボール初期位置オフセット（パドル上端からの距離）")]
    [SerializeField] private float ballSpawnOffsetY = 1f;

    [Header("メトロノーム発射")]
    [SerializeField] private LaunchAimer launchAimer;

    // HYPER スキルの床。手動配置したオブジェクトをバインドすると、発動中だけ SetActive(true) にして使う
    // （位置・サイズ・マテリアル・コライダーを Unity 上で調整可能）。**非アクティブで配置・コライダー(非トリガー)付き**を想定。
    // 未バインドなら実行時に簡易キューブを生成（null セーフフォールバック）。
    [Header("HYPER スキルの床（任意）")]
    [SerializeField] private GameObject hyperFloor;
    private Coroutine hyperFloorRoutine;

    // シェイク対象。ボール（非キネマティック Rigidbody）はこの配下に置かない＝シェイクに
    // 引きずられないようにするため、壁/パドル/ブロック等を収める ShakeRoot を揺らす。
    // 未設定なら名前で解決、それも無ければ ArenaRoot にフォールバック（後方互換）。
    [SerializeField] private Transform shakeRoot;
    private HitStopController hitStop;
    private SkillController   skillController;
    private PlayerController  cachedPlayer;
    private UIManager         cachedUIManager;

    // ArenaController は Arena の子なので、兄弟オブジェクトには親から辿る
    private Transform ArenaRoot => transform.parent != null ? transform.parent : transform;

    public BlockSpawner  GetSpawner()          => spawner;
    public BallScript    GetBall()             => ball;
    public SkillController GetSkillController() => skillController;

    // HYPER スキル: Dead Zone 付近に一時的な床を出してボールを落としにくくする（DESIGN.md 5.6）。
    // バインド済みオブジェクトがあれば発動中だけ SetActive(true)（位置/サイズ/見た目は Unity 上で調整）、
    // 未バインドなら ShakeRoot 配下に簡易キューブを実行時生成する（null セーフフォールバック）。
    public void SpawnHyperFloor(float duration)
    {
        if (hyperFloor != null)
        {
            if (hyperFloorRoutine != null) StopCoroutine(hyperFloorRoutine);
            hyperFloorRoutine = StartCoroutine(HyperFloorRoutine(duration));
            return;
        }
        SpawnRuntimeHyperFloor(duration);
    }

    private System.Collections.IEnumerator HyperFloorRoutine(float duration)
    {
        hyperFloor.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (hyperFloor != null) hyperFloor.SetActive(false);
        hyperFloorRoutine = null;
    }

    // 未バインド時のフォールバック（手応えだけ確保。見た目調整するならオブジェクトをバインドする）
    private void SpawnRuntimeHyperFloor(float duration)
    {
        Transform sr = ShakeRootResolved();
        float paddleY = cachedPlayer != null ? cachedPlayer.transform.localPosition.y : -8f;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube); // BoxCollider 付き＝ボールが跳ね返る
        floor.name = "HyperFloor_Runtime";
        floor.transform.SetParent(sr, worldPositionStays: false);
        floor.transform.localPosition = new Vector3(0f, paddleY - 1.2f, 0f);
        floor.transform.localScale    = new Vector3(arenaHalfWidth * 2f, 0.3f, 1f);

        var r = floor.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(1.0f, 0.5f, 0.1f, 1f); // ホットなオレンジ

        Object.Destroy(floor, duration);
    }

    // BURST スキル: 一定時間 LaunchAimer を連射モードにする（DESIGN.md 5.6）
    public void BeginBurst(int shots, float duration, float ballLifetime)
    {
        launchAimer?.BeginBurst(shots, duration, ballLifetime);
    }

    // BURST の 1 発: ボール生成位置（パドル上）から指定方向へ追加ボールを発射する
    public void SpawnBurstBall(Vector3 localDir, float lifetime)
    {
        if (ball == null) return;
        GameObject extra = Object.Instantiate(ball.gameObject, ball.transform.parent);
        extra.name = "Ball_Burst";
        BallScript bs = extra.GetComponent<BallScript>();
        bs.isExtraBall = true; // 時間加速なし・ラウンドリセットで破棄される
        hitStop?.RegisterFreezable(bs);
        StartCoroutine(LaunchBurstRoutine(bs, localDir, lifetime));
    }

    private System.Collections.IEnumerator LaunchBurstRoutine(BallScript bs, Vector3 localDir, float lifetime)
    {
        yield return null; // BallScript.Start() を待つ
        if (bs == null) yield break;
        bs.transform.localPosition = GetBallSpawnLocalPos(); // パドル上から発射
        bs.LaunchInDirection(localDir);
        yield return new WaitForSeconds(lifetime);
        if (bs != null) Object.Destroy(bs.gameObject);
    }

    // シェイク対象（ShakeRoot）を Awake と同じ規則で解決する（HyperFloor の親に使用）
    private Transform ShakeRootResolved()
    {
        if (shakeRoot != null) return shakeRoot;
        Transform found = ArenaRoot.Find("ShakeRoot");
        return found != null ? found : ArenaRoot;
    }

    public void SpawnItem(Vector3 worldPos, ItemType type)
    {
        AudioManager.Instance?.PlayItemDrop(playerIndex); // アイテム出現 SE（DESIGN.md 10.4）

        Sprite icon = ArenaSharedConfig.Instance?.GetItemIcon(type);
        GameObject go = icon != null ? CreateIconItem(type, icon) : CreateSphereItem(type);

        go.transform.SetParent(ArenaRoot, worldPositionStays: true);
        go.transform.position = worldPos;

        go.AddComponent<ItemDrop>().Setup(type, playerIndex, this);
    }

    // Bloom 発光する透過スプライト用マテリアル（全アイテムで共有・遅延生成）。
    private static Material _iconMaterial;
    private static Material IconMaterial
    {
        get
        {
            if (_iconMaterial == null)
            {
                Shader sh = Shader.Find("Custom/HDRSprite");
                if (sh != null) _iconMaterial = new Material(sh);
            }
            return _iconMaterial;
        }
    }

    // アイコンスプライトを持つ落下アイテム本体（SpriteRenderer）。
    // ピックアップ判定は ItemDrop 側がパドルを OverlapSphere で拾うため、本体にコライダーは不要。
    private GameObject CreateIconItem(ItemType type, Sprite icon)
    {
        GameObject go = new GameObject("Item_" + type);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = icon;

        var cfg = ArenaSharedConfig.Instance;

        // HDR スプライトマテリアルで Bloom 発光。
        // ※ SpriteRenderer.color は Color32(0〜1) にパックされ 1 超がクランプされるため使えない。
        //   発光量はクランプされないマテリアル _Color(HDR float4) に載せて閾値(1.0)を越えさせる。
        // シェーダーが見つからなければデフォルトマテリアルのまま（発光なしでアイコンは表示される）。
        if (IconMaterial != null)
        {
            float glow = cfg != null ? cfg.itemIconGlow : 1.8f;
            IconMaterial.SetColor("_Color", new Color(glow, glow, glow, 1f));
            sr.sharedMaterial = IconMaterial;
        }

        // スプライトの実寸（PPU 込み）を基準に、共有設定の目標ワールド高さへスケール。
        float worldSize = cfg != null ? cfg.itemIconWorldSize : 0.9f;
        float spriteH   = icon.bounds.size.y;
        float scale     = spriteH > 0.0001f ? worldSize / spriteH : 1f;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    // アイコン未割り当て時のフォールバック（従来の色付き球）。
    private GameObject CreateSphereItem(ItemType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Item_" + type;
        go.transform.localScale = Vector3.one * 0.4f;

        // アイテムはキネマティック運動なので Rigidbody は不要
        Object.Destroy(go.GetComponent<Rigidbody>());

        // ボールが当たって跳ね返らないようにトリガーにする
        go.GetComponent<Collider>().isTrigger = true;

        go.GetComponent<Renderer>().material.color = ItemDefinition.GetColor(type);
        return go;
    }

    // freeze=false でフリーズせずシェイクのみ（ボール衝突でない底到達・スライド着地用, DESIGN.md 5.x）
    public void TriggerHitStop(int frames, bool strong = false, bool shake = true, bool freeze = true)
    {
        hitStop?.TriggerHitStop(frames, strong, shake, freeze);
    }

    void Awake()
    {
        ApplySharedConfig();

        skillController = gameObject.GetComponent<SkillController>()
                       ?? gameObject.AddComponent<SkillController>();
        skillController.Initialize(playerIndex, this);

        // ArenaController は Arena の子なので、兄弟の Player を探すには親から検索する
        cachedPlayer    = ArenaRoot.GetComponentInChildren<PlayerController>();
        cachedUIManager = Object.FindFirstObjectByType<UIManager>();

        hitStop = GetComponentInChildren<HitStopController>();
        if (hitStop != null)
        {
            // シェイク対象は ShakeRoot（壁/パドル/ブロックを収める）。ボールはこの配下に
            // 置かないので、シェイクで Rigidbody が引きずられない（飛行が止まる/トレイルが裂ける問題の対策）。
            Transform shakeTarget = shakeRoot != null ? shakeRoot : ArenaRoot.Find("ShakeRoot");
            if (shakeTarget == null) shakeTarget = ArenaRoot;
            hitStop.SetShakeTarget(shakeTarget);
            // アリーナ枠（P{N}ArenaFrame）も同じワールド変位でシェイクさせる
            if (cachedUIManager != null)
                hitStop.SetFrameShakeTarget(cachedUIManager.GetArenaFrameTransform(playerIndex));
            if (ball         != null) hitStop.RegisterFreezable(ball);
            if (spawner      != null) hitStop.RegisterFreezable(spawner);
            if (cachedPlayer != null) hitStop.RegisterFreezable(cachedPlayer);
        }

        if (launchAimer != null)
            launchAimer.Initialize(ball, playerIndex, this);
    }

    public void ResetForNewRound()
    {
        if (spawner != null)
            spawner.ClearAndRespawn();

        if (ball != null)
            ball.PrepareRespawn(GetBallSpawnLocalPos());

        // 発射エイマーの位相を中央へリセット（ボールが待機中のままラウンドが終わった場合の引き継ぎ防止）
        launchAimer?.ResetAim();

        // BURST 等で生成された追加ボール（isExtraBall）を破棄（メインボールは残す）
        foreach (var b in ArenaRoot.GetComponentsInChildren<BallScript>())
            if (b.isExtraBall) Object.Destroy(b.gameObject);

        // 未取得の落下アイテムを破棄
        foreach (var item in ArenaRoot.GetComponentsInChildren<ItemDrop>())
            Object.Destroy(item.gameObject);

        // パドルの一時効果（幅・入力反転）を解除
        cachedPlayer?.ResetState();

        foreach (var zone in ArenaRoot.GetComponentsInChildren<ZonePoison>())
            Object.Destroy(zone.gameObject);
        foreach (var zone in ArenaRoot.GetComponentsInChildren<ZoneSlow>())
            Object.Destroy(zone.gameObject);

        // HYPER スキルの床がラウンドを跨いで残らないようにする。
        // バインド済みは破棄せず非アクティブへ、実行時生成のフォールバックは破棄。
        if (hyperFloorRoutine != null) { StopCoroutine(hyperFloorRoutine); hyperFloorRoutine = null; }
        if (hyperFloor != null) hyperFloor.SetActive(false);
        foreach (var t in ShakeRootResolved().GetComponentsInChildren<Transform>())
            if (t != null && t.name == "HyperFloor_Runtime") Object.Destroy(t.gameObject);
    }

    public Vector3 GetBallSpawnLocalPos()
    {
        float paddleY = cachedPlayer != null ? cachedPlayer.transform.localPosition.y : -3.7f;
        return new Vector3(0f, paddleY + ballSpawnOffsetY, 0f);
    }

    public float GetPaddleWorldY()
    {
        float localY = cachedPlayer != null ? cachedPlayer.transform.localPosition.y : -8f;
        return ArenaRoot.position.y + localY;
    }

    public void SpawnZonePoison(Vector3 worldPos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ZonePoison";
        go.transform.SetParent(ArenaRoot, worldPositionStays: true);
        go.transform.position   = worldPos;
        go.transform.localScale = Vector3.one * 0.9f;

        Object.Destroy(go.GetComponent<Rigidbody>());
        go.GetComponent<Collider>().isTrigger = true;
        go.GetComponent<Renderer>().material.color = new Color(0.635f, 1.0f, 0.357f, 0.65f); // #a2ff5b 毒々しい緑

        go.AddComponent<ZonePoison>().Setup(playerIndex, GetPaddleWorldY() + 0.5f);
    }

    public Vector3 GetRandomFloorWorldPos()
    {
        float x = ArenaRoot.position.x + Random.Range(-arenaHalfWidth * 0.8f, arenaHalfWidth * 0.8f);
        return new Vector3(x, ArenaRoot.position.y + 6f, ArenaRoot.position.z);
    }

    public void SpawnZoneSlow(Vector3 worldPos)
    {
        // ZoneSlow はアリーナ中央付近（root Y）に着地させてボール飛行ラインを塞ぐ
        float targetY = ArenaRoot.position.y;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ZoneSlow";
        go.transform.SetParent(ArenaRoot, worldPositionStays: true);
        go.transform.position   = worldPos;
        go.transform.localScale = Vector3.one * 1.5f;

        Object.Destroy(go.GetComponent<Rigidbody>());
        go.GetComponent<Collider>().isTrigger = true;
        go.GetComponent<Renderer>().material.color = new Color(0f, 0.8f, 0.7f, 0.6f);  // シアン

        go.AddComponent<ZoneSlow>().Setup(targetY);
    }

    public void HardenBlocks()
    {
        spawner?.HardenRandomBlocks();
    }

    public void ShowInterferenceOverlay(string label)
    {
        cachedUIManager?.ShowInterferenceOverlay(playerIndex, label);
    }

    // 攻撃アイテム送付時、攻撃者（このアリーナ）の HUD に SENT → 表示
    public void ShowSentLabel(string interferenceLabel)
    {
        cachedUIManager?.ShowSentLabel(playerIndex, interferenceLabel);
    }

    // コンボマイルストーン到達演出（このアリーナのプレイヤー）
    public void ShowComboMilestone(int milestone)
    {
        cachedUIManager?.ShowComboMilestone(playerIndex, milestone);
    }

    // 妨害予約を Incoming インジケータに積む（このアリーナ＝受信側）
    public void PushIncoming(GameManager.InterferenceType type)
    {
        cachedUIManager?.PushIncoming(playerIndex, type);
    }

    // 底到達ペナルティ発生時、死線ラインを白フラッシュ（このアリーナ＝被害者）
    public void FlashDangerLine()
    {
        cachedUIManager?.FlashDangerLine(playerIndex);
    }

    // ラウンド/マッチ決着時、このアリーナ枠を勝者=発光フラッシュ / 敗者=暗転させる
    public void FlashRoundResult(bool isWinner)
    {
        cachedUIManager?.FlashRoundResult(playerIndex, isWinner);
    }

    // 共有設定（ArenaSharedConfig）があれば左右共通のパラメータを自分へ適用（null セーフ）。
    private void ApplySharedConfig()
    {
        var c = ArenaSharedConfig.Instance;
        if (c == null) return;
        arenaHalfWidth   = c.arenaHalfWidth;
        arenaHalfHeight  = c.arenaHalfHeight;
        ballSpawnOffsetY = c.ballSpawnOffsetY;
    }
}
