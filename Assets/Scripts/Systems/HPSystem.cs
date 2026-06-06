using UnityEngine;

// プレイヤーのHPを管理するクラス
// MonoBehaviour ではなく純粋なC#クラス（GameManager がインスタンスを保持）
public class HPSystem
{
    private int currentHP;
    private int maxHP;

    public int   CurrentHP => currentHP;
    public int   MaxHP     => maxHP;
    public float Ratio     => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public bool  IsAlive   => currentHP > 0;

    public HPSystem(int maxHP)
    {
        this.maxHP    = maxHP;
        this.currentHP = maxHP;
    }

    // ダメージ適用。0未満にはならない
    public int TakeDamage(int amount)
    {
        if (amount <= 0) return 0;
        int before = currentHP;
        currentHP = Mathf.Max(0, currentHP - amount);
        return before - currentHP; // 実際に減った量
    }

    // 回復。maxHPを超えない
    public int Heal(int amount)
    {
        if (amount <= 0) return 0;
        int before = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        return currentHP - before;
    }

    // ラウンド開始時のリセット
    public void Reset()
    {
        currentHP = maxHP;
    }

    // maxHP を変更する（バランス調整の動的変更用）
    public void SetMaxHP(int newMaxHP, bool refill = false)
    {
        maxHP = Mathf.Max(1, newMaxHP);
        if (refill || currentHP > maxHP)
            currentHP = maxHP;
    }
}
