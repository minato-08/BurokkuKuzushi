# 学習ロードマップ：ゼロからこのプロジェクトを書けるようになるまで

このドキュメントは「C# をほぼ書けない状態」から「BurokkuKuzushi を自力で実装できる状態」までの道筋を示す。  
各ステップに「このプロジェクトのどのコードが書けるようになるか」を対応させている。

---

## 全体の流れ

```
Step 1  C# の文法基礎
Step 2  クラスとオブジェクト指向
Step 3  Unity の基本（MonoBehaviour・シーン）
Step 4  Unity の物理・入力・衝突
Step 5  Unity の UI
Step 6  コルーチン
Step 7  インターフェースと抽象クラス
Step 8  設計パターン（Singleton・イベントハブ）
Step 9  プロジェクト全体の組み立て
```

---

## Step 1：C# の文法基礎

### 学ぶこと

**変数と型**
```csharp
int    hp     = 500;      // 整数
float  speed  = 7.5f;     // 小数（fを忘れずに）
bool   isAlive = true;    // 真偽値
string name   = "Player"; // 文字列
```

**条件分岐**
```csharp
if (hp <= 0)
{
    // HPが0以下のとき
}
else if (hp <= 150)
{
    // HPが150以下のとき
}
else
{
    // それ以外
}
```

**繰り返し**
```csharp
// 6回繰り返す
for (int i = 0; i < 6; i++)
{
    // i = 0, 1, 2, 3, 4, 5
}

// リストの全要素を処理
foreach (var block in allBlocks)
{
    // block を処理
}
```

**メソッド（関数）**
```csharp
// 戻り値なし
void TakeDamage(int amount)
{
    hp -= amount;
}

// 戻り値あり
float GetRatio()
{
    return (float)hp / maxHp;  // int同士の割り算は int → float にキャストが必要
}
```

**プロパティ（読み取り専用フィールドの公開）**
```csharp
public float Ratio => (float)currentHP / maxHP;
// これは以下と同じ意味
public float Ratio { get { return (float)currentHP / maxHP; } }
```

**switch 式（C# 8以降）**
```csharp
int damage = attribute switch
{
    BallAttribute.Ice   => 2,
    BallAttribute.Heavy => 3,
    _                   => 1   // デフォルト
};
```

### このプロジェクトでの使われ方
- `HPSystem.cs` の `TakeDamage / Heal / Ratio` — 変数・条件分岐・プロパティの基本形
- `Block.cs` の `SelectRandomItemType` — switch 式
- `BlockSpawner.cs` の `DescendBlocks` — for ループでリスト全要素を処理

### 確認問題
- `hp` を `500` で初期化して、`TakeDamage(30)` を呼ぶと `hp` はいくつになるか？
- `float ratio = currentHP / maxHP;` と書いたとき、`ratio` は常に `0` か `1` になる。なぜか？

---

## Step 2：クラスとオブジェクト指向

### 学ぶこと

**クラスとインスタンス**
```csharp
// クラス定義（設計図）
public class HPSystem
{
    private int currentHP;    // private: 外から直接触れない
    public  int maxHP;        // public: 外から触れる

    // コンストラクタ（new したときに呼ばれる）
    public HPSystem(int max)
    {
        maxHP    = max;
        currentHP = max;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;
    }
}

// インスタンス化（設計図から実体を作る）
HPSystem p1HP = new HPSystem(500);
p1HP.TakeDamage(30);  // p1HP.currentHP が 470 になる
```

**継承**
```csharp
// 親クラス（基底クラス）
public abstract class EffectDefinition
{
    // abstract: 子クラスが必ず実装しなければならないメソッド
    public abstract void Apply(int playerIndex, ArenaController arena);
}

// 子クラス（派生クラス）
public sealed class EffectHeal : EffectDefinition
{
    public int Amount;

    // override: 親の abstract メソッドを実装する
    public override void Apply(int playerIndex, ArenaController arena)
    {
        GameManager.Instance?.Heal(playerIndex, Amount);
    }
}
```

**なぜ継承を使うのか：**
```csharp
// 型を親クラスで受け取れば、どの子クラスでも同じコードで処理できる
EffectDefinition effect = new EffectHeal { Amount = 50 };
effect.Apply(1, arena);  // EffectHeal の Apply が呼ばれる

EffectDefinition effect2 = new EffectBallSpeed { Multiplier = 1.4f, Duration = 8f };
effect2.Apply(1, arena);  // EffectBallSpeed の Apply が呼ばれる
// → ItemDrop.BuildEffect() が返す型が変わるだけで、呼び出し側のコードは同じ
```

**static（インスタンスなしで使える）**
```csharp
public static class ItemDefinition
{
    // new しなくても ItemDefinition.GetColor(...) で呼べる
    public static Color GetColor(ItemType type) => type switch
    {
        ItemType.Fire => new Color(1f, 0.3f, 0.1f),
        _             => Color.white
    };
}
```

**enum（列挙型）**
```csharp
public enum BallAttribute
{
    Normal,
    Fire,
    Thunder,
    Ice,
    Heavy,
    Pierce
}
// 使い方
BallAttribute attr = BallAttribute.Fire;
if (attr == BallAttribute.Fire) { ... }
```

### このプロジェクトでの使われ方
- `HPSystem.cs` — クラスとインスタンスの基本形（MonoBehaviour でない純粋C#クラス）
- `EffectDefinition.cs` — abstract クラスと継承（アイテム/スキル効果を統一）
- `ItemDrop.cs` の `ItemDefinition` — static クラス
- `Block.cs` / `BallScript.cs` — enum の定義と switch での使用

---

## Step 3：Unity の基本（MonoBehaviour・シーン）

### 学ぶこと

**MonoBehaviour とは**  
Unity のゲームオブジェクトにアタッチするスクリプトの基底クラス。`Awake / Start / Update` などのライフサイクルメソッドを持つ。

```csharp
public class PlayerController : MonoBehaviour
{
    // Awake: このオブジェクトが生成された直後（Start より前）に1回呼ばれる
    void Awake()
    {
        rb = GetComponent<Rigidbody>();  // 同じオブジェクトのコンポーネントを取得
    }

    // Start: 最初のフレームの Update より前に1回呼ばれる
    void Start()
    {
        // 初期化処理
    }

    // Update: 毎フレーム呼ばれる（フレームレート依存）
    void Update()
    {
        // 入力処理・表示更新など
    }

    // FixedUpdate: 一定間隔で呼ばれる（物理演算はここで行う）
    void FixedUpdate()
    {
        // Rigidbody の操作など
    }
}
```

**Awake と Start の順序の重要性**  
BlockSpawner が `Instantiate` した直後に `blockScript.blockType = BlockType.Spike` と設定する。  
→ `Awake()` は Instantiate の瞬間に走るが、`Start()` は次フレームに走る。  
→ `Block.Start()` が走る時点では `blockType` が設定済みになっているため、`RefreshColor()` が正しい色を適用できる。

**GetComponent と SerializeField**
```csharp
// コードで取得する方法
Rigidbody rb = GetComponent<Rigidbody>();

// Inspector から設定する方法（SerializeField）
[SerializeField] private BallScript ball;
// → Unity Editor の Inspector に表示される。ドラッグ&ドロップで参照を設定できる
```

**階層（Hierarchy）でのコンポーネント取得**
```csharp
// 子オブジェクト（子孫を含む）から取得
GetComponentInChildren<ArenaController>()

// 親から取得
GetComponentInParent<ArenaController>()

// シーン全体から取得（重い・乱用禁止）
Object.FindFirstObjectByType<UIManager>()
```

**Instantiate と Destroy**
```csharp
// Prefab や GameObject をコピーして生成
GameObject blockGO = Instantiate(blockPrefab, transform); // 親を transform に設定

// 削除
Destroy(blockGO);
Destroy(blockGO, 5f);  // 5秒後に削除
```

**transform**  
すべての GameObject が持つ位置・回転・スケールの情報。

```csharp
transform.position      // ワールド座標
transform.localPosition // 親基準のローカル座標（このプロジェクトは主にこちら）
transform.localScale    // スケール
transform.parent        // 親の Transform
```

### このプロジェクトでの使われ方
- すべてのスクリプトが `MonoBehaviour` を継承している（`HPSystem` / `EnergySystem` 以外）
- `ArenaController.Awake()` が `GetComponentInChildren` で子から各コンポーネントをキャッシュ
- `BlockSpawner.SpawnRow()` が `Instantiate(blockPrefab, transform)` でブロックを生成

---

## Step 4：Unity の物理・入力・衝突

### 学ぶこと

**Rigidbody（物理オブジェクト）**
```csharp
Rigidbody rb = GetComponent<Rigidbody>();
rb.linearVelocity = new Vector3(3f, 5f, 0f);  // 速度を直接設定
rb.isKinematic = true;  // 物理演算を無効化（transform で直接動かす）
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 高速オブジェクトの貫通防止
```

**衝突イベント**
```csharp
// Collider（isTrigger=false）同士の衝突
void OnCollisionEnter(Collision collision)
{
    // collision.gameObject → 衝突したオブジェクト
    if (collision.gameObject.CompareTag("BallTag"))
    {
        BallScript ball = collision.gameObject.GetComponent<BallScript>();
    }
}

// Collider（isTrigger=true）がもう一方のColliderと重なった
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("BallTag")) { ... }
}
```

**Physics.OverlapSphere（物理クエリ）**
```csharp
// 指定した球体範囲内の Collider を全て取得（ヒープ確保あり）
Collider[] hits = Physics.OverlapSphere(position, radius);

// ヒープ確保なし版（バッファを事前に用意する）
private readonly Collider[] _buffer = new Collider[4];  // 最大4個まで
int count = Physics.OverlapSphereNonAlloc(position, radius, _buffer);
for (int i = 0; i < count; i++)
{
    // _buffer[i] を使う
}
```

**なぜ ItemDrop は OnTriggerEnter を使わないのか：**  
パドルは `isKinematic = true`（物理演算なし）。Unity では kinematic 同士の衝突では `OnTriggerEnter` が発火しない。そのため ItemDrop は毎フレーム `OverlapSphere` でポーリングする。

**Input System**
```csharp
using UnityEngine.InputSystem;

// キーが押されている間（毎フレーム true）
if (Keyboard.current.aKey.isPressed) { ... }

// キーが押された瞬間（1フレームだけ true）
if (Keyboard.current.sKey.wasPressedThisFrame) { ... }
```

**Vector3（3D座標・方向・速度）**
```csharp
Vector3 pos = new Vector3(1f, 2f, 0f);  // x=1, y=2, z=0
Vector3.zero    // (0, 0, 0)
Vector3.up      // (0, 1, 0)
Vector3.down    // (0, -1, 0)

// 正規化（長さを1にする）
Vector3 dir = new Vector3(3f, 4f, 0f);
Vector3 normalized = dir.normalized;  // (0.6, 0.8, 0)

// 内積・外積は今は気にしなくていい
```

**ローカル座標とワールド座標の変換**
```csharp
// ローカル方向 → ワールド方向
Vector3 worldDir = transform.TransformDirection(localDir);
// BallScript.Launch() で使っている:
// ボールのローカルな「上方向」をアリーナのワールド座標での方向に変換する
```

### このプロジェクトでの使われ方
- `BallScript` — Rigidbody + ContinuousDynamic + linearVelocity の直接操作
- `PlayerController` — isKinematic で物理なし、localPosition を直接操作
- `Block.OnCollisionEnter` — ボールとの衝突でダメージ処理
- `DeadZone.OnTriggerEnter` — ボール落下の検知
- `ZonePoison / ZoneSlow` — OverlapSphereNonAlloc でパドル・ボールを検出

---

## Step 5：Unity の UI

### 学ぶこと

**Canvas と Screen Space Overlay**  
キャンバスを Screen Space Overlay にすると、3Dシーンの前面に常に表示される。このプロジェクトでは CenterUI が両方のカメラに重なるように使っている。

**TextMeshPro でテキスト表示**
```csharp
using TMPro;
[SerializeField] private TextMeshProUGUI hpText;

// テキストを更新
hpText.text = $"HP {currentHP} / {maxHP}";
```

**Image（HPバー）**
```csharp
[SerializeField] private Image hpFill;

// fillAmount: 0.0 〜 1.0 で塗りの割合を指定（Image Type = Filled に設定する必要あり）
hpFill.fillAmount = (float)currentHP / maxHP;
hpFill.color = Color.red;
```

**CanvasGroup（透明度制御）**
```csharp
[SerializeField] private CanvasGroup overlay;

overlay.alpha = 1f;   // 完全表示
overlay.alpha = 0f;   // 完全非表示
// CanvasGroup は配下の全 UI 要素の透明度をまとめて制御できる
```

**Color**
```csharp
Color.red
Color.white
new Color(0.4f, 1.0f, 0.4f)         // RGB 0.0〜1.0
new Color(0.5f, 0f, 0.8f, 0.7f)     // RGBA（4つめがアルファ）
```

### このプロジェクトでの使われ方
- `UIManager.cs` — TextMeshProUGUI / Image.fillAmount / Color でHP・スコア・コンボを表示
- `UIManager.ShowInterferenceOverlay` — CanvasGroup.alpha を 0/1 で切り替えてフラッシュ演出
- `SetupHPUI.cs`（Editor スクリプト）— コードで UI オブジェクトを自動生成・配置

---

## Step 6：コルーチン

### 学ぶこと

コルーチンは「途中で処理を止めて次フレームや数秒後に再開できる」メソッド。  
`Update()` の外でタイマーや待機を実現するときに使う。

**基本形**
```csharp
// コルーチンは IEnumerator を返す
private IEnumerator HealRoutine(int amount, float delay)
{
    yield return new WaitForSeconds(delay);  // delay 秒待つ
    hp += amount;
}

// 開始
StartCoroutine(HealRoutine(50, 2f));

// 停止
private Coroutine healRoutine;
healRoutine = StartCoroutine(HealRoutine(50, 2f));
StopCoroutine(healRoutine);
```

**yield return の種類**
```csharp
yield return null;                          // 次のフレームまで待つ
yield return new WaitForSeconds(1.5f);      // 1.5秒待つ（timeScale の影響を受ける）
yield return new WaitForSecondsRealtime(1.5f); // 1.5秒待つ（timeScale=0でも進む）
```

**重複防止パターン（このプロジェクトで頻出）**
```csharp
private Coroutine attributeRoutine;

public void SetAttributeTemporary(BallAttribute attr, float duration)
{
    // 前のコルーチンが走っていれば止める
    if (attributeRoutine != null) StopCoroutine(attributeRoutine);
    attributeRoutine = StartCoroutine(AttributeRoutine(attr, duration));
}

private IEnumerator AttributeRoutine(BallAttribute attr, float duration)
{
    attribute = attr;
    yield return new WaitForSeconds(duration);
    attribute = BallAttribute.Normal;
    attributeRoutine = null;  // 終わったら null に戻す
}
```

**WaitForSecondsRealtime を使う場面：**  
`Time.timeScale = 0` の状態（試合終了後の停止中など）でも動かしたいコルーチンには `WaitForSecondsRealtime` を使う。`WaitForSeconds` は `timeScale=0` だと止まってしまう。

**時限フラグパターン（RetaliationWindow の実装例）：**

「5 秒間だけ有効になるフラグ」のような "時限状態" は、コルーチン + null 判定でシンプルに実装できる。

```csharp
private Coroutine[] retaliationRoutines = new Coroutine[2];
private bool[] retaliationActive = new bool[2];

// 妨害を受けた瞬間に呼ぶ
public void StartRetaliationWindow(int playerIndex)
{
    int i = playerIndex - 1;
    // 既に有効なウィンドウがあればタイマーをリセット
    if (retaliationRoutines[i] != null)
        StopCoroutine(retaliationRoutines[i]);
    retaliationActive[i] = true;
    retaliationRoutines[i] = StartCoroutine(RetaliationRoutine(i));
}

private IEnumerator RetaliationRoutine(int i)
{
    yield return new WaitForSecondsRealtime(5f);  // ラウンド終了中でも動く
    retaliationActive[i] = false;
    retaliationRoutines[i] = null;
}

// 攻撃アイテム取得時に呼ぶ
public bool ConsumeRetaliationWindow(int playerIndex)
{
    int i = playerIndex - 1;
    if (!retaliationActive[i]) return false;
    // 1回で消費する
    StopCoroutine(retaliationRoutines[i]);
    retaliationActive[i] = false;
    retaliationRoutines[i] = null;
    return true;  // 2x 効果を適用
}
```

**時限アイテム（ItemDrop の寿命タイマー）：**

```csharp
private void Start()
{
    StartCoroutine(LifetimeRoutine());
}

private IEnumerator LifetimeRoutine()
{
    float warningTime = 2f;
    float lifetime = 8f;
    yield return new WaitForSeconds(lifetime - warningTime);
    // 残り 2 秒 → 高速点滅開始
    StartCoroutine(BlinkRoutine());
    yield return new WaitForSeconds(warningTime);
    // タイムアップ → 消滅
    Destroy(gameObject);
}

private IEnumerator BlinkRoutine()
{
    var renderer = GetComponent<SpriteRenderer>();
    while (true)
    {
        renderer.enabled = !renderer.enabled;  // 点滅
        yield return new WaitForSeconds(0.1f);
    }
}
```

### このプロジェクトでの使われ方
- `BallScript.AttributeRoutine / SpeedRoutine` — 属性・速度の一時変更
- `PlayerController.WidthRoutine` — パドル幅の一時変更
- `HitStopController.HitStopRoutine` — フリーズ中のカメラシェイク
- `ArenaController.LaunchExtraBallRoutine` — 追加ボールの発射と自動削除
- `GameManager.RetaliationRoutine` — 反撃ウィンドウの 5s 時限管理（Phase F-Combat で実装）
- `ItemDrop.LifetimeRoutine` — アイテム寿命タイマー + 消滅直前の点滅（Phase F-Combat で実装）
- `GameManager.NextRoundCoroutine / MatchOverCoroutine` — ラウンド間の待機
- `UIManager.OverlayRoutine` — 妨害通知フラッシュ

---

## Step 7：インターフェースと抽象クラス

### 学ぶこと

**インターフェース（契約）**
```csharp
// インターフェース定義
public interface IFreezable
{
    void Freeze();
    void Unfreeze();
}

// 実装（複数のクラスが同じインターフェースを実装できる）
public class BallScript : MonoBehaviour, IFreezable
{
    public void Freeze()
    {
        frozenVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector3.zero;
    }
    public void Unfreeze()
    {
        rb.linearVelocity = frozenVelocity;
    }
}

public class PlayerController : MonoBehaviour, IFreezable
{
    public void Freeze()   => frozen = true;
    public void Unfreeze() => frozen = false;
}
```

**なぜインターフェースが便利なのか：**
```csharp
// HitStopController はどのクラスか知らなくていい
// IFreezable を実装していれば何でも管理できる
private readonly List<IFreezable> freezables = new List<IFreezable>();

public void RegisterFreezable(IFreezable f)
{
    freezables.Add(f);
}

private void FreezeAll()
{
    foreach (var f in freezables)
        f.Freeze();  // BallScript でも PlayerController でも BlockSpawner でも同じコードで動く
}
```

**抽象クラス（abstract）**  
インスタンス化できないクラス。子クラスが実装すべきメソッドを定義する。インターフェースと似ているが、フィールドや実装済みメソッドを持てる。

```csharp
public abstract class SkillDefinition
{
    public abstract string DisplayName { get; }

    // virtual: 子クラスが上書きしてもよい（しなくてもよい）
    public virtual bool CanActivate(int playerIndex) => true;

    // abstract: 子クラスが必ず上書きしなければならない
    public abstract void Activate(int playerIndex, ArenaController arena);
}

public sealed class SkillPanic_BlockClear : SkillDefinition
{
    public override string DisplayName => "Block Clear!";

    // CanActivate を上書き: HP 1/3 以下のみ発動可
    public override bool CanActivate(int playerIndex)
        => GameManager.Instance?.GetHPRatio(playerIndex) <= 1f / 3f;

    public override void Activate(int playerIndex, ArenaController arena) { ... }
}
```

**interface vs abstract class の使い分け:**
| | interface | abstract class |
|---|---|---|
| フィールドを持てるか | ✕ | ○ |
| 実装済みメソッドを持てるか | ✕（C# 8以降は可） | ○ |
| 複数実装できるか | ○（複数 implements） | ✕（単一継承） |
| このプロジェクトでの用途 | IFreezable（停止契約） | EffectDefinition / SkillDefinition（効果定義） |

---

## Step 8：設計パターン

### Singleton パターン

「ゲーム中に1つしか存在しないオブジェクト」へのグローバルアクセスを提供する。

```csharp
public class GameManager : MonoBehaviour
{
    // static: クラス自身が保持（インスタンスではなくクラスに紐づく）
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // すでにインスタンスがあれば自分を破棄（シーンリロード対策）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}

// どこからでもアクセスできる
GameManager.Instance.OnBallDropped(1);

// null チェック付き（Instance が null の場合に安全に無視する）
GameManager.Instance?.OnBallDropped(1);
```

**なぜ Singleton を使うのか：**  
複数のスクリプトが `GameManager` への参照を `SerializeField` で持つと管理が煩雑になる。Singleton にすることで「どこからでも `GameManager.Instance.メソッド` で呼べる」状態になる。ただし乱用すると依存関係が見えにくくなるため、このプロジェクトでは GameManager だけに使っている。

---

### イベントハブパターン

「全てのゲームイベントを1か所（GameManager）に集める」設計。

**やっていないこと（悪い例）：**
```csharp
// Block が直接相手の ArenaController にアクセスする → 結合が強くなる
public class Block : MonoBehaviour
{
    private ArenaController opponentArena;  // 相手を直接知っている

    void OnDestroyed()
    {
        // 複雑なロジックが Block に入り込む
        opponentArena.HardenBlocks();
        opponentArena.TriggerHitStop(10);
    }
}
```

**やっていること（このプロジェクト・2026-05-20 仕様刷新後）：**
```csharp
// Block は「壊れた」という事実だけを通知する
public class Block : MonoBehaviour
{
    void OnDestroyed()
    {
        GameManager.Instance.AddScore(ball.playerIndex, baseScore);
        GameManager.Instance.RegisterBlockDestroyed(ball.playerIndex);
        // 「誰に何を送るか」は GameManager が決める（ここではコンボ更新のみ）
        TryDropItem(ball);  // アイテムドロップは確率抽選
    }
}

// GameManager は自陣の状態だけ管理（妨害送付はしない）
public class GameManager : MonoBehaviour
{
    public void RegisterBlockDestroyed(int playerIndex)
    {
        int i = Idx(playerIndex);
        combo[i]++;
        comboTimer[i] = 0f;
        maxCombo[i] = Mathf.Max(maxCombo[i], combo[i]);
        // 妨害送付はここでは行わない（攻撃アイテム経由モデル）
    }

    // 妨害送付の唯一の窓口（攻撃アイテム取得 / 攻撃スキル発動から呼ぶ）
    public void SendInterference(int targetPlayerIndex, InterferencePayload payload)
    {
        ApplyInterference(GetArena(targetPlayerIndex), payload);
    }
}

// ItemDrop が攻撃アイテムを取得した瞬間に妨害が飛ぶ
public class ItemDrop : MonoBehaviour
{
    void OnCaughtByPaddle(int paddleOwnerPi)
    {
        if (IsAttackItem(itemType))
        {
            var payload = BuildAttackPayload(itemType, paddleOwnerPi);
            GameManager.Instance.SendInterference(Opponent(paddleOwnerPi), payload);
        }
        else
        {
            // 強化 / 罠は EffectDefinition.Apply で自陣に作用
            BuildEffect().Apply(paddleOwnerPi, arena);
        }
        Destroy(gameObject);
    }
}
```

> 旧仕様（2026-05-20 以前）では `RegisterBlockDestroyed` がコンボ閾値を見て自動的に `SendSabotageTo(2)` を呼んでいた。コードを刷新する Phase F-Combat 完了までは旧経路が残っている可能性があるため、最新仕様は `DESIGN.md` 5.7 を参照。

---

### Facade パターン（ArenaController）

複数のコンポーネントへのアクセスを1つのクラスでまとめる。

```csharp
// 外部から直接 hitStop.TriggerHitStop() や spawner.HardenRandomBlocks() を呼ばず
// ArenaController を通じて呼ぶ

public class ArenaController : MonoBehaviour
{
    private HitStopController hitStop;
    private BlockSpawner spawner;

    // 外部向けの統一 API
    public void TriggerHitStop(int frames, ...) => hitStop?.TriggerHitStop(frames, ...);
    public void HardenBlocks() => spawner?.HardenRandomBlocks();
}

// 呼び出し側は ArenaController だけ知っていればいい
target.TriggerHitStop(10);
target.HardenBlocks();
```

---

### null 条件演算子（`?.`）

```csharp
// hitStop が null なら何もしない。null でなければ TriggerHitStop を呼ぶ
hitStop?.TriggerHitStop(frames);

// チェーンできる
arena?.GetSpawner()?.ReceiveSpikeRow();
```

---

## Step 9：プロジェクト全体の組み立て

ここまでのステップを踏まえた上で、このプロジェクトを再現する順序を示す。

### 推奨実装順序

```
Phase 1: 土台
  ├─ GameManager（Singleton・GameState・HPSystem）
  ├─ PlayerController（isKinematic・localPosition・入力）
  ├─ BallScript（Rigidbody・速度管理・基本反射）
  └─ DeadZone（OnTriggerEnter・OnBallDropped通知）

Phase 2: ブロックと破壊
  ├─ Block（OnCollisionEnter・TakeDamage・OnDestroyed）
  └─ BlockSpawner（SpawnRow・DescendBlocks・CheckBottomReached）

Phase 3: UI
  ├─ UIManager（毎フレームポーリング・HPバー・スコア）
  └─ MatchResultUI（マッチ終了検知・再戦）

Phase 4: ヒットストップ
  ├─ IFreezable インターフェース
  ├─ BallScript / PlayerController / BlockSpawner への実装追加
  └─ HitStopController（コルーチン・カメラシェイク）

Phase 5: メトロノーム発射
  └─ LaunchAimer（sin波・LineRenderer・自動発射）

Phase 6: アイテム
  ├─ EffectDefinition と実装クラス群
  ├─ ItemDrop（落下・OverlapSphere・BuildEffect）
  └─ Block への TryDropItem 追加

Phase 7: スキル
  ├─ EnergySystem / SkillController
  ├─ SkillDefinition と実装クラス群
  └─ SkillSelectUI

Phase 8: 妨害
  ├─ ZonePoison / ZoneSlow（落下・OverlapSphereNonAlloc）
  ├─ BlockType.Spike 追加
  └─ GameManager の妨害dispatch（SelectInterferenceType・ApplyInterference）
```

---

## 各概念の理解チェックリスト

以下を自分の言葉で説明できればそのステップは完了。

### Step 1（C#基礎）
- [ ] `int` と `float` の違いを説明できる
- [ ] `(float)currentHP / maxHP` と `currentHP / maxHP` の結果がなぜ違うか説明できる
- [ ] `foreach` と `for` の使い分けを説明できる

### Step 2（クラス）
- [ ] `private` と `public` の違いを説明できる
- [ ] なぜ `HPSystem` は `MonoBehaviour` を継承しないのか説明できる
- [ ] `abstract` メソッドを持つクラスをインスタンス化できない理由を説明できる

### Step 3（Unity基本）
- [ ] `Awake` と `Start` の実行タイミングの違いを説明できる
- [ ] `GetComponent<T>()` と `[SerializeField]` の違いを説明できる
- [ ] `localPosition` と `position` の違いを説明できる

### Step 4（物理・入力）
- [ ] `isKinematic = true` の Rigidbody はどう動くか説明できる
- [ ] `OnCollisionEnter` と `OnTriggerEnter` の違いを説明できる
- [ ] `OverlapSphereNonAlloc` を使う理由を説明できる

### Step 5（UI）
- [ ] `fillAmount` を使う Image Type の設定を説明できる
- [ ] `CanvasGroup.alpha` で何ができるか説明できる

### Step 6（コルーチン）
- [ ] `WaitForSeconds` と `WaitForSecondsRealtime` の違いを説明できる
- [ ] コルーチンを重複起動しないようにする方法を説明できる

### Step 7（インターフェース・抽象クラス）
- [ ] `IFreezable` を使わず全クラスに直接 `Freeze()` を呼ぶ場合の問題点を説明できる
- [ ] `abstract` と `virtual` の違いを説明できる

### Step 8（設計パターン）
- [ ] Singleton を使わない場合、何が困るか説明できる
- [ ] イベントハブパターンで、Block が GameManager を経由する理由を説明できる
- [ ] `?.`（null条件演算子）がない場合にどう書くか説明できる

---

## 参考資料

| 項目 | 資料 |
|---|---|
| C# 基礎 | Microsoft 公式 C# チュートリアル |
| Unity 基本 | Unity 公式マニュアル（Scripting > MonoBehaviour） |
| Unity 物理 | Unity 公式マニュアル（Physics > Rigidbody） |
| コルーチン | Unity 公式マニュアル（Scripting > Coroutines） |
| Input System | Unity 公式パッケージドキュメント（Input System） |
| TextMeshPro | Unity 公式マニュアル（UI > TextMeshPro） |

このプロジェクトのコードは各 Step の実例として読むことができる。  
ステップを完了したら `CLAUDE.md` の該当スクリプト説明を読んで、実装を自分で再現してみること。
