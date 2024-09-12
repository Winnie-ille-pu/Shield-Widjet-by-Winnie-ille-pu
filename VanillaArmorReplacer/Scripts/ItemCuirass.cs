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
    /*
        Leather, Hide, Hardened Leather, Studded Leather, Brigandine

            Leather     = 0x0000,
            Chain       = 0x0100,
            Chain2      = 0x0103,
            Iron        = 0x0200,
            Steel       = 0x0201,
            Silver      = 0x0202,
            Elven       = 0x0203,
            Dwarven     = 0x0204,
            Mithril     = 0x0205,
            Adamantium  = 0x0206,
            Ebony       = 0x0207,
            Orcish      = 0x0208,
            Daedric     = 0x0209,

    */
    public class ItemCuirass : DaggerfallUnityItem
    {
        public const int templateIndex = 600;

        public ItemCuirass() : base(ItemGroups.Armor, templateIndex)
        {
        }

        public override int CurrentVariant
        {
            set
            {
                if (VanillaArmorReplacer.Instance.textureArchive == 1) //Vanilla textures with Leather and Chain
                {
                    if (NativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    {
                        base.CurrentVariant = 0;
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassLeather;
                    }
                    else if (NativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    {
                        base.CurrentVariant = 4;
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassChain;
                    }
                    else
                    {
                        base.CurrentVariant = Mathf.Clamp(value,1,3);
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassPlate;
                    }
                }
                else //Vanilla textures
                {
                    if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                    {
                        base.CurrentVariant = 0;
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassLeather;
                    }
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    {
                        base.CurrentVariant = 4;
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassChain;
                    }
                    else
                    {
                        base.CurrentVariant = Mathf.Clamp(value, 1, 3);
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                        {
                            if (NativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                                shortName = VanillaArmorReplacer.Instance.nameCuirassLeather;
                            else if (NativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                                shortName = VanillaArmorReplacer.Instance.nameCuirassChain;
                            else
                                shortName = VanillaArmorReplacer.Instance.nameCuirassPlate;
                        }
                    }
                }
            }
        }

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
            return EquipSlots.ChestArmor;
        }

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
            data.className = typeof(ItemCuirass).ToString();
            return data;
        }
    }
}

