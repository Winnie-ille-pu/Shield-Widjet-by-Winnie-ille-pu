// Project:         RoleplayRealism:Items mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Hazelnut
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Hazelnut

using UnityEngine;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop;

namespace VanillaArmorReplacer
{
    public class ItemGreaves : DaggerfallUnityItem
    {
        public const int templateIndex = 602;

        public ItemGreaves() : base(ItemGroups.Armor, templateIndex)
        {
        }
        public override int CurrentVariant
        {
            set
            {
                if (VanillaArmorReplacer.Instance.textureArchive == 1)
                {
                    if (NativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    {
                        base.CurrentVariant = Random.Range(0, 2);
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameGreavesLeather;
                    }
                    else if (NativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    {
                        base.CurrentVariant = 6;
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameGreavesChain;
                    }
                    else
                    {
                        base.CurrentVariant = Mathf.Clamp(value, 2, 5);
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameGreavesPlate;
                    }
                }
                else
                {
                    if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    {
                        base.CurrentVariant = Random.Range(0, 2);
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameGreavesLeather;
                    }
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    {
                        base.CurrentVariant = 6;
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameGreavesLeather;
                    }
                    else
                    {
                        base.CurrentVariant = Mathf.Clamp(value, 2, 5);
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                        {
                            if (NativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                                shortName = VanillaArmorReplacer.Instance.nameGreavesLeather;
                            else if (NativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                                shortName = VanillaArmorReplacer.Instance.nameGreavesChain;
                            else
                                shortName = VanillaArmorReplacer.Instance.nameGreavesPlate;
                        }
                    }
                }
            }
        }

        /*// Add brig prefix to name for Iron+ materials.
        public override int CurrentVariant
        {
            set
            {
                base.CurrentVariant = value;
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                {
                    shortName = ItemHauberk.padded + shortName;
                    nativeMaterialValue = (int)ArmorMaterialTypes.Leather;
                    message = 1;
                }
                else
                    shortName = ItemHauberk.mail + shortName;
            }
        }

        // Always use same archive for both genders as the same image set is used
        public override int InventoryTextureArchive
        {
            get { return templateIndex; }
        }

        public override int InventoryTextureRecord
        {
            get
            {
                int offset = PlayerTextureArchive - ItemBuilder.firstFemaleArchive;
                // Only use 2 & 6 / 10 & 14 human morphology for now..

                offset = (offset < 4) ? 2 : 6;

                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    offset += 10;
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    offset += 20;

                return offset;
            }
        }*/

        // Gets native material value, modifying it to use the 'chain' value for first byte if plate.
        // This fools the DFU code into treating this item as chainmail for forbidden checks etc.
        public override int NativeMaterialValue
        {
            get
            {
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    return (int)ArmorMaterialTypes.Leather;
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    return (int)ArmorMaterialTypes.Chain;
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Iron)
                {
                    if (VanillaArmorReplacer.Instance.armorIron == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorIron == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Steel)
                {
                    if (VanillaArmorReplacer.Instance.armorSteel == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorSteel == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Silver)
                {
                    if (VanillaArmorReplacer.Instance.armorSilver == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorSilver == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Elven)
                {
                    if (VanillaArmorReplacer.Instance.armorElven == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorElven == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Dwarven)
                {
                    if (VanillaArmorReplacer.Instance.armorDwarven == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorDwarven == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Mithril)
                {
                    if (VanillaArmorReplacer.Instance.armorMithril == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorMithril == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Adamantium)
                {
                    if (VanillaArmorReplacer.Instance.armorAdamantium == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorAdamantium == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Ebony)
                {
                    if (VanillaArmorReplacer.Instance.armorEbony == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorEbony == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Orcish)
                {
                    if (VanillaArmorReplacer.Instance.armorOrcish == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorOrcish == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Daedric)
                {
                    if (VanillaArmorReplacer.Instance.armorDaedric == 1)
                        return (int)ArmorMaterialTypes.Chain;
                    else
                    if (VanillaArmorReplacer.Instance.armorDaedric == 2)
                        return (int)ArmorMaterialTypes.Leather;
                    else
                        return nativeMaterialValue;
                }
                else
                    return nativeMaterialValue;
            }
        }

        public override EquipSlots GetEquipSlot()
        {
            return EquipSlots.LegsArmor;
        }

        /*public override int GetMaterialArmorValue()
        {
            return ItemHauberk.GetChainmailMaterialArmorValue(nativeMaterialValue);
        }*/

        public override int GetEnchantmentPower()
        {
            float multiplier = FormulaHelper.GetArmorEnchantmentMultiplier((ArmorMaterialTypes)nativeMaterialValue);
            return enchantmentPoints + Mathf.FloorToInt(enchantmentPoints * multiplier);
        }

        public override SoundClips GetEquipSound()
        {
            return SoundClips.EquipLeather;
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemGreaves).ToString();
            return data;
        }

    }
}

