using UnityEngine;
using UnityEngine.InputSystem;

// 1プレイヤーのスキルとエナジーゲージを管理する
// ArenaController.Awake() で自動生成・初期化される
public class SkillController : MonoBehaviour
{
    [SerializeField] private float maxEnergy = 10f; // ゲージ満タンに必要なエナジー量

    private int             playerIndex;
    private ArenaController arena;
    private EnergySystem    energy;
    private SkillDefinition equippedSkill;

    // ゲージ表示は「装備スキルの必要量に対する充填率」（DESIGN.md 5.6）。安いスキルほど早く満タンになる。
    public float  EnergyRatio => (energy != null && equippedSkill != null && equippedSkill.EnergyCost > 0f)
                                 ? Mathf.Clamp01(energy.Energy / equippedSkill.EnergyCost) : 0f;
    public string SkillName   => equippedSkill?.DisplayName ?? "---";

    // 装備スキルが必要ゲージに達しているか（発動可能・READY 表示判定）
    public bool   IsReady     => energy != null && equippedSkill != null
                                 && energy.Energy >= equippedSkill.EnergyCost;

    public void Initialize(int pIndex, ArenaController a)
    {
        playerIndex = pIndex;
        arena       = a;
        var c = ArenaSharedConfig.Instance; // 共有設定があれば maxEnergy を共通化（null セーフ）
        if (c != null) maxEnergy = c.maxEnergy;
        energy      = new EnergySystem(maxEnergy);
    }

    public void SetSkill(SkillDefinition skill) => equippedSkill = skill;
    public void AddEnergy(float amount)         => energy?.AddEnergy(amount);

    public void ResetEnergy()
    {
        energy?.SetMax(maxEnergy);
        energy?.Reset();
        wasReady = false;
    }

    private bool wasReady;

    void Update()
    {
        if (GameManager.Instance?.GetCurrentState() != GameManager.GameState.Playing) return;
        if (equippedSkill == null || energy == null) return;

        // チャージ完了（必要ゲージ到達）の立ち上がりで READY SE（DESIGN.md 10.4）
        bool ready = IsReady;
        if (ready && !wasReady) AudioManager.Instance?.PlaySkillReady(playerIndex);
        wasReady = ready;

        if (!ready) return;
        if (!IsSkillKeyPressed()) return;
        if (!equippedSkill.CanActivate(playerIndex)) return;

        AudioManager.Instance?.PlaySkillActivate(playerIndex); // スキル発動 SE
        energy.Consume(equippedSkill.EnergyCost);
        equippedSkill.Activate(playerIndex, arena);
    }

    private bool IsSkillKeyPressed()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return playerIndex == 1 ? kb.qKey.wasPressedThisFrame : kb.uKey.wasPressedThisFrame;
    }
}
