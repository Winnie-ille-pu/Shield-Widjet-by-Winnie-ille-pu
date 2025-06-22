using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Formulas;

namespace WeaponFeaturesAddonsMod
{
    public class ItemKnuckles : DaggerfallUnityItem
    {
        public const int CustomTemplateIndex = 1309;
        private const string itemName = "Cesti";

        public ItemKnuckles() : base(ItemGroups.Weapons, CustomTemplateIndex)
        {

        }

        public override int InventoryTextureArchive
        {
            get
            {
                if (GameManager.Instance.PlayerEntity.Gender == DaggerfallWorkshop.Game.Entity.Genders.Female)
                    return 112391;
                else
                    return 112392;
            }
        }

        public override int InventoryTextureRecord
        {
            get
            {
                return 3;
            }
        }

        public override int GetBaseDamageMin()
        {
            return FormulaHelper.CalculateHandToHandMinDamage(GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DaggerfallConnect.DFCareer.Skills.HandToHand)) + WeaponFeaturesAddons.Instance.knuckleDamage.x;
        }

        public override int GetBaseDamageMax()
        {
            return FormulaHelper.CalculateHandToHandMaxDamage(GameManager.Instance.PlayerEntity.Skills.GetLiveSkillValue(DaggerfallConnect.DFCareer.Skills.HandToHand)) + WeaponFeaturesAddons.Instance.knuckleDamage.y;
        }

        public override string ItemName
        {
            get
            {
                if (IsIdentified)
                    return shortName.Replace("%it", itemName);
                else
                    return itemName;
            }
        }

        public override string LongName
        {
            get
            {
                return $"{DaggerfallUnity.Instance.TextProvider.GetWeaponMaterialName((WeaponMaterialTypes)NativeMaterialValue)} {ItemName}";
            }
        }

        public override int GroupIndex
        {
            get { return 0; }
            set { base.GroupIndex = value; }
        }

        public override ItemHands GetItemHands()
        {
            return ItemHands.Both;
        }

        public override WeaponTypes GetWeaponType()
        {
            return WeaponTypes.Warhammer;
        }

        public override int GetWeaponSkillUsed()
        {
            return (int)DaggerfallConnect.DFCareer.ProficiencyFlags.HandToHand;
        }

        public override short GetWeaponSkillIDAsShort()
        {
            return (int)DaggerfallConnect.DFCareer.ProficiencyFlags.HandToHand;
        }

        public override SoundClips GetEquipSound()
        {
            return SoundClips.EquipLeather;
        }

        public override SoundClips GetSwingSound()
        {
            return SoundClips.SwingHighPitch;
        }
        public override ItemData_v1 GetSaveData()
        {
            var data = base.GetSaveData();
            data.className = $"WeaponFeaturesAddonsMod.{nameof(ItemKnuckles)}";
            return data;
        }
    }
}
