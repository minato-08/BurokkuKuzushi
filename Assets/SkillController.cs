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
    private bool            forceCatchActive;

    public float  EnergyRatio       => energy?.Ratio ?? 0f;
    public bool   IsForceCatchActive => forceCatchActive;
    public string SkillName         => equippedSkill?.DisplayName ?? "---";

    public void Initialize(int pIndex, ArenaController a)
    {
        playerIndex = pIndex;
        arena       = a;
        energy      = new EnergySystem(maxEnergy);
    }

    public void SetSkill(SkillDefinition skill) => equippedSkill = skill;
    public void AddEnergy(float amount)         => energy?.AddEnergy(amount);
    public void SetForceCatch(bool active)      => forceCatchActive = active;
    public void ConsumeForceCatch()             => forceCatchActive = false;

    public void ResetEnergy()
    {
        energy?.SetMax(maxEnergy);
        energy?.Reset();
    }

    void Update()
    {
        if (GameManager.Instance?.GetCurrentState() != GameManager.GameState.Playing) return;
        if (equippedSkill == null || energy == null || !energy.IsFull) return;
        if (!IsSkillKeyPressed()) return;
        if (!equippedSkill.CanActivate(playerIndex)) return;

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
