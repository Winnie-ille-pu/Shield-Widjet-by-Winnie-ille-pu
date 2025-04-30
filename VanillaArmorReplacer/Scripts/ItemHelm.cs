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
    public class ItemHelm : DaggerfallUnityItem
    {
        public const int templateIndex = 605;

        public ItemHelm() : base(ItemGroups.Armor, templateIndex)
        {
        }
        public override int CurrentVariant
        {
            set
            {
                if (VanillaArmorReplacer.Instance.textureArchive == 2 && (nativeMaterialValue == (int)ArmorMaterialTypes.Leather || nativeMaterialValue == (int)ArmorMaterialTypes.Chain))
                {
                    base.CurrentVariant = 0;
                    if (!HasCustomEnchantments && !HasLegacyEnchantments)
                    {
                        if (NativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                            shortName = VanillaArmorReplacer.Instance.nameHelmLeather;
                        else if (NativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                            shortName = VanillaArmorReplacer.Instance.nameHelmChain;
                        else
                            shortName = VanillaArmorReplacer.Instance.nameHelmPlate;
                    }
                }
                else
                {
                    base.CurrentVariant = message;
                    if (!HasCustomEnchantments && !HasLegacyEnchantments)
                    {
                        if (NativeMaterialValue == (int)ArmorMaterialTypes.Leather)
                            shortName = VanillaArmorReplacer.Instance.nameHelmLeather;
                        else if (NativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                            shortName = VanillaArmorReplacer.Instance.nameHelmChain;
                        else
                            shortName = VanillaArmorReplacer.Instance.nameHelmPlate;
                    }
                }
            }
        }

        //set gender and phenotype
        public override int InventoryTextureArchive
        {
            get
            {
                if (VanillaArmorReplacer.Instance.textureArchive == 2 && (nativeMaterialValue == (int)ArmorMaterialTypes.Leather || nativeMaterialValue == (int)ArmorMaterialTypes.Chain))
                {
                    int offset = PlayerTextureArchive - ItemBuilder.firstFemaleArchive;

                    //race and gender
                    //0 = female argonian = 7
                    //1 = female elf = 5
                    //2 = female human = 4
                    //3 = female khajiit = 6
                    //4 = male argonian = 3
                    //5 = male elf = 1
                    //6 = male human = 0
                    //7 = male khajiit = 2

                    int archive = 112350;

                    if (offset == 0)
                        archive += 7;
                    else if (offset == 1)
                        archive += 5;
                    else if (offset == 2)
                        archive += 4;
                    else if (offset == 3)
                        archive += 6;
                    else if (offset == 4)
                        archive += 3;
                    else if (offset == 5)
                        archive += 1;
                    else if (offset == 6)
                        archive += 0;
                    else if (offset == 7)
                        archive += 2;

                    return archive;
                }
                else
                    return base.InventoryTextureArchive;
            }
        }

        public override int InventoryTextureRecord
        {
            get
            {
                if (VanillaArmorReplacer.Instance.textureArchive == 2 && (nativeMaterialValue == (int)ArmorMaterialTypes.Leather || nativeMaterialValue == (int)ArmorMaterialTypes.Chain))
                {
                    int offset = 4;

                    if (nativeMaterialValue == (int)ArmorMaterialTypes.Daedric)
                        offset += 1100;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Orcish)
                        offset += 1000;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Ebony)
                        offset += 900;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Adamantium)
                        offset += 800;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Mithril)
                        offset += 700;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Dwarven)
                        offset += 600;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Elven)
                        offset += 500;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Silver)
                        offset += 400;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Steel)
                        offset += 300;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Iron)
                        offset += 200;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                        offset += 100;

                    dyeColor = DyeColors.Silver;

                    return offset;
                }
                else
                    return base.InventoryTextureRecord;
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
            return EquipSlots.Head;
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
            data.className = typeof(ItemHelm).ToString();
            return data;
        }

    }
}

