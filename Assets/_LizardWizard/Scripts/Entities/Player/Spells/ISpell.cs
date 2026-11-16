using UnityEngine;

public interface ISpell
{
    public enum Elements
    {
        None,
        Water,
        Ice,
        Steam,
    }
    Elements GetElement();
    void SetElement(Elements setTo);
    void CastSpellDown();
    void CastSpellTick(float dt);
    void CastSpellReleased();
    void UpgradeSpell();
    bool IsOnCooldown();
}
