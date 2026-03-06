/// <summary>
/// DamageInfo — shared data struct passed between combat systems.
/// Keeps damage calculation consistent and easy to extend (e.g. add elemental types).
/// </summary>
[System.Serializable]
public struct DamageInfo
{
    public float  Amount;
    public bool   IsBlockable;
    public bool   IsKi;           // true = ki/laser damage
    public bool   IsUnblockable;  // berserker slam etc.
    public bool   Critical;       // extra juice
    public UnityEngine.Transform Source;

    public DamageInfo(float amount, UnityEngine.Transform source,
                      bool blockable = true, bool isKi = false, bool unblockable = false, bool critical = false)
    {
        Amount       = amount;
        Source       = source;
        IsBlockable  = blockable;
        IsKi         = isKi;
        IsUnblockable = unblockable;
        Critical      = critical;
    }

    public static implicit operator float(DamageInfo d) => d.Amount;
}
