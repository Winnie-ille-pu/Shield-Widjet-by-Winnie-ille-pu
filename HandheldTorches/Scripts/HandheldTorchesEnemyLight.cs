using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallConnect;

public class HandheldTorchesEnemyLight : IncumbentEffect
{
    public static readonly string EffectKey = "HandheldTorchesEnemyLight";
    Light Light = null;
    HandheldTorchesEnemyParticleEmitter Emitter;

    public override void SetProperties()
    {
        properties.Key = EffectKey;
        properties.SupportDuration = true;
        properties.AllowedTargets = EntityEffectBroker.TargetFlags_All;
        properties.AllowedCraftingStations = MagicCraftingStations.None;
        properties.AllowedElements = EntityEffectBroker.ElementFlags_All;
        properties.MagicSkill = DFCareer.MagicSkills.Illusion;
        properties.DurationCosts = MakeEffectCosts(8, 40);
        properties.DisableReflectiveEnumeration = true;
    }

    public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
    {
        base.Start(manager, caster);
        StartLight();
    }

    public override void Resume(EntityEffectManager.EffectSaveData_v1 effectData, EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
    {
        base.Resume(effectData, manager, caster);
        StartLight();
    }

    public override void End()
    {
        base.End();
        EndLight();
    }

    protected override bool IsLikeKind(IncumbentEffect other)
    {
        return other is HandheldTorchesEnemyLight;
    }

    protected override void AddState(IncumbentEffect incumbent)
    {
        // Stack my rounds onto incumbent
        incumbent.RoundsRemaining += RoundsRemaining;
    }

    void StartLight()
    {
        Debug.Log("Handheld Torches - ENEMY LIGHT STARTED");
        // Do nothing if light already started
        if (Light)
            return;

        if (caster == null)
        {
            RoundsRemaining = 0;
            return;
        }

        // Create the light object
        GameObject go = new GameObject(EffectKey);
        go.transform.parent = caster.transform;
        go.transform.localPosition = (Vector3.back * 0.4f) + (Vector3.up * 0.6f);

        Light playerLightSource = HandheldTorches.Instance.playerLightSource;

        Light = go.AddComponent<Light>();
        Light.type = playerLightSource.type;
        Light.color = playerLightSource.color;
        Light.range = playerLightSource.range;
        Light.shadows = (DaggerfallUnity.Settings.EnableSpellShadows && HandheldTorches.Instance.fireLightShadows) ? LightShadows.Soft : LightShadows.None;

        Emitter = go.AddComponent<HandheldTorchesEnemyParticleEmitter>();
        Emitter.color = playerLightSource.color;

    }

    void EndLight()
    {
        // Destroy the light gameobject when done
        if (Light)
            GameObject.Destroy(Light.gameObject);
    }


} //class EnemyMageLightMU
