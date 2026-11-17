using System;
using UnityEngine;
using static DebugHandler;

[CreateAssetMenu(fileName = "SpellData", menuName = "Scriptable Objects/SpellData")]
public class SpellData : AttackData, ISpell
{//This will be used for ALL spells!
//implements the following behaviours:
//Holding an amount of 'power' that shrinks over time
    [SerializeField] private float cost;
    [SerializeField] private ISpell.Elements element;
    [Tooltip("Shows up in the spell slot on the hud")] public Sprite icon;
    [NonSerialized] public float chargeTime;
    [LabelOverride("Charge Time"), Min(1f)] public float maxChargeTime = 1f;
    [Header("Movement")]
    [LabelOverride("Is Movement Spell?")] public bool moveSpell = false;
    public Vector3 velocity;
    [Header("Prefab")]
    [Tooltip("Attach the desired prefab for the spell projectile/VFX")] public GameObject spellObj;
    public Vector3 objVelocity;
    [LabelOverride("Decelerates?")] public bool decel;
    [NonSerialized] public GameObject root;
    [NonSerialized] public Entity rootData;
    [NonSerialized] public Transform firePosition;
    private bool charging;
    private float cooldownRemaining;

    public bool IsCharging() => charging;
    public bool IsOnCooldown() => cooldownRemaining > 0f;
    public float CooldownPercent() => cooldown > 0f ? cooldownRemaining / cooldown : 0f; //-- 1 right after casting, 0 when ready

    public ISpell.Elements GetElement()
    {
        return element;
    }
    public bool CanActivate()
    {
        return !IsOnCooldown() && rootData.water.GetCurrent() > cost;
    }
    public void SetElement(ISpell.Elements elmnt)
    {
        element = elmnt;
    }
    public void CastSpellDown()
    {
        if (!CanActivate() || charging) return;
        chargeTime = 0f;
        charging = true;
        rootData.water.Set(rootData.water.GetCurrent() - cost);
    }
    public void CastSpellTick(float dt)
    {
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
        if (!charging) return;

        chargeTime = Mathf.Min(chargeTime + dt, maxChargeTime);
        if (chargeTime >= maxChargeTime) CastSpellReleased();
    }
    public void CastSpellReleased()
    {
        if (!CanActivate() || !charging) return;
        charging = false;
        cooldownRemaining = cooldown; //-- start cooldown
        float chargePercent = Mathf.Clamp01(chargeTime / maxChargeTime);
        var player = root.GetComponent<Player>();

        var lookDir = rootData.GetLookDir();
        if (player != null)
        {
            lookDir = player.GetLookDir(); //optimizes to use the camera
        }
        Vector3 forward = lookDir.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (!VerifyVector(right)) right = rootData.visualRoot.right;
        right.Normalize();

        #region Movement
            if (moveSpell)
            {
                var velocity = this.velocity; //Set here to prevent data overwriting
                if (rootData.GetStateType() == 'A') velocity *= 0.25f; //The lack of friction in the air requires a dampener


                rootData.momentum = (
                    right * velocity.x +
                    Vector3.up * velocity.y +
                    forward * velocity.z
                ); //+ _% * chargePercent
            }
        #endregion
        if (spellObj != null)
        {
            Quaternion rotation = Quaternion.LookRotation(forward);

            var projObj = Instantiate(spellObj, firePosition.position, rotation);
            var projectile = projObj.GetComponent<Projectiles>();

            projectile.data = Instantiate(this);
            projectile.root = root;
            projectile.velocity = rotation * objVelocity;
            projectile.lifeTime = cooldown;

            if (decel) projectile.decel = range;

            if (AudioManager.instance != null) AudioManager.instance.PlaySFX("WaterShot"); //-- cast sound
        }
    }
    public void UpgradeSpell() {}
}
