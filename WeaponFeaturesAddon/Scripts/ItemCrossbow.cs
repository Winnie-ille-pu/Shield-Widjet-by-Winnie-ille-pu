using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace WeaponFeaturesAddonsMod
{
    public class ItemCrossbow : DaggerfallUnityItem
    {
        public const int CustomTemplateIndex = 1307;
        private const string itemName = "Crossbow";

        public ItemCrossbow() : base(ItemGroups.Weapons, CustomTemplateIndex)
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
                return 1;
            }
        }

        public override int GetBaseDamageMin()
        {
            return WeaponFeaturesAddons.Instance.crossbowDamage.x;
        }

        public override int GetBaseDamageMax()
        {
            return WeaponFeaturesAddons.Instance.crossbowDamage.y;
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
            return WeaponTypes.Bow;
        }

        public override int GetWeaponSkillUsed()
        {
            return (int)DaggerfallConnect.DFCareer.ProficiencyFlags.MissileWeapons;
        }

        public override SoundClips GetEquipSound()
        {
            return SoundClips.EquipBow;
        }

        public override SoundClips GetSwingSound()
        {
            return SoundClips.ArrowShoot;
        }
        public override ItemData_v1 GetSaveData()
        {
            var data = base.GetSaveData();
            data.className = $"WeaponFeaturesAddonsMod.{nameof(ItemCrossbow)}";
            return data;
        }
    }
}
