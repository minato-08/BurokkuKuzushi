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

    public float  EnergyRatio => energy?.Ratio ?? 0f;
    public string SkillName   => equippedSkill?.DisplayName ?? "---";

    public void Initialize(int pIndex, ArenaController a)
    {
        playerIndex = pIndex;
        arena       = a;
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

        // チャージ完了の立ち上がりで READY SE（DESIGN.md 10.4）
        bool ready = energy.IsFull;
        if (ready && !wasReady) AudioManager.Instance?.PlaySkillReady(playerIndex);
        wasReady = ready;

        if (!ready) return;
        if (!IsSkillKeyPressed()) return;
        if (!equippedSkill.CanActivate(playerIndex)) return;

        AudioManager.Instance?.PlaySkillActivate(playerIndex); // スキル発動 SE
        energy.ConsumeAll();
        equippedSkill.Activate(playerIndex, arena);
    }

    private bool IsSkillKeyPressed()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return playerIndex == 1 ? kb.qKey.wasPressedThisFrame : kb.uKey.wasPressedThisFrame;
    }
}
