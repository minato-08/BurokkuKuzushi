# コード読解ガイド — C# とこのゲームを同時に理解する

このドキュメントは **「C# も Unity もほぼ初めて」「文法もクラスもよく分からない」** 状態から、
`BurokkuKuzushi`（DUAL BREAK）のコードを自力で読めるようになるためのものです。

- `docs/LEARNING.md` … C# を**ゼロから順に学ぶ**抽象ロードマップ（教科書的）。
- **本ファイル** … このゲームの**実コードそのもの**を教材に、文法とゲームの仕組みを同時に解説（実物密着型）。
- `CLAUDE.md` … 実装の現状サマリー（本ファイルより簡潔・索引向き）。

本ファイルは2部構成です:

- **知識編（第1〜6章）** … 読んで理解する。Unity の仕組み・C# 文法・設計の地図。
- **トレーニング編（第7〜11章）** … 手を動かして身につける。読解・デバッグ・改造・設計の演習と、
  Claude Code を「先生」として使う方法。**読むだけでは身につかない部分はこちら**。

> 読み方の推奨: **第1章 → 第2章 → 第2.5章を順に**（ここで C# の文法をこのコードで身につける）。
> 第3章以降（ゲーム全体の地図）は気になった所だけつまみ食いで OK。
> そのあとは**トレーニング編を週に1演習ずつ**回す。知識編は演習で詰まった時に戻る辞書として使う。

### 鍛えたい力と章の対応表

| 身につけたい力 | 主に使う章 |
|---|---|
| 1. コードを読んで「何が起きてるか」を説明できる | **第7章**（土台: 第1・2・2.5章） |
| 2. エラーと向き合える（デバッグ力） | **第8章** |
| 3. 既存コードを目的どおりに改造できる | **第9章**（地図: 第6章） |
| 4. Unity の仕組みのメンタルモデル | 第1章・第3章（＋全演習で反復） |
| 5. C# の基礎文法 | 第2章（演習で必要になった順に戻って読む） |
| 6. 「設計を日本語で書ける」力 | **第10章** |

---

## 目次

- [第1章 まず世界観 — Unity と C# は何をどう分担しているか](#第1章-まず世界観--unity-と-c-は何をどう分担しているか)
- [第2章 C# 文法を、このゲームの実コードで覚える](#第2章-c-文法をこのゲームの実コードで覚える)
- [★第2.5章 1行ずつ「何が書いてあって、何を動かすか」で読む（行注釈版）](#第25章-1行ずつ何が書いてあって何を動かすかで読む行注釈版)
- [第3章 設計の地図 — 誰が誰を知っているか](#第3章-設計の地図--誰が誰を知っているか)
- [第4章 全スクリプト解説（フォルダ別）](#第4章-全スクリプト解説フォルダ別)
- [第5章 イベントを追う — コードが実際にどう動くか](#第5章-イベントを追う--コードが実際にどう動くか)
- [第6章 「○○を変えたい」逆引き表](#第6章-を変えたい逆引き表)
- [第7章 読解トレーニング — 「読める」を「説明できる」にする](#第7章-読解トレーニング--読めるを説明できるにする)
- [第8章 デバッグ道場 — エラーと向き合う](#第8章-デバッグ道場--エラーと向き合う)
- [第9章 改造ドリル — 壊さず変える・足す](#第9章-改造ドリル--壊さず変える足す)
- [第10章 設計を日本語で書く](#第10章-設計を日本語で書く)
- [第11章 Claude Code を「先生」にする](#第11章-claude-code-を先生にする)

---

## 第1章 まず世界観 — Unity と C# は何をどう分担しているか

### 1.1 Unity と C# の役割分担

- **Unity** = ゲームエンジン（土台）。画面・物理・音・入力・当たり判定を提供する。
- **C#** = そのうえで動く**あなたのルール**を書く言語。
- ゲームの世界には **GameObject（ゲームオブジェクト）** という「物」が並んでいる（ボール、ブロック、パドル、UI…）。
- GameObject 単体はただの空っぽの箱。そこに **Component（部品）** を貼って機能を持たせる。
- **C# スクリプト1つ = Component1種類**。例えば `BallScript.cs` を Ball という GameObject に貼ると、その箱が「ボールとして振る舞う」ようになる。

```
GameObject "Ball"
 ├─ Transform        ← 位置・回転・大きさ（全 GameObject が必ず持つ）
 ├─ Rigidbody        ← 物理（重力・速度）。Unity 標準部品
 ├─ Collider         ← 当たり判定。Unity 標準部品
 └─ BallScript ★     ← あなたが書いた C#。これが「ボールの頭脳」
```

### 1.2 「スクリプトが自動で呼ばれる」という感覚

普通のプログラムは `main()` から上から下へ実行されます。Unity は違います。
**Unity が毎フレーム、貼られている全スクリプトの決まった名前のメソッドを呼んでくれる**のです。

| メソッド名 | いつ呼ばれるか | このゲームでの例 |
|---|---|---|
| `Awake()` | 生成された瞬間（最初の1回） | `GameManager` が自分を `Instance` に登録 |
| `Start()` | `Awake` の後、最初のフレーム直前（1回） | `BallScript` が部品をキャッシュして発射 |
| `Update()` | **毎フレーム**（1秒に約60回） | `PlayerController` がキー入力を読んでパドル移動 |
| `FixedUpdate()` | 物理用の一定間隔 | `BallScript` がボール速度を正規化 |
| `OnCollisionEnter()` | 何かにぶつかった瞬間 | `Block` がボール衝突を検知してダメージ |
| `OnDestroy()` | 消える瞬間 | `ZoneSlow` がボールの減速を解除 |

> 「フレーム」= 画面の1コマ。60fps なら1秒に60コマ。`Update()` が1コマごとに呼ばれる、と思えば OK。

これが分かると、**「このコードはいつ動くの？」** という最大の疑問が解けます。メソッド名を見れば
「生成時か」「毎フレームか」「衝突時か」が分かる、ということです。

---

## 第2章 C# 文法を、このゲームの実コードで覚える

ここが本丸です。**実際にこのゲームで動いているコード**を1つずつ分解して、文法を説明します。
教科書の `int x = 5;` ではなく、あなたのボールの HP を動かしている本物のコードで覚えます。

### 2.1 クラスとは — `HPSystem.cs` を丸ごと解剖する

まず一番小さくて完結したクラス、`Assets/Scripts/Systems/HPSystem.cs` を題材にします。
**「クラス」「フィールド」「メソッド」「コンストラクタ」「プロパティ」** が全部詰まっています。

```csharp
public class HPSystem          // ← ① クラス宣言
{
    private int currentHP;     // ← ② フィールド（このクラスが覚えておく値）
    private int maxHP;

    public int   CurrentHP => currentHP;                       // ← ③ プロパティ
    public float Ratio     => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public bool  IsAlive   => currentHP > 0;

    public HPSystem(int maxHP)  // ← ④ コンストラクタ（生成時に1回走る初期化）
    {
        this.maxHP    = maxHP;
        this.currentHP = maxHP;
    }

    public int TakeDamage(int amount)   // ← ⑤ メソッド
    {
        if (amount <= 0) return 0;
        int before = currentHP;
        currentHP = Mathf.Max(0, currentHP - amount);
        return before - currentHP;
    }
}
```

**① クラス宣言** `public class HPSystem`
「HP を管理する設計図」に `HPSystem` という名前をつけている。`class` = 設計図。
`public` = 他のファイルからも使ってよい（公開）。反対は `private`（このクラス内だけ）。

**② フィールド** `private int currentHP;`
クラスが**覚えておく変数**。`int` は整数の型。`private` なので外からは直接いじれない。
→ 「現在 HP」を内部に隠し持っている。

**③ プロパティ** `public int CurrentHP => currentHP;`
外向きの「読み取り窓口」。`=>` は「これを返す」という短縮記法。
外のコードは `hp.CurrentHP` と書くと中の `currentHP` の値を**読める（が書けない）**。
`Ratio` は `currentHP / maxHP`（割合）をその場で計算して返す。`(float)` は「整数を小数に変換」。
> なぜフィールドを直接 public にしないのか？ → **読めるけど勝手に書けない**ようにして、
> HP の変更は必ず `TakeDamage`/`Heal` を通させるため。これが「カプセル化」。

**④ コンストラクタ** `public HPSystem(int maxHP) { ... }`
クラス名と同じ名前の特別なメソッド。`new HPSystem(500)` と書くと走り、初期値を入れる。
`this.maxHP` の `this` は「このインスタンス自身の」。引数の `maxHP` とフィールドの `maxHP` を区別するため。

**⑤ メソッド** `public int TakeDamage(int amount)`
「ダメージを受ける」という**動作**。`int amount` は受け取る引数（ダメージ量）。
先頭の `int` は**戻り値の型**（=実際に減った HP を返す）。`return` で値を返して終了。
`Mathf.Max(0, ...)` は Unity の便利関数で「0 と比べて大きい方」＝ HP がマイナスにならないようにしている。

> **クラスとインスタンスの違い**（最重要）
> `HPSystem` は**設計図**。`new HPSystem(500)` で作った実体を**インスタンス**と呼ぶ。
> `GameManager` は P1 用と P2 用に**2つ**インスタンスを作っている（`GameManager.cs:107`）:
> ```csharp
> p1HP = new HPSystem(maxHP);   // P1 のHP管理インスタンス
> p2HP = new HPSystem(maxHP);   // P2 のHP管理インスタンス（別物）
> ```
> 同じ設計図から作った別々の箱。だから P1 が殴られても P2 の HP は減らない。

### 2.2 型 — 値には種類がある

C# は「この変数は整数」「これは小数」と**型**を決めて使います。このゲームで頻出の型:

| 型 | 意味 | コード例 |
|---|---|---|
| `int` | 整数 | `public int hp = 1;`（Block.cs） |
| `float` | 小数（末尾に `f`） | `public float speed = 7f;` |
| `bool` | 真偽（true/false） | `public bool isExtraBall = false;` |
| `string` | 文字列 | `"FIRE"`, `"GO!"` |
| `Color` | 色（Unity型） | `Color.white`, `new Color(1f, 0.5f, 0.1f, 1f)` |
| `Vector3` | 3次元座標(x,y,z)（Unity型） | `new Vector3(0f, paddleY, 0f)` |

`var` という書き方もよく出ます（`var c = ArenaSharedConfig.Instance;`）。
これは「型は右辺から自動で分かるから省略させて」という意味。`ArenaSharedConfig c = ...` と同じ。

### 2.3 if と三項演算子 — 分岐

```csharp
// PlayerController.cs:211 — 入力反転中なら移動方向を逆にする
if (inputReversed) move = -move;
```
`if (条件) 文;` は「条件が true なら実行」。

```csharp
// GameManager.cs:578 — playerIndex が 1 なら arena1、違えば arena2 を返す
public ArenaController GetArena(int playerIndex) => playerIndex == 1 ? arena1 : arena2;
```
`条件 ? A : B` は**三項演算子**。「条件が true なら A、false なら B」を1行で書く。
`==` は「等しいか」の比較（`=` は代入なので別物）。このゲームは P1/P2 をこの `? :` で大量に分岐しています。

### 2.4 for と foreach — 繰り返し

```csharp
// BlockSpawner.cs:191 — 1行分のブロックを横に並べる
for (int i = 0; i < blocksPerRow; i++)
{
    float x = startX + i * spacing;
    // ... i 番目のブロックを生成
}
```
`for (初期値; 続ける条件; 1回ごとの更新)`。`i` を 0,1,2... と増やしながら `blocksPerRow` 回繰り返す。

```csharp
// BlockSpawner.cs:369 — 選ばれたブロックを順に硬化させる
foreach (Block b in candidates)
    b.HardenToHp(hardenTargetHp);
```
`foreach` は「コレクション（配列やリスト）の中身を1個ずつ取り出す」。`candidates` の各 `Block` を `b` として処理。

### 2.5 enum — 選択肢に名前をつける

```csharp
// BallScript.cs:3 — ボールの属性は、この6種類のどれか
public enum BallAttribute
{
    Normal, Fire, Thunder, Ice, Heavy, Pierce
}
```
`enum`（列挙型）は「**決まった選択肢の集合**」。`BallAttribute.Fire` のように使う。
数字（0,1,2…）で管理するより `Fire` と書ける方が読みやすく、打ち間違いも防げる。
このゲームの enum: `BallAttribute`（ボール属性）/ `BlockType`（ブロック種別）/ `ItemType`（アイテム15種）/
`GameState`（試合の状態）/ `SkillId`（スキル4種）など。

### 2.6 switch 式 — enum ごとに値を振り分ける

ボールの属性によってダメージが違う、を表現したコード:

```csharp
// BallScript.cs:534 — 属性ごとにダメージ量を返す
public int GetDamage()
{
    return attribute switch
    {
        BallAttribute.Ice    => iceDamage,    // Ice なら 2
        BallAttribute.Heavy  => heavyDamage,  // Heavy なら 3
        BallAttribute.Pierce => pierceDamage, // Pierce なら 1
        _ => normalDamage                     // それ以外は通常 1
    };
}
```
`値 switch { A => 結果1, B => 結果2, _ => 既定 }` は **switch 式**。
「`attribute` が Ice なら `iceDamage` を返す…」を簡潔に書ける。`_` は「上のどれにも当てはまらない場合」。
`if-else` を何個も並べる代わりの、C# らしい書き方。このゲームの色・名前・スコア決定で多用されています
（`ItemDefinition.GetColor`、`Block.OnDestroyed` のスコア選択など）。

### 2.7 MonoBehaviour と `[SerializeField]` — Unity に貼れるクラス

`HPSystem` は普通のクラスでしたが、**GameObject に貼る部品は `MonoBehaviour` を継承**します。

```csharp
// PlayerController.cs:4
public class PlayerController : MonoBehaviour, IFreezable
{
    [SerializeField] private int playerIndex = 1;
    [HideInInspector] public float speed = 10f;
```

`: MonoBehaviour` の `:` は **「継承」**。「PlayerController は MonoBehaviour の一種です」という宣言。
これにより `Update()` などが自動で呼ばれ、Unity の Inspector に表示できるようになる。
（`, IFreezable` も継承の一種。→ 2.10 で説明）

`[SerializeField]` は **Unity の Inspector 画面で値をいじれるようにする目印**。
`private`（コード的には非公開）なのに Inspector には出す、という Unity 特有の書き方。
`[HideInInspector] public` はその逆で「コードからは public だが Inspector には隠す」。

> **重要**: `[SerializeField] private int playerIndex = 1;` の `= 1` はコード上の初期値ですが、
> **Inspector で設定した値が優先**されます。だから CLAUDE.md は「正確な値は Unity Inspector で確認」と
> 念押ししています。コードの初期値 ≠ 実際にシーンで使われる値、ということ。

### 2.8 `?.` と `??` — null（空っぽ）で落ちないための演算子

このゲームのコードには `?.` が大量に出ます。

```csharp
// Block.cs:178
GetArena()?.TriggerHitStop(frames, shake: true, freeze: ball.ShouldFreezeOnImpact());
```

`GetArena()` が**何も返さない（null＝空）**ことがあり得ます。普通に `.TriggerHitStop()` を呼ぶと
「null に対して操作した」とエラーで止まります。`?.` は **「左が null なら、何もせず読み飛ばす」**。
→ 「アリーナが取れたら、その時だけヒットストップを呼ぶ」という安全な書き方。

```csharp
// SkillController.cs:21
public string SkillName => equippedSkill?.DisplayName ?? "---";
```
`??` は **「左が null なら右を使う」**。
→ 「スキルが装備されていればその名前、なければ `"---"`」。

> この2つは「**未バインド（Inspector で繋いでいない）でも落ちない**」設計の要。CLAUDE.md が何度も言う
> 「null セーフ」はこの `?.` `??` で実現されています。UI 要素を全部繋がなくてもゲームが動くのはこのおかげ。

### 2.9 コルーチンと `IEnumerator` — 「時間がかかる処理」を書く

「パドルを10秒間だけ大きくして、その後戻す」をどう書くか？ `Update` で時間を数えるのは面倒です。
Unity には **コルーチン** という「途中で待てるメソッド」があります。

```csharp
// PlayerController.cs:110
public void SetWidthTemporary(float multiplier, float duration)
{
    if (widthRoutine != null) StopCoroutine(widthRoutine);   // 前のがあれば止める
    widthRoutine = StartCoroutine(WidthRoutine(multiplier, duration));  // 開始
}

private System.Collections.IEnumerator WidthRoutine(float multiplier, float duration)
{
    transform.localScale = new Vector3(originalScale.x * multiplier, originalScale.y, originalScale.z); // 大きく
    yield return new WaitForSeconds(duration);  // ← ここで duration 秒「待つ」（中断して後で再開）
    transform.localScale = originalScale;       // 元に戻す
    widthRoutine = null;
}
```

ポイント:
- 戻り値の型が `IEnumerator` のメソッドが**コルーチン**になれる。
- `yield return new WaitForSeconds(duration);` で **「ここで○秒待ってから続きを実行」**。
  普通のメソッドはこんな「待ち」ができない。
- `StartCoroutine(...)` で開始、`StopCoroutine(...)` で中断。
- このゲームは「一時的な効果」を全部この形で書いています（属性付与、速度変化、巨大化、フラッシュ、フェード…）。
  `Coroutine widthRoutine;` という**変数に握っておく**のは、効果が重なったとき前のを `StopCoroutine` で
  打ち切って上書きするため。

> `WaitForSeconds` と `WaitForSecondsRealtime` の違い（このゲームで超重要）:
> 前者は `Time.timeScale` の影響を受ける＝**ポーズ中(timeScale=0)は止まる**。
> 後者は実時間で進む＝**ポーズ中でも進む**。カウントダウンやリザルト演出は後者を使っています
> （`GameManager.cs:279` の `WaitForSecondsRealtime`）。

### 2.10 interface（インターフェース）— 「この機能を持つ」という約束

```csharp
// Systems/IFreezable.cs — 全文これだけ
public interface IFreezable
{
    void Freeze();
    void Unfreeze();
}
```

`interface` は **「中身のない約束ごとの一覧」**。`IFreezable` は「Freeze と Unfreeze ができる」という契約。
これを `BallScript`・`BlockSpawner`・`PlayerController` が「実装」します:

```csharp
// PlayerController.cs:33
public void Freeze()   => frozen = true;
public void Unfreeze() => frozen = false;
```

何が嬉しいか？ `HitStopController` は「ボールかブロックかパドルか」を**気にせず**、
ただ「IFreezable なやつら」をまとめて止められる:

```csharp
// HitStopController.cs:14
private readonly List<IFreezable> freezables = new List<IFreezable>();
// ...止めるとき:
freezables[i].Freeze();   // 中身が何であれ Freeze() を呼ぶだけ
```

> これが「**ポリモーフィズム**（多態性）」。型が違っても「同じ約束を持つ」なら同じ扱いができる。
> ヒットストップが timeScale を使わずに片方のアリーナだけ止められるのは、この interface のおかげ。

### 2.11 abstract クラスと継承 — 「効果」の共通の型

アイテムやスキルの「効果」は種類が多い（属性付与、回復、妨害送付…）。これを共通化しているのが
**abstract（抽象）クラス**です。

```csharp
// EffectDefinition.cs:4
public abstract class EffectDefinition
{
    public abstract void Apply(int playerIndex, ArenaController arena);  // 中身は書かない（各自で）
}

// それを継承した具体的な効果たち
public sealed class EffectHeal : EffectDefinition   // 回復
{
    public int Amount;
    public override void Apply(int playerIndex, ArenaController arena)
        => GameManager.Instance?.Heal(playerIndex, Amount);
}

public sealed class EffectAttack : EffectDefinition  // 妨害送付
{
    public ItemType AttackItem;
    public override void Apply(int playerIndex, ArenaController arena)
    {
        int opponent = playerIndex == 1 ? 2 : 1;
        GameManager.Instance?.SendInterference(opponent, AttackItem);
    }
}
```

- `abstract class` は **「それ単体では使えない、共通の親」**。`Apply` の中身を書かず「子が必ず書くこと」とだけ決める。
- `: EffectDefinition` で継承し、`override` で `Apply` の**中身を埋める**。
- `sealed` は「これ以上継承させない」（最終形）の意味。

何が嬉しいか？ `ItemDrop` は**どの効果かを気にせず**「`EffectDefinition` を1個もらって `.Apply()` を呼ぶ」
だけで済む（`ItemDrop.cs:175`）。新しい効果を追加しても `ItemDrop` 側は変えなくていい。
`SkillDefinition`（`SkillHyper`/`SkillExplosion`/`SkillBurst`/`SkillGiant`）も全く同じ仕組みです。

### 2.12 static — インスタンスを作らずに使う / 1個だけ存在させる

```csharp
// ItemDefinition.cs:22
public static class ItemDefinition
{
    public static Color GetColor(ItemType type) => type switch { ... };
    public static string GetName(ItemType type) => type switch { ... };
}
```
`static` は **「インスタンス（実体）を作らずに、クラス名から直接使う」**。
`ItemDefinition.GetName(ItemType.Fire)` のように、`new` せず呼べる。
道具箱のような「状態を持たない便利関数の集まり」に使う。

もう一つの static の用途が **Singleton（シングルトン）= 「世界に1個だけ」**:

```csharp
// GameManager.cs:21
public static GameManager Instance { get; private set; }

void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;   // 自分を「唯一の GameManager」として登録
}
```
`static` な `Instance` に自分自身を入れることで、どこからでも `GameManager.Instance.GetHP(1)` と
呼べるようになる。試合状態の「真実」が1箇所に集まる仕組み。`AudioManager`・`ArenaSharedConfig` も同じ Singleton。

### 2.13 ジェネリックコレクション — `List` / `HashSet` / `Dictionary`

複数のものをまとめて持つ入れ物。`<...>` は「中身の型」を指定する記法（ジェネリック）。

```csharp
List<Block> allBlocks = new List<Block>();   // BlockSpawner.cs:49 — Block を順番に並べて持つ（追加/削除自在）
HashSet<Block> pierceIgnored = new();         // BallScript.cs:78 — 重複なしの集合（同じ Block を二重登録しない）
Dictionary<ItemType, Sprite> _itemIconMap;    // ArenaSharedConfig.cs:280 — ItemType→画像 の対応表（辞書）
```

- `List<T>` … 順番つきの可変長リスト。`.Add()` で追加、`foreach` で回す。
- `HashSet<T>` … 重複を許さない集合。「もう処理した Block か？」の判定が速い。
- `Dictionary<キー, 値>` … キーから値を引く対応表。`GetItemIcon(type)` が `ItemType` から画像を引くのに使用。

### 2.14 struct — 軽量な「値のかたまり」

```csharp
// GameManager.cs:70 — 「効いている効果」1件分のデータ
public struct ActiveEffect
{
    public ItemEffectSlot slot;
    public string         name;
    public float          endTime;
}
```
`struct` はクラスに似ていますが「**いくつかの値を1セットで持ち運ぶ箱**」用途。
`ActiveEffect`（効果のスロット・名前・終了時刻）のように、関連する値をまとめて扱いたいときに使う。
（クラスとの細かい違いは今は気にしなくて OK。「軽いデータ用のクラス」くらいの理解で十分）

### 2.15 LINQ — コレクションを「絞り込む・並べ替える」一行術

```csharp
// BlockSpawner.cs:361 — 通常ブロックからランダムに hardenCount 個選んで硬化させる
Block[] candidates = allBlocks
    .Where(b => b != null && b.blockType == BlockType.Normal)  // Normal だけ残す
    .OrderBy(_ => Random.value)                                // ランダムに並べ替え
    .Take(hardenCount)                                         // 先頭 hardenCount 個取る
    .ToArray();                                                // 配列に変換
```
`.Where()`・`.OrderBy()`・`.Take()` は **LINQ** という「コレクション加工の道具」。
`b => b.blockType == BlockType.Normal` の `=>` は **ラムダ式**（「その場で作る小さな関数」）で、
「各 b について、この条件を満たすか？」を表す。SQL のような「絞り込み→並べ替え→取り出し」を繋げて書ける。

### 2.16 nullable 型 `?` — 「値が無い」を許す型

```csharp
// SkillController.cs:24
public SkillId? EquippedSkillId => equippedSkill?.Id;
```
`SkillId?` の末尾の `?` は **「SkillId か、もしくは null（未装備）」**。
普通の `SkillId` は必ず何か値が入りますが、「まだスキルを選んでない」状態を表すために `?` で null を許す。
UI 側は `if (id == null)` で「未装備ならアイコンを隠す」と判定できる（`UIManager.cs:251`）。

---

ここまでで、このゲームのコードに出てくる文法はほぼ網羅しました。
**第3章以降は、これらの部品が組み合わさって「ゲーム」になる様子**を見ていきます。

---

## ★第2.5章 1行ずつ「何が書いてあって、何を動かすか」で読む（行注釈版）

この章が、おそらくあなたが一番欲しかったものです。
**「このコードはこう動く」という説明ではなく、「この1行に何が書いてあって、それが画面の何をどう動かしているか」**を、
本物のメソッドを丸ごと、1行ずつ字幕をつけて読みます。

読み方: 左にコード、すぐ下に **`▶`（何が書いてあるか）** と **`🎮`（それがゲームの何を動かすか）** を書きます。

---

### 2.5.1 パドルを動かす — `PlayerController.Update()`（一番「目に見える」コード）

キーを押すとパドルが動く。その全部がこのメソッドです。`Update()` は**毎フレーム（毎コマ）**呼ばれます。

```csharp
void Update()
{
```
`▶` 「毎フレーム実行されるメソッド」の始まり。
`🎮` 1秒に約60回、以下の中身が繰り返される＝なめらかな動きの正体。

```csharp
    if (frozen) return;
```
`▶` `frozen`（凍結中フラグ）が true なら `return`（＝この先を実行せず即終了）。
`🎮` ヒットストップ中はパドルを動かさない。`HitStopController` が `Freeze()` で `frozen=true` にしている。

```csharp
    if (GameManager.Instance == null) return;
    var state = GameManager.Instance.GetCurrentState();
    bool countdown = state == GameManager.GameState.Countdown;
    if (state != GameManager.GameState.Playing && !countdown) return;
```
`▶` 今の試合状態を取得し、「Playing でも Countdown でもない」なら `return` で終了。
`🎮` **タイトル画面やリザルト中はパドルが動かせない**のはこの4行のおかげ。動かせるのは試合中とカウントダウン中だけ。

```csharp
    float dt = countdown ? Time.unscaledDeltaTime : Time.deltaTime;
```
`▶` `dt` に「前フレームからの経過秒」を入れる。三項演算子（2.3）でカウントダウン中だけ `unscaled` を使う。
`🎮` カウントダウン中は `timeScale=0`（時間停止中）なので普通の `deltaTime` は 0 になり動けない。
   だから「停止の影響を受けない `unscaledDeltaTime`」に切り替えて、停止中でもパドルだけは動かせるようにしている。

```csharp
    float move = 0f;
```
`▶` `move`（動く向き）という箱を作り、ひとまず 0（止まる）を入れる。
`🎮` この後キー入力で -1（左）か +1（右）に書き換わる。「今フレームどっちへ動くか」を表す。

```csharp
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
```
`▶` 自分が P1 か P2 かで読むキーを変える。`isPressed`＝「今押されているか」。A/← で `move=-1`、D/→ で `move=+1`。
`🎮` **キー入力を「向きの数字」に翻訳している部分**。P1 は A/D（または矢印）、P2 は J/L。
   押していなければ `move` は 0 のまま＝止まる。`||` は「または」。

```csharp
    if (inputReversed) move = -move;
```
`▶` 入力反転中なら `move` の符号を反転（-1↔+1）。
`🎮` TrapBall_Reversed（罠アイテム）の効果。左キーで右に動く嫌がらせがこの1行。

```csharp
    Vector3 localPos = transform.localPosition;
    localPos.x += move * speed * dt;
```
`▶` 今のパドル位置を `localPos` にコピーし、その x に `move × speed × dt` を足す。
`🎮` **ここが実際にパドルを動かす計算**。「向き(±1) × 速さ × 経過秒」だけ横へずらす。
   `dt` を掛けるのは、PC が速くても遅くても**同じ速度に見える**ようにするため（フレーム数に依存させない）。

```csharp
    localPos.x = Mathf.Clamp(localPos.x, -xLimit, xLimit);
    localPos.y = paddleLocalY;
    localPos.z = paddleLocalZ;
    transform.localPosition = localPos;
}
```
`▶` x を `-xLimit〜xLimit` の範囲に丸め（`Clamp`）、y/z は固定値に。最後に `transform.localPosition` へ書き戻す。
`🎮` `Clamp` が**壁の外に出ないストッパー**。最後の `transform.localPosition = localPos;` で
   **計算した位置を実際の画面上のパドルに反映**＝ここで初めて見た目が動く。
   それまでの行は「箱の中の数字をいじっていただけ」で、この代入で世界に適用される。

> **この章の肝**: コードの大半は「箱(`move`,`localPos`)の中の数字をこねる」作業で、
> 画面が動くのは最後の `transform.localPosition = localPos;` の**たった1行**。
> 「計算」と「世界への反映」は別、と意識すると、どの行が"効く"のか見抜けるようになります。

---

### 2.5.2 ボールがブロックに当たった瞬間 — `Block.OnCollisionEnter()`

`OnCollisionEnter` は**何かが物理的にぶつかった瞬間に Unity が自動で呼ぶ**メソッドです（毎フレームではない）。

```csharp
private void OnCollisionEnter(Collision collision)
{
```
`▶` 「衝突が起きた」時に呼ばれる。`collision` には「ぶつかってきた相手」の情報が入っている。
`🎮` ボール・壁・パドル、何がぶつかっても呼ばれる。だから最初に「相手は誰か」を確かめる必要がある。

```csharp
    if (!collision.gameObject.CompareTag("BallTag")) return;
```
`▶` ぶつかった相手の「タグ」が `"BallTag"` でなければ `return`。`!` は「でない」。
`🎮` **ボール以外がぶつかっても無視する関所**。ブロック同士の接触などで誤作動しないように。

```csharp
    BallScript ball = collision.gameObject.GetComponent<BallScript>();
```
`▶` ぶつかった相手から `BallScript` 部品を取り出して `ball` に入れる。
`🎮` 以降「`ball.GetDamage()`」のように、当たったボールの属性や速度を**問い合わせる窓口**になる。

```csharp
    int damage = ball != null ? ball.GetDamage() : 1;
    bool willBreak = currentHp - damage <= 0;
```
`▶` ボールのダメージ量を取得（`ball` が null なら 1）。それで HP が 0 以下になるなら `willBreak=true`。
`🎮` 「この一撃でブロックが壊れるか？」を先に判定。次の行で「壊れるなら衝突音を鳴らさない（破壊音と被るから）」に使う。

```csharp
    if (ball != null && !willBreak)
        AudioManager.Instance?.PlayBlockHit((int)blockType, ball.playerIndex);
```
`▶` ボールがあり、かつ「壊れない」当たりなら、ブロック種別に応じた衝突音を鳴らす。
`🎮` **コツン、という当たり音**。とどめの一撃のときは鳴らさない（破壊音 `PlayBlockBreak` に任せる）。

```csharp
    if (blockType == BlockType.Absorb)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity *= absorbSpeedMultiplier;
    }
```
`▶` このブロックが Absorb（吸収）種なら、ボールの `Rigidbody`（物理）を取り出し、速度に 0.7 を掛ける。
`🎮` **吸収ブロックに当たるとボールが減速する**挙動。`rb.linearVelocity` がボールの速度ベクトルそのもの。

```csharp
    if (blockType != BlockType.Explosive)
    {
        int frames = ball != null ? ball.GetImpactFrames() : 0;
        if (frames > 0) GetArena()?.TriggerHitStop(frames, shake: true, freeze: ball.ShouldFreezeOnImpact());
    }
```
`▶` 爆発ブロック以外なら、ボールに「手応えフレーム数」を聞き、0より大きければヒットストップを起こす。
`🎮` **当たった瞬間に画面が一瞬止まって揺れる「手応え」**。速くて強い当たりほど `frames` が大きい＝強く止まる。
   （爆発ブロックは破壊時に別途やるので、ここでは除外）

```csharp
    TakeDamage(damage, ball);

    if (ball != null)
        ball.OnHitBlock(this);
}
```
`▶` 自分にダメージを与え（HP を減らす）、最後にボール側の `OnHitBlock` を呼ぶ。
`🎮` `TakeDamage` で **HP が減り、0なら破壊**へ。`ball.OnHitBlock(this)` は**ボールの属性効果の引き金**＝
   Fire なら周囲に延焼、Thunder なら同種連鎖、Pierce なら貫通維持。「自分が壊れる」と「ボールの特殊効果」を分けている。

---

### 2.5.3 「箱の数字をいじる」と「世界に反映する」の見分け方

2.5.1〜2.5.2 で見たとおり、コードの行は大きく2種類に分かれます。これを見分けられると読解が一気に楽になります。

| 種類 | 例 | 見分け方 |
|---|---|---|
| **① 計算・準備**（世界はまだ動かない） | `float move = 0f;` / `int damage = ball.GetDamage();` / `if (...)` | ローカル変数に入れる・条件を調べる・値を聞く |
| **② 世界への反映**（ここで初めて動く/変わる） | `transform.localPosition = localPos;` / `rb.linearVelocity *= 0.7f;` / `Destroy(gameObject);` / `TriggerHitStop(...)` / `AudioManager...Play...()` | `transform`/`rb` への代入、`Destroy`、`Instantiate`、`Play〜`、`SetActive`、他オブジェクトのメソッド呼び出し |

**②に当たる代表的な"世界を動かす"操作**（このゲーム頻出）:

- `transform.localPosition = ...` / `transform.localScale = ...` … **位置・大きさを変える**（見た目が動く）
- `rb.linearVelocity = ...` … **物理速度を変える**（ボールの飛ぶ向き・速さ）
- `Instantiate(prefab, ...)` … **新しい GameObject を生み出す**（ブロック生成、追加ボール）
- `Destroy(gameObject)` … **GameObject を消す**（ブロック破壊、アイテム消滅）
- `renderer.material.color = ...` … **色を変える**（属性カラー、Ball Heat、フラッシュ）
- `gameObject.SetActive(true/false)` … **表示/非表示**（UI パネル、カーソル）
- `someText.text = "..."` … **文字を書き換える**（HP 数値、コンボ数）
- `AudioManager.Instance?.Play〜()` … **音を鳴らす**
- `GameManager.Instance.Xxx()` … **ゲーム状態を変える**（HP 減、コンボ加算、状態遷移）

> 練習として、`BallScript.cs` の `FixedUpdate()` を開いて、各行に①か②のラベルを自分で付けてみてください。
> ②は「`rb.linearVelocity = ...`」のところだけ＝あとは全部「実効速度を計算する準備」だと分かるはずです。
> 分からない行があれば、その行をそのまま貼って聞いてください。①/②と「何を動かすか」を返します。

---

## 第3章 設計の地図 — 誰が誰を知っているか

### 3.1 全体図

```
            ┌───────────────────────────────────────────┐
            │  GameManager  (Singleton・世界に1個)       │  試合状態/HP/スコア/コンボの「真実」
            │  状態機械(GameState) + 全ダメージの最終窓口 │
            └───────────────────────────────────────────┘
                 │ GetArena(1)            │ GetArena(2)
                 ▼                        ▼
        ┌──────────────────┐    ┌──────────────────┐
        │ ArenaController  │    │ ArenaController  │   ← 各アリーナの「司令塔（ファサード）」
        │  (Arena1)        │    │  (Arena2)        │
        ├──────────────────┤    └──────────────────┘
        │ HitStopController│  ← 一時停止＋シェイク（timeScale を使わない）
        │ SkillController  │  ← エナジー/スキル
        │ BallScript       │  ← ボール（物理・属性・速度4層）
        │ BlockSpawner     │  ← ブロック生成・降下
        │ PlayerController │  ← パドル（キー入力）
        │ LaunchAimer      │  ← 発射照準
        └──────────────────┘

  ┌────────────────────┐   ┌──────────────┐   ┌──────────────┐
  │ ArenaSharedConfig  │   │ AudioManager │   │  UIManager   │
  │ 全バランス値を集約    │   │ 音の中央ハブ   │   │ 画面を毎フレ   │
  │ (Singleton)        │   │ (Singleton)  │   │ ーム更新      │
  └────────────────────┘   └──────────────┘   └──────────────┘
```

### 3.2 この設計を貫く5つの考え方

**① 中央ハブ GameManager（Singleton）**
HP・スコア・コンボ・勝敗を**1箇所**に集める。Block も DeadZone も**自分で HP をいじらない**。
必ず `GameManager.Instance.OnXxx()` を呼ぶだけ。→ ルールの真実が散らばらない。

**② 状態機械 GameState で「いつ何が動くか」を統制**
`Title / Settings / SkillSelect / Countdown / Playing / RoundOver / MatchOver` の7状態。
ほぼ全てのスクリプトの先頭に `if (state != Playing) return;` があり、**Playing 以外では何も進まない**。
さらに `Time.timeScale`（時間の倍率）を 0/1 で切り替えて物理ごと止める。

**③ ArenaSharedConfig で全バランス値を一元管理**
左右のアリーナで同じはずの値（ボール速度、ブロック確率、ダメージ量…）を**1個の設定オブジェクト**に集約。
各スクリプトは起動時に `ApplySharedConfig()` でそこから値を読む。
→ 調整は1箇所だけ。未配置でも各自のコード初期値で動く（null セーフ）。

**④ ファサード ArenaController**
GameManager は個々のボールやブロックを直接触らず、ArenaController の簡潔な API
（`HardenBlocks()`、`SpawnZonePoison()`…）越しに操作する。→ 中の構造を知らなくて済む。

**⑤ ポーリング型 UI**
UI は「変わったら教えて」ではなく、`UIManager.Update()` が**毎フレーム** GameManager を
読みに行く（`GetHP`/`GetScore`/`GetCombo`…）。→ イベント配線が要らず、表示が常に最新。

---

## 第4章 全スクリプト解説（フォルダ別）

各ファイルの「役割」＋「C# / 設計の見どころ」を1段落で。ファイルを開く前の地図として使ってください。

### Core/（中核 — 全体を統べる層）

- **`GameManager.cs`**（699行・Singleton）
  試合の頭脳。7状態の**状態機械**、全ダメージの最終窓口 `ApplyDamage`、コンボ/スコア/エナジー計算、
  妨害送付 `SendInterference`、ラウンド/マッチ決着 `EndRound`、カウントダウン等の**コルーチン**群。
  見どころ: `Time.timeScale` と `WaitForSecondsRealtime` の使い分け（ポーズ中も進む演出）。

- **`ArenaController.cs`**（398行）
  1アリーナの**司令塔（ファサード）**。子部品（Ball/Spawner/Player/SkillController/HitStop）を
  `Awake` でキャッシュし、`GetSpawner()`・`SpawnHyperFloor()`・`BeginBurst()` 等の API で包む。
  ゾーン/アイテム/床の生成、ラウンド間リセット `ResetForNewRound` もここ。

- **`HitStopController.cs`**（114行）
  「手応え」の演出。`timeScale` を使わず、`IFreezable` を個別に Freeze→シェイク→Unfreeze する。
  見どころ: interface で型を問わず止める設計（2.10）。`ShakeRoot` の位置をランダムに揺らす。

- **`ArenaSharedConfig.cs`**（323行・Singleton）
  **全ゲームバランス値の唯一の置き場**。パドル/ブロック/ボール/スキル/アイテム/HP/妨害ゾーンの
  数値を1枚に集約。`Dictionary` でアイテム画像・重みを引く。各スクリプトがここから読む。

- **`AudioManager.cs`**（347行・Singleton）
  音の中央ハブ。`AudioSource` プールでラウンドロビン再生、音量を dB 変換、BGM クロスフェード。
  全クリップは**任意**（未割り当てでも `?.` で無音動作）。見どころ: `PlayBlockHit` の種別switch＋クールダウン。

### Systems/（純粋 C# の小道具 — Unity に依存しない）

- **`HPSystem.cs`**（52行）… HP の増減（2.1 で解剖済み）。MonoBehaviour ではない**ただのクラス**。
- **`EnergySystem.cs`**（20行）… スキルゲージの蓄積/消費。同じく純粋クラス。`SkillController` が保持。
- **`IFreezable.cs`**（5行）… `Freeze`/`Unfreeze` の**約束（interface）**。3クラスが実装。

> なぜ HPSystem/EnergySystem は MonoBehaviour にしない？ → Transform も衝突もコルーチンも要らない
> 「ただの計算」だから。シーンに置かず `new` で作れ、テストも容易。判断基準は「Unity 機能が要るか？」。

### Gameplay/（ゲームの実物 — 画面上で動く物たち）

- **`BallScript.cs`**（640行・最大・要注意）
  ボール。`IFreezable` 実装。見どころが多い:
  - **速度4層**: `naturalSpeed(時間加速) × speedMultiplier(アイテム) × slowZoneMul(減速ゾーン) × 属性係数`
    = 実効速度（`EffectiveSpeed()`）。
  - **属性**6種を switch で分岐（Fire=範囲, Thunder=同種連鎖, Pierce=貫通…）。
  - **Pierce 素通り**: 物理反発で軌道が折れないよう `OverlapSphere` で検出し `IgnoreCollision` で直進。
  - **手応え** `GetImpactFrames()`: 速度×攻撃力でヒットストップのフレーム数を算出。
  - **Ball Heat**: コンボが伸びると `Color.Lerp` で白→赤へ加熱（純演出）。

- **`Block.cs`**（402行）
  ブロック1個。`OnCollisionEnter` でボール衝突を検知→ `TakeDamage` → 0で `OnDestroyed`。
  見どころ: **多重破壊ガード** `destroyed` フラグ、**遅延カスケード爆発**（`ScheduleExplosion` で一拍ずつ伝播）、
  **HP pip**（残耐久ドット生成）、**ドロップ抽選**（カテゴリ→重み付き `WeightedPick`）。

- **`BlockSpawner.cs`**（439行・`IFreezable`）
  ブロックの**生成・降下・底判定**。タイマーで行を生成、毎フレーム降下、`blockDeadZoneY` 超えで
  破棄＋ダメージ通知。見どころ: **行スライドイン演出**（コルーチン `SlideInRow`）、妨害行/スペシャル行、
  Dynamic Escalation（経過時間でプロパティ `CurrentSpawnInterval` が縮む）、LINQ の `HardenRandomBlocks`。

- **`PlayerController.cs`**（221行・`IFreezable`）
  パドル。`rb.isKinematic=true` で物理ではなく `transform.localPosition` 直接操作。
  見どころ: 入力は `Keyboard.current.aKey.isPressed`（Input System）、移動可能なのは Playing/Countdown のみ、
  一時効果（幅/速度/入力反転/フラッシュ）は全部コルーチン。

- **`LaunchAimer.cs`**（171行）
  発射照準。`ball.IsWaitingToLaunch` を監視し、sin 波で角度を往復（メトロノーム）、`LineRenderer` で線を描画。
  S/K キーで `ball.LaunchInDirection()`。発射は Playing 限定。

- **`DeadZone.cs`**（47行）
  画面下の落下検知。`OnTriggerEnter` でボールを検知→ `GameManager.OnBallDropped` → リスポーン。
  追加ボール（BURST 等）はペナルティなしで破棄。

- **`ZonePoison.cs`**（63行）/ **`ZoneSlow.cs`**（73行）
  妨害で生成されるエリア。落下→着地→効果（毒は毎秒ダメージ、減速はボールの `slowZoneMul` を書換）。
  見どころ: `OverlapSphereNonAlloc`（毎フレーム判定でも**ゴミ（GC）を出さない**よう事前確保バッファを使う）、
  `OnDestroy` で効果を確実に解除。

### ItemsSkills/（アイテムとスキル — 効果の抽象化が主役）

- **`ItemDrop.cs`**（226行）
  落下中のアイテム本体。`enum ItemType`（全15種）と `ItemDefinition`（static な色/名前/カテゴリ表）もここ。
  落下しながら `OverlapSphere` でパドル接触を検知→ `BuildEffect().Apply()` で効果発動。
  見どころ: `BuildEffect()` の switch が「アイテム種別→ `EffectDefinition` インスタンス」を組み立てる。

- **`EffectDefinition.cs`**（85行）
  効果の**抽象基底**（2.11 で解剖済み）。`EffectBallAttribute`/`EffectHeal`/`EffectAttack`/`EffectInputReverse` 等。

- **`SkillDefinition.cs`**（111行）
  スキルの**抽象基底**＋4実装（`SkillHyper`/`SkillExplosion`/`SkillBurst`/`SkillGiant`）。
  `enum SkillId` もここ。性能差は `EnergyCost`（必要ゲージ量）で表現。全て自己強化系（攻撃送付スキルは無い）。

- **`SkillController.cs`**（84行）
  1人分のエナジー＆スキル。`EnergySystem` を保持、キー（Q/U）で発動、発動後クールダウン（`chargeLockUntil`）。
  プロパティ `EnergyRatio`/`IsReady` で UI に状態を見せる。

### UI/（画面表示 — ほぼ全部 GameManager を読むだけ）

- **`UIManager.cs`**（727行）… HUD の主役。毎フレーム GameManager をポーリングして HP バー/コンボ/スコア/
  スキルアイコン/危険ライン/Last Stand/決着フラッシュ等を更新。`[必須]/[任意]/[演出]` の3区分で未バインドでも安全。
- **`TitleUI.cs`** / **`SettingsUI.cs`** / **`SkillSelectUI.cs`** / **`CountdownUI.cs`** /
  **`RoundResultUI.cs`** / **`RoundDecisionUI.cs`** / **`MatchResultUI.cs`**
  各画面。共通パターン: `Update` で `GetCurrentState()` を見て**自分の担当状態のときだけ** panel を表示し、
  キー入力を処理して GameManager のメソッド（`StartFromTitle`/`ConfirmSettings`/`BeginMatch`/`StartRematch`…）を呼ぶ。

### Visual/（純演出）

- **`BreathPulse.cs`**（71行）… Material の HDR 強度を sin 波で脈動させ Bloom を「呼吸」させる。
- **`BackdropBlur.cs`**（178行）… メニュー中に画面を1枚キャプチャ→ぼかして「磨りガラス」背景にする。

---

## 第5章 イベントを追う — コードが実際にどう動くか

文法と地図が分かったら、最後は**「1つの出来事がコードをどう流れるか」**を追います。これが読めれば一人前です。

### 5.1 起動 → 試合開始までの流れ

```
アプリ起動
  → GameManager.Start()           : 状態=Title, timeScale=0, タイトルBGM
  → [Space] TitleUI               : GameManager.StartFromTitle()  → 状態=Settings
  → [A/D で先取数, Space] SettingsUI: GameManager.ConfirmSettings() → StartSkillSelect()
                                      （HP/スコア/統計をリセット）→ 状態=SkillSelect
  → [両者がカード確定] SkillSelectUI: 各 SkillController.SetSkill() → GameManager.BeginMatch()
  → BeginMatch()                   : アリーナをリセット → BeginCountdown()
  → CountdownCoroutine()           : "3"→"2"→"1"→"GO!"（GO! の瞬間に 状態=Playing, timeScale=1, 試合BGM）
  → 以後 Playing                   : ボール発射可能、ブロック降下開始
```

### 5.2 ボールがブロックを1個壊す瞬間（最重要トレース）

```
① Block.OnCollisionEnter(collision)          ← ボールがぶつかった
     ・collision.gameObject が "BallTag" か確認（違えば無視）
     ・ball.GetDamage() で属性別ダメージ量を取得
     ・ball.GetImpactFrames() で手応えフレーム数を計算
        → GetArena()?.TriggerHitStop(frames, ...)  ← ArenaController 経由でヒットストップ
② Block.TakeDamage(damage, ball)
     ・currentHp -= damage
     ・currentHp <= 0 なら → OnDestroyed(ball)
③ Block.OnDestroyed(ball)
     ・destroyed フラグで「一度だけ」保証（多重破壊ガード）
     ・GameManager.Instance.RegisterBlockDestroyed(ball.playerIndex)  ← コンボ+1, エナジー蓄積, 統計
     ・GameManager.Instance.AddScore(ball.playerIndex, score)         ← コンボ倍率込みでスコア加算
     ・(Explosive なら) spawner.ScheduleExplosion(...)                ← 遅延連鎖爆発を予約
     ・TryDropItem(ball)                                              ← 確率でアイテム生成
     ・Destroy(gameObject)                                            ← ブロック消滅
④ Block.OnCollisionEnter の続き: ball.OnHitBlock(this)
     ・Fire なら周囲に範囲ダメージ / Thunder なら同種連鎖 / Pierce なら貫通維持
```
**ポイント**: ブロックは HP を直接いじらず、必ず `GameManager` のメソッドを呼ぶ（3.2①）。
「ぶつかった瞬間の処理」が `OnCollisionEnter` に集約されているのが Unity 流。

### 5.3 ボールが下に落ちる瞬間

```
DeadZone.OnTriggerEnter(other)                ← ボールが画面下のトリガーに入った
  → GameManager.OnBallDropped(playerIndex)
       ・コンボを 0 にリセット
       ・ApplyDamage(playerIndex, damageBallDrop)  ← 最終窓口。HP=0 なら EndRound()
  → ball.PrepareRespawn(...)                   ← ボールを発射待ち状態に戻す
```

### 5.4 攻撃アイテムを取って相手を妨害する流れ

```
ItemDrop.Update() がパドル接触を検知
  → BuildEffect() = EffectAttack インスタンス
  → EffectAttack.Apply(取得者index, arena)
       ・opponent = 取得者の反対
       ・GameManager.SendInterference(opponent, AttackItem)
            ・被妨害回数を集計
            ・ApplyInterference(相手アリーナ, type)  ← AddRow/Harden/Poison/Slow を実行
            ・相手にヒットストップ＋オーバーレイ表示
            ・攻撃者の HUD に "SENT →" 表示
```
**ポイント**: 効果が `EffectAttack` という**抽象基底の子**なので、`ItemDrop` は中身を知らずに `.Apply()` を
呼ぶだけ（2.11）。攻撃も回復も属性付与も、`ItemDrop` から見れば「`EffectDefinition` を1個 Apply する」だけ。

### 5.5 スキル発動（例: GIANT）

```
SkillController.Update()
  ・Playing 中かつ IsReady（ゲージ満タン）かつ Qキー押下を確認
  → energy.Consume(EnergyCost) でゲージ消費 + クールダウン開始
  → equippedSkill.Activate(playerIndex, arena)
       SkillGiant.Activate:
         ・ball.ClearItemEffects()                       ← アイテム効果を消して純粋なスキル弾に
         ・ball.SetAttributeTemporary(Pierce, duration)  ← 貫通化（コルーチン）
         ・ball.SetScaleTemporary(scaleMul, duration)    ← 巨大化（コルーチン）
```

---

## 第6章 「○○を変えたい」逆引き表

| やりたいこと | 触る場所 |
|---|---|
| ボールの速度・ブロック確率・ダメージ量などの**数値調整** | `ArenaSharedConfig`（シーン上の Inspector）。コードではなくここ |
| ボールの**属性の挙動**（炎の範囲、雷の連鎖など） | `BallScript.OnHitBlock` / `ApplyAreaDamage` |
| **新しいアイテム**を追加 | `ItemType` に追加 → `ItemDefinition` の色/名前/カテゴリ → `ItemDrop.BuildEffect` → 必要なら `EffectDefinition` に新クラス |
| **新しいスキル**を追加 | `SkillId` に追加 → `SkillDefinition` を継承した新クラス → `SkillSelectUI.AllSkills` に登録 |
| **ブロックの種類**を増やす | `BlockType` に追加 → `Block` の色/挙動 → `BlockSpawner` の生成確率 |
| **試合の状態遷移**（画面の流れ） | `GameManager` の `GameState` と各遷移メソッド（`StartFromTitle` 等） |
| **HUD の表示**（HPバー、コンボ等） | `UIManager`（読み取りは GameManager の `GetXxx`） |
| **ヒットストップ/シェイクの強さ** | `ArenaSharedConfig` の `impactBaseFrames` / `shakeIntensityNormal` 等 |
| **効果音/BGM** | `AudioManager`（クリップは Inspector で割り当て） |

---

## 第7章 読解トレーニング — 「読める」を「説明できる」にする

知識編を読むと「読めた気」になりますが、**説明できるかどうかは別の能力**です。
説明は出力（アウトプット）なので、出力する練習をしないと身につきません。この章はその練習です。

### 7.1 コメント書き足し演習（メイン演習）

到達目標「全スクリプトに自分でコメントを書き足せる」を、そのまま演習にします。
**1ファイル 15〜30分**。コピーではなく**本物のファイルに**書き込んでください（git があるのでいつでも消せます）。

手順:

1. 対象ファイルを開き、まず最後まで**ざっと眺める**（分からなくて OK。長さと形を把握するだけ）
2. メソッドごと・ブロックごとに、第2.5章の型で自分の言葉のコメントを書く:
   ```csharp
   // ▶ ぶつかった相手がボールでなければ無視する
   // 🎮 ブロック同士の接触などで誤作動しないための関所
   if (!other.CompareTag("BallTag")) return;
   ```
3. 分からない行は誤魔化さず `// ？ ここが分からない: ...` と正直に書く（これが一番価値のあるコメント）
4. 書き終えたら Claude に**採点だけ**させる（プロンプトは 11.2）。`// ？` の行は答えをもらう
5. 終わったらコメントは `git restore` で消すか、残したければそのまま commit してもいい（自分のリポジトリ）

推奨順（短い・依存が少ない順。1週1〜2ファイルで約2ヶ月のメニュー）:

| # | ファイル | 行数 | ここで身につくこと | 自己チェック質問（口頭で答えられたら合格） |
|---|---|---|---|---|
| 1 | `Systems/IFreezable.cs` | 5 | interface | なぜ中身が書かれていない？ |
| 2 | `Systems/HPSystem.cs` | 52 | クラスの全要素（2.1） | なぜ `currentHP` は private？ |
| 3 | `Gameplay/DeadZone.cs` | 47 | イベント・タグ・null セーフ | なぜ `isExtraBall` を先に判定して return？ |
| 4 | `Gameplay/ZoneSlow.cs` | 73 | Overlap 判定・後始末 | `OnDestroy` の処理が無いと何が残る？ |
| 5 | `Gameplay/PlayerController.cs` | 221 | 入力・状態ガード・一時効果 | なぜ Countdown 中だけ `unscaledDeltaTime`？ |
| 6 | `ItemsSkills/SkillController.cs` | 84 | プロパティ・ロックアウト | `IsReady` が true になる条件は？ |
| 7 | `Gameplay/Block.cs` | 402 | 衝突→破壊の流れ（5.2） | `destroyed` フラグが無いと何が起きる？ |
| 8 | `Gameplay/BlockSpawner.cs` | 439 | コルーチン演出・キュー | `slidingBlocks` は何を防いでいる？ |
| 9 | `Gameplay/BallScript.cs` | 640 | 速度4層・属性 | 実効速度の4つの掛け算を全部言える？ |
| 10 | `Core/GameManager.cs` | 699 | 状態機械・最終窓口 | `ApplyDamage` を1箇所に集める理由は？ |

> 各ファイルの先頭コメントは「そのファイルが何者か」を日本語で説明しています。**まず先頭コメントだけ読む**のも有効。
> 文法に詰まったら第2章の該当節へ。「この記号は何？」を1つずつ潰すのが近道です。

### 7.2 「どこが原因か」当て稽古

実戦（特に VR の実機デバッグ）で一番使う力は、**画面の現象からファイルの当たりをつける**ことです。
当たりのつけ方には型があります:

1. **いつ起きる？** → ライフサイクル表（1.2）で候補メソッドを絞る。
   「ぶつかった瞬間」→ `OnCollisionEnter` / 「毎フレームじわじわ」→ `Update` / 「生成された直後」→ `Awake`/`Start`
2. **誰の責務？** → 第3章の地図で「その現象の持ち主」を探す。パドルの動き→`PlayerController`、HP→`GameManager`…
3. **第6章の逆引き表**で確認

練習問題 — まず**ファイルを開かずに**当たりを言ってから、開いて答え合わせ:

| 現象 | 当たり（答え） |
|---|---|
| Q1. ボールが落ちたのに HP が減らない | `DeadZone.OnTriggerEnter` → `GameManager.OnBallDropped` の経路のどこか |
| Q2. タイトル画面なのにパドルが動いてしまう | `PlayerController.Update` 先頭の状態ガード |
| Q3. コンボが伸びてもボールの色が変わらない | `BallScript.Update` の Ball Heat（`GetHeatColor`） |
| Q4. アイテムを取っても効果が出ない | `ItemDrop.Update` の Overlap 判定 → `BuildEffect().Apply()` |
| Q5. ゲージ満タンなのにスキルが発動しない | `SkillController`（`IsReady` / `chargeLockUntil` / 状態ガード） |
| Q6. 攻撃アイテムを取ったのに相手に妨害が届かない | `EffectAttack.Apply` → `GameManager.SendInterference` → `BlockSpawner.ReceiveSabotageRow` |

「ファイル名」だけでなく「**どの行が原因になり得るか**」まで言えたら満点です。
自分で問題を増やしたければ、Claude にクイズを出させるプロンプトが 11.2 にあります。

---

## 第8章 デバッグ道場 — エラーと向き合う

VR 実機では「Claude に状況を伝える」こと自体が難しい。**エラーを自分で読み、自分で切り分ける力**がここで要ります。

### 8.1 エラー文の解剖 — 赤い文字には住所が書いてある

Console の赤いエラーは、**「何が起きたか」と「どこで起きたか」の2行**さえ読めれば十分です。

```
NullReferenceException: Object reference not set to an instance of an object
Block.OnCollisionEnter (UnityEngine.Collision collision) (at Assets/Scripts/Gameplay/Block.cs:152)
```

- **1行目 = 何が起きたか**。`NullReferenceException` =「null（空っぽ）のものに `.` で触ろうとした」
- **2行目以降 = どこで起きたか**（スタックトレース）。`クラス.メソッド (at ファイル:行番号)` の形。
  **Console でこの行をダブルクリックすると、エディタのその行に飛べます**
- 複数行ある場合、**一番上が「実際に落ちた場所」**、下へ行くほど「そこを呼び出した側」。
  まず一番上にある**自分のスクリプト**（`Assets/Scripts/` で始まる行）を探す

読み解きの型: 上の例なら「`Block.cs` の 152 行目で、null の何かに `.` した」→ 152行目を開き、
**`.` の左側にある変数**を疑う（`ball.GetDamage()` なら「`ball` が null だった」）。

### 8.2 頻出エラー図鑑（このゲームで実際に出るもの）

| エラー | 意味 | このゲームでありがちな原因 | 最初に見ること |
|---|---|---|---|
| `NullReferenceException` | null に `.` した | `GetComponent` が見つからない / Inspector 未バインド / `Instance` がまだ無い | 落ちた行の `.` の左の変数 |
| `UnassignedReferenceException` | SerializeField が Inspector で未設定 | `blockPrefab` 等の繋ぎ忘れ | エラー文に**変数名が書いてある** → Inspector を見る |
| `MissingReferenceException` | **Destroy 済み**のものに触った | 破壊されたブロック/ボールへの参照が残っていた | そのオブジェクトが「いつ Destroy されるか」を遡る |
| `IndexOutOfRangeException` | 配列の範囲外（5個しかないのに `[7]`） | `skillIconsReady[]` 等のサイズ不足 | 添字に入る値と配列の Size |
| `CS〜`（コンパイルエラー） | 文法ミス。**直すまで再生ボタンが押せない** | `;` 忘れ・括弧の対応・大文字小文字 | **一番上のエラーから**直す（連鎖して下に偽エラーが出るため） |

> **コンパイルエラーと実行時エラーの違い**（超重要）:
> CS エラーは「文として成立していない＝ゲームを始められない」。例外（Exception）は「動いている途中で事故った」。
> Console が赤くて再生できないときは前者。プレイ中に赤い行が流れるのは後者。対処の入口が違います。

### 8.3 `Debug.Log` — 3つの使い方

```csharp
// ① 到達確認 — 「そもそもこの行まで来てるか？」
Debug.Log("[ZoneSlow] OverlapZone called");

// ② 値の確認 — $"..."（文字列補間）で {変数} がその時の実際の値に展開される
Debug.Log($"[ZoneSlow] hits={hitCount} slowFactor={slowFactor}");

// ③ 主の確認 — 第2引数に this を渡すと、Console でクリックした時 Hierarchy 上の発信元が光る
Debug.Log($"[Ball] effectiveSpeed={EffectiveSpeed()}", this);
```

コツ:

- 先頭に `[タグ]` を付ける（Console の検索欄で絞り込める。左右どちらのアリーナか分からない時は ③ も併用）
- `Update` 内に置くと**毎秒約60回**出る。確認したら**すぐ消す**こと。消し忘れ防止に必ず `[タグ]` を付けておく
- 仕込む場所は 2.5.3 の「**②世界への反映行**」の直前が効率的（「反映される直前の値」が見えるから）

### 8.4 「動かない」を潰す4つの質問（仮説→検証ループの型）

「動かない」は曖昧すぎてデバッグできません。**次の4つに分解**します:

1. **そもそも呼ばれてる？** → メソッド先頭に到達確認 Log（①）。
   出ないなら原因は呼び出し側か発火条件（タグ・Collider・`if (state != Playing) return;` の状態ガード）
2. **値は期待どおり？** → 値確認 Log（②）。「null だった」「0 だった」が見えた時点で原因の半分は判明
3. **反映行まで届いてる？** → 途中の `if` で return されていないか。反映行（2.5.3 ②）の直前に Log
4. **コードじゃなく Inspector では？** → SerializeField は**シーンの値が優先**（2.7）。
   コードを直したのに変わらない時は、まずこれを疑う

実例 — 「ZoneSlow に入ったのにボールが遅くならない」:

```
仮説A: そもそも OverlapSphere がボールを拾っていない
  → Update に Debug.Log($"hits={hitCount}") → ずっと 0 なら半径・位置・Collider を疑う
仮説B: 拾っているが slowZoneMul が書き込まれていない
  → ball.slowZoneMul = slowFactor; の直後に Log
仮説C: 書き込まれているが BallScript 側が読んでいない
  → BallScript.EffectiveSpeed() 側に Log
```

**1回に1仮説**。Log の結果で仮説を棄却して次へ。これが「動かない → 仮説 → 試す → 確かめる」ループの実物です。

### 8.5 デバッグ稽古 — Claude にバグを仕込ませる

本物のバグは、練習したい時に都合よく出てくれません。**わざと壊してもらう**のが最速の練習法です:

1. 安全な作業場を作る: `git switch -c debug-dojo`
2. Claude Code に頼む:
   ```
   デバッグ練習をしたい。Assets/Scripts/Gameplay/ のどれか1ファイルに、
   実行時に症状が出るバグを1個だけ仕込んで。どこを変えたかは絶対に言わないで。
   症状のヒントだけ1行で教えて。
   ```
3. Unity を再生して症状を確認 → **8.4 の4つの質問**で自力で特定 → 直す
4. 答え合わせ: `git diff` で Claude が変えた場所を見る（自分の見立て・修正と比べる）
5. 後片付け: `git switch main` → `git branch -D debug-dojo`

レベル調整（Claude への頼み方を変えるだけ）:

- **易**: 「Console にエラーが出るバグにして」（8.1〜8.2 の練習）
- **中**: 「エラーは出ないが挙動がおかしくなるバグにして」（8.3〜8.4 の練習）
- **難**: 「2個仕込んで」（実戦は複合バグ）

> 週1回 15分のこの稽古が、6つの力の中で一番「Claude 依存から抜ける」効果が大きい練習です。

---

## 第9章 改造ドリル — 壊さず変える・足す

ゼロから書く力より、**既存コードを目的どおりに変える力**が実戦では要ります。これも型があります。

### 9.1 改造の型 — 5ステップ

1. **目的を一文に**する: 「パドルをもっと速くしたい」
2. **当たりをつける**: 第6章の逆引き表 → 無ければ 7.2 の型（いつ？誰の責務？）
3. **読んでから触る**: 変更する行の前後を理解する。**理解せず変えるのが「壊す」の最大の原因**
4. **最小の変更**: 1回に1箇所。変えたら**すぐ再生して確認**。まとめて変えない
5. **戻せる状態を保つ**: `git diff` で自分が何を変えたか確認 / `git restore <ファイル>` で1ファイルだけ元通り / 動いたら commit

> git は「失敗しても無傷で戻れる」装置です。**改造の勇気は git から出ます**。
> 変更を始める前に `git status` がクリーンか確認する癖をつけると、diff が自分の変更だけになって読みやすい。

### 9.2 演習（Lv1 → Lv3）

ヒントを見ずに 9.1 の型だけでやるのが理想。詰まったらヒント → それでも詰まったら Claude に「ヒントだけ」頼む（11.2）。

#### Lv1 — 数値を変える（コードを書かない改造）

- **演習 1-1: パドルの速度を2倍にする（罠つき）**
  まず `ArenaSharedConfig.cs` の `paddleSpeed = 10f` を `20f` に書き換えて再生してください。
  → **おそらく変わりません**。なぜ？ → コードの `= 10f` は初期値で、**シーンの Inspector 値が優先**される（2.7）。
  正解はシーン上の ArenaSharedConfig オブジェクトの Inspector。
  この「コード初期値 ≠ シーンの実値」を**一度体で踏んでおく**のがこの演習の目的です（8.4 の質問④の正体）。
- **演習 1-2**: Heavy ボールが「遅くならない」ようにする。
  ヒント: `heavySpeedFactor` 0.7 → 1.0。確認: Heavy アイテム取得時に速度が落ちないこと
- **演習 1-3**: アイテムブロックを出やすくする。ヒント: `itemBlockChance` 0.08 → 0.3。確認: 紫っぽい Item ブロックの出現頻度

#### Lv2 — 1〜3行の挙動変更

- **演習 2-1**: BURST の追加ボールも、落ちたら HP が減るようにする。
  ヒント: `DeadZone.OnTriggerEnter` の `isExtraBall` 分岐。確認: BURST 発動 → 弾が落ちるたび HP 減
- **演習 2-2**: P2 の操作キーを J/L から ←/→ に変える。
  ヒント: `PlayerController.Update` の P2 分岐。**P1 も矢印キーを使っている**ことに気づけるか？（変更が他とぶつかる例）
- **演習 2-3**: Absorb ブロックを「減速」ではなく「加速」にする。
  ヒント: `Block.OnCollisionEnter` の `absorbSpeedMultiplier`。その値が**どこで設定されるか**を探すのも演習のうち
- 終わったら `git restore .` で全部戻す（気に入った変更は commit してもいい）

#### Lv3 — 小さな機能追加（逆引き表をフル活用）

- **演習 3-1: 新アイテム「FREEZE（相手のパドルを2秒止める）」を追加する**
  手順は第6章「新しいアイテムを追加」の通り:
  `ItemType` に追加 → `ItemDefinition` の色/名前 → `ItemDrop.BuildEffect` → `EffectDefinition` に新クラス。
  設計のヒント: 「パドルを止める」仕組みは**既にある**（`IFreezable` / `PlayerController.Freeze()`）。
  新しいコードはほぼ「既存部品の組み合わせ」で書けます。既存の `EffectAttack`／`EffectInputReverse` を写し見しながら。
  **先に第10章の設計文を書いてから**実装すると、第10章の演習も兼ねられます。
- **演習 3-2**: コンボ10以上の間だけパドルが少し速くなる。
  ヒント: コンボは `GameManager.GetCombo(playerIndex)` で読める。誰が・いつ読むのが素直か？
  （`PlayerController.Update` で毎フレーム読むのが一番簡単。ポーリング型 UI と同じ発想 = 3.2 ⑤）

### 9.3 壊さないための習慣（まとめ）

- 変更前: `git status` がクリーンか確認
- **1変更 → 1再生 → 1確認**。Console が赤い（CS エラー）間は再生できない → **最後に変えた場所**をまず疑う
- ダメなら `git restore .` で全戻し。「とりあえず全部戻して仕切り直す」が一番安全で速い

---

## 第10章 設計を日本語で書く

実装の前に「どのオブジェクトに、いつ、何をさせるか」を言葉にできれば、実装は調べながらでも書けます。
チーム開発（VR 部活）では、**この言語化そのものが指示書・議論の土台**になります。

### 10.1 設計文のテンプレート

```
【どこに】  どの GameObject / どのスクリプトに
【いつ】    どのタイミングで（毎フレーム / 衝突時 / 状態が X のとき / N 秒後）
【何を】    何をさせる（値を変える / 生成する / 消す / 通知する）
【値は】    調整したい数値をどこに置く（ArenaSharedConfig / SerializeField）
【後始末】  いつ・どうやって元に戻す（ラウンドリセットでどうなる？）
```

5行目の**【後始末】が書けるかどうかが、初心者とそれ以外の分かれ目**です。
このゲームの `ResetForNewRound` / `OnDestroy` / `StopCoroutine` は全部「後始末」の実装です。

書いた設計文へのチェックリスト:

- [ ] 【いつ】がライフサイクルの言葉（`Update` / `OnCollisionEnter` / コルーチン…）に翻訳できるか（1.2）
- [ ] `Playing` 以外の状態でも発動していいか？（状態ガードは要るか）
- [ ] ラウンドが切り替わった瞬間、この効果はどうなるべきか
- [ ] 自分のアリーナと相手のアリーナ、どちらの話か
- [ ] 参照が null（未バインド・未生成）だったらどうするか（2.8）

### 10.2 逆演習 — 既存機能を設計文に「逆翻訳」する

実装を読んで設計文に戻す練習。読解（第7章）と設計（この章）を同時に鍛えられます。お手本として ZoneSlow:

```
【どこに】  ZoneSlow（実行時に生成される GameObject、ZoneSlow.cs を貼る）
【いつ】    AttackSlow 妨害を受けた時に生成。落下して着地後、duration 秒のあいだ毎フレーム
【何を】    自分の範囲内にいるボールの slowZoneMul に slowFactor(0.5) を書き込む
            （実際の減速はボール自身が速度計算で読むことで起きる = 速度4層の1層）
【値は】    slowFactor と duration は ArenaSharedConfig（両アリーナ共通だから）
【後始末】  毎フレーム先頭で前フレーム分を 1 に戻す（→ボールが離脱したことを自動検出できる）。
            duration 後に自動 Destroy。OnDestroy でも 1 に戻す（ラウンドリセットの即時破棄対策）
```

練習: **`ZonePoison` / `LaunchAimer` / Ball Heat** を同じ形式で書く
→ 書けたら Claude に「実装と食い違っている箇所」だけ指摘させる（11.2）。

### 10.3 順演習 — 設計文を書いてから作る

1. 作りたい機能（9.2 Lv3 の FREEZE アイテム等）を上のテンプレートで書く。**コードは1行も書かない**
2. Claude に設計文だけ見せてレビューさせる:
   ```
   実装はまだしないで。この設計文の穴（後始末・状態ガード・null・どちらのアリーナか）だけ指摘して。
   ```
3. 直した設計文をもとに実装する（自分で書く / Claude と分担、どちらでも。**設計者が自分**であることが重要）
4. 完成後、設計文と実際の実装の**差分を振り返る**。設計時に見えていなかったものが、次の設計から見えるようになります

---

## 第11章 Claude Code を「先生」にする

Claude Code は「代わりに書く道具」としてだけ使うと、速いが何も残りません。
**採点者・出題者・スパーリング相手**として使うと、上の全章の練習効率が上がります。

### 11.1 3つの原則

1. **先に自分の仮説を言う**: 「○○が原因だと思うけど合ってる？」と聞く。丸投げ（「直して」）との学習効果の差は歴然
2. **書かせたら説明させる**: Claude が書いたコードは `▶`（何が書いてあるか）/`🎮`（何を動かすか）形式で
   1行ずつ説明させてから受け入れる。説明を読んでも分からない行が残るなら、それはまだ「自分のコード」ではない
3. **答えではなくヒントを頼む**: 「答えは言わないで」が言えるようになると、学習速度が一番上がる

### 11.2 コピペで使えるプロンプト集

**行解説**（いつでも・第2章の補講）:
```
Block.cs の152行目の `?.` は何？ ▶（何が書いてあるか）と 🎮（ゲームの何を動かすか）の2段で説明して。
```

**コメント採点**（7.1 用）:
```
PlayerController.cs に自分で ▶/🎮 コメントを書いた。間違っている・ズレているコメントだけ指摘して。
合っているものは触れなくていい。「// ？」と書いた行には答えを教えて。
```

**クイズ出題**（7.2 用）:
```
BallScript.cs から読解クイズを3問出して。「この行を消すと何が起きる？」形式で。
答えはすぐ言わず、私が答えてから採点して。
```

**ヒント限定デバッグ**（第8章用）:
```
このエラーを自分で直したい。答えとファイル名は言わないで。
まず「何が null になり得るか」のヒントを1個だけちょうだい。
```

**バグ仕込み**（8.5 用）: → 8.5 に記載

**設計レビュー**（10.3 用）: → 10.3 に記載

**逆翻訳の答え合わせ**（10.2 用）:
```
この設計文は ZonePoison.cs の実装を私が言葉にしたもの。実装と食い違っている箇所だけ指摘して。
```

### 11.3 「自分でやる / Claude に任せる」の線引き

| 自分でやる（力になる） | Claude に任せてよい（時間の節約） |
|---|---|
| 現象からファイルの当たりをつける（7.2） | その当たりが合っているかの確認 |
| `Debug.Log` を**どこに**仕込むか決める（8.3） | Log の出力結果を見せての壁打ち |
| 数値調整・1〜3行の改造（9.2 Lv1-2） | 定型コードの大量生成（UI バインド・ボイラープレート） |
| 設計文を書く（10.1） | 設計文の穴のレビュー |
| エラーの1行目と場所を読む（8.1） | 知らない API・Unity 機能の調査 |
| 最初の仮説を立てる（8.4） | 仮説が外れた後の「次の仮説」の壁打ち |

> 目安: **「どこを見ればいいか」を自分で言えるなら、その先を Claude に任せても依存ではない**。
> VR 実機のように Claude に状況を渡せない場面でも、「どこを見るか」さえ自分にあれば詰みません。

### 11.4 おすすめの回し方（週 30 分から）

- **週1セット**: 7.1 を1ファイル（15分）＋ 8.5 のバグ稽古を1本（15分）
- 7.1 が10ファイル終わったら、9.2 の Lv2〜Lv3 と 10.2 の逆翻訳に切り替える
- ゲームの調整作業（バランス・演出）そのものが 9.2 Lv1 の実戦なので、開発を進めること自体が練習になります

> 分からない箇所が出たら、ファイル名・行番号・「ここの `?.` は何？」のように具体的に聞いてください。
> その行を起点に、必要な文法だけを深掘りして説明します。
