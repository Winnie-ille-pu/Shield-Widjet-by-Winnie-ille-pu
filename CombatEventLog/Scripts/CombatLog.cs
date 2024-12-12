using System;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallConnect;

public class CombatLog : MonoBehaviour
{
    static Mod mod;

    [Invoke(StateManager.StateTypes.Start, 0)]

    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        go.AddComponent<CombatLog>();
    }

    void Awake()
    {
        ModCompatibilityChecking();

        DaggerfallUnity.Settings.DisableEnemyDeathAlert = true;

        mod.IsReady = true;
    }

    private void ModCompatibilityChecking()
    {
        //listen to Combat Event Handler for attacks
        Mod ceh = ModManager.Instance.GetModFromGUID("fb086c76-38e7-4d83-91dc-f29e6f1bb17e");
        if (ceh != null)
        {
            ModManager.Instance.SendModMessage(ceh.Title, "onAttackDamageCalculated", (Action<DaggerfallEntity, DaggerfallEntity, DaggerfallUnityItem, int, int>)OnAttackDamageCalculated);
            ModManager.Instance.SendModMessage(ceh.Title, "onSavingThrow", (Action<DFCareer.Elements, DFCareer.EffectFlags, DaggerfallEntity, int>)OnSavingThrow);
        }
    }

    public void OnAttackDamageCalculated(DaggerfallEntity attacker, DaggerfallEntity target, DaggerfallUnityItem weapon, int bodyPart, int damage)
    {
        if (attacker == GameManager.Instance.PlayerEntity)
        {
            if (target != null)
            {
                if (damage > 0)
                    PlayerAttackHitMessage(target, damage);
                else
                    PlayerAttackMissMessage(target);
            }
        }
        else if (target == GameManager.Instance.PlayerEntity)
        {
            if (damage > 0)
                PlayerTargetHitMessage(attacker, damage);
            else
                PlayerTargetMissMessage(attacker);
        }
    }

    public void OnSavingThrow(DFCareer.Elements elementType, DFCareer.EffectFlags effectFlags, DaggerfallEntity target, int result)
    {
        if (target == GameManager.Instance.PlayerEntity)
        {
            if (result == 0)
                PlayerSaveSuccessMessage(effectFlags);
            else
                PlayerSaveFailMessage(effectFlags);
        }
        else
        {
            if (result == 0)
                EnemySaveSuccessMessage(effectFlags, target);
            else
                EnemySaveFailMessage(effectFlags, target);
        }
    }

    void PlayerSaveSuccessMessage(DFCareer.EffectFlags effectFlags)
    {
        DaggerfallUI.Instance.PopupMessage("You've successfully saved against " + effectFlags.ToString() + "!");
    }

    void PlayerSaveFailMessage(DFCareer.EffectFlags effectFlags)
    {
        DaggerfallUI.Instance.PopupMessage("You've failed to save against " + effectFlags.ToString() + "!");
    }

    void EnemySaveSuccessMessage(DFCareer.EffectFlags effectFlags, DaggerfallEntity target)
    {
        string targetName = target.Name;
        EnemyEntity enemy = target as EnemyEntity;
        if (enemy != null)
            targetName = TextManager.Instance.GetLocalizedEnemyName(enemy.MobileEnemy.ID);

        DaggerfallUI.Instance.PopupMessage(targetName + " successfully saved against " + effectFlags.ToString() + "!");
    }

    void EnemySaveFailMessage(DFCareer.EffectFlags effectFlags, DaggerfallEntity target)
    {
        string targetName = target.Name;
        EnemyEntity enemy = target as EnemyEntity;
        if (enemy != null)
            targetName = TextManager.Instance.GetLocalizedEnemyName(enemy.MobileEnemy.ID);

        DaggerfallUI.Instance.PopupMessage(targetName + " failed to save against " + effectFlags.ToString() + "!");
    }

    void PrintMessage(string[] messages1, string[] messages2, string target)
    {
        int index = UnityEngine.Random.Range(0, messages1.Length);

        string message = messages1[index] + target + messages2[index];

        DaggerfallUI.Instance.PopupMessage(message);
    }

    void PlayerAttackHitMessage(DaggerfallEntity target, int damage)
    {
        int index = UnityEngine.Random.Range(0, 5);
        string message = "";

        string targetName = target.Name;
        EnemyEntity enemy = target as EnemyEntity;
        if (enemy != null)
            targetName = TextManager.Instance.GetLocalizedEnemyName(enemy.MobileEnemy.ID);

        bool killed = target.CurrentHealth-damage < 1;

        if (killed)
        {
            switch (index)
            {
                case 0:
                    message = "Your strike lands true, killing the " + targetName; break;
                case 1:
                    message = "The " + targetName + " collapses as you land the killing blow!"; break;
                case 2:
                    message = "A solid hit from you knocks the " + targetName + " to the ground, never to rise again!"; break;
                case 3:
                    message = "Your strike bites deep and the " + targetName + " falls over in death!"; break;
                case 4:
                    message = "The " + targetName + " clutches at a mortal wound as your swing connects!"; break;
            }
        }
        else
        {
            switch (index)
            {
                case 0:
                    message = "Your strike lands true, sending the " + targetName + " reeling!"; break;
                case 1:
                    message = "The " + targetName + " recoils as you land a telling blow!"; break;
                case 2:
                    message = "A solid hit from you knocks the " + targetName + " off-balance for a moment!"; break;
                case 3:
                    message = "Your strike bites deep and the " + targetName + " cries out in pain!"; break;
                case 4:
                    message = "The " + targetName + " momentarily stumbles as your swing connects!"; break;
            }
        }

        DaggerfallUI.Instance.PopupMessage(message);
    }

    void PlayerAttackMissMessage(DaggerfallEntity target)
    {
        bool canParry = false;

        string targetName = target.Name;
        EnemyEntity enemy = target as EnemyEntity;
        if (enemy != null)
        {
            targetName = TextManager.Instance.GetLocalizedEnemyName(enemy.MobileEnemy.ID);
            if (enemy.MobileEnemy.ParrySounds)
                canParry = true;
        }


        int index = UnityEngine.Random.Range(0, 5);
        string message = "";

        if (canParry)
        {
            switch (index)
            {
                case 0:
                    message = "The " + targetName + " deftly redirects your swing!"; break;
                case 1:
                    message = "The " + targetName + "'s armor turns your blow!"; break;
                case 2:
                    message = "Your attack glances off the " + targetName + "'s armor!"; break;
                case 3:
                    message = "A turn of the " + targetName + "'s weapon sends your attack sailing harmlessly past"; break;
                case 4:
                    message = "Your swing meets the " + targetName + "'s weapon and is skillfully turned!"; break;
            }
        }
        else
        {
            switch (index)
            {
                case 0:
                    message = "The " + targetName + " deftly side-steps your swing!"; break;
                case 1:
                    message = "The " + targetName + " jukes at the last moment and avoids your attack!"; break;
                case 2:
                    message = "Your attack slices through empty air as the " + targetName + " slides out of reach!"; break;
                case 3:
                    message = "Your swing sails harmlessly past the " + targetName + " as they evade!"; break;
                case 4:
                    message = "The " + targetName + " narrowly evades your swing!"; break;
            }
        }

        DaggerfallUI.Instance.PopupMessage(message);
    }

    void PlayerTargetHitMessage(DaggerfallEntity target, int damage)
    {
        int index = UnityEngine.Random.Range(0, 5);
        string message = "";

        string targetName = target.Name;
        EnemyEntity enemy = target as EnemyEntity;
        if (enemy != null)
            targetName = TextManager.Instance.GetLocalizedEnemyName(enemy.MobileEnemy.ID);

        bool killed = GameManager.Instance.PlayerEntity.CurrentHealth - damage < 1;

        if (killed)
        {
            switch (index)
            {
                case 0:
                    message = "The " + targetName + "'s strike lands true, killing you!"; break;
                case 1:
                    message = "You collapse as the " + targetName + " lands a killing blow!"; break;
                case 2:
                    message = "A solid hit from the " + targetName + " knocks you to the ground, never to rise again!"; break;
                case 3:
                    message = "The " + targetName + "'s strike bites deep and you fall over in death!"; break;
                case 4:
                    message = "You clutch at a mortal wound as the " + targetName + "'s swing connects!"; break;
            }
        }
        else
        {
            switch (index)
            {
                case 0:
                    message = "The " + targetName + "'s strike lands true, sending you reeling!"; break;
                case 1:
                    message = "You recoil as the " + targetName + " lands a telling blow!"; break;
                case 2:
                    message = "A solid hit from the " + targetName + " knocks you off-balance for a moment!"; break;
                case 3:
                    message = "The " + targetName + "'s strike bites deep and you cry out in pain!"; break;
                case 4:
                    message = "You momentarily stumble as the " + targetName + "'s swing connects!"; break;
            }
        }

        DaggerfallUI.Instance.PopupMessage(message);
    }

    void PlayerTargetMissMessage(DaggerfallEntity target)
    {
        int index = UnityEngine.Random.Range(0, 5);
        string message = "";

        string targetName = target.Name;
        EnemyEntity enemy = target as EnemyEntity;
        if (enemy != null)
            targetName = TextManager.Instance.GetLocalizedEnemyName(enemy.MobileEnemy.ID);

        switch (index)
        {
            case 0:
                message = "You deftly side-step the " + targetName + "'s swing!"; break;
            case 1:
                message = "You juke at the last moment and avoid the " + targetName + "'s attack!"; break;
            case 2:
                message = "The " + targetName + "'s attack slices through empty air as you slide away!"; break;
            case 3:
                message = "The " + targetName + "'s swing sails harmlessly past as you evade it!"; break;
            case 4:
                message = "You narrowly evade the " + targetName + "'s swing!"; break;
        }

        DaggerfallUI.Instance.PopupMessage(message);
    }
}
