using UnityEngine;

public class EnergySystem
{
    private float energy;
    private float maxEnergy;

    public float Energy    => energy;
    public float MaxEnergy => maxEnergy;
    public float Ratio     => maxEnergy > 0f ? energy / maxEnergy : 0f;
    public bool  IsFull    => energy >= maxEnergy;

    public EnergySystem(float max) { maxEnergy = Mathf.Max(1f, max); }

    public void AddEnergy(float amount) { energy = Mathf.Min(maxEnergy, energy + amount); }
    public void ConsumeAll()            { energy = 0f; }
    public void Reset()                 { energy = 0f; }
    public void SetMax(float max)       { maxEnergy = Mathf.Max(1f, max); }
}
