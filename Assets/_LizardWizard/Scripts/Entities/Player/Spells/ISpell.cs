using UnityEngine;

public interface ISpell
{
    int GetElement();
    void SetElement(int setTo);
    void CastSpellDown();
    void CastSpellReleased();
    void UpgradeSpell();
}
