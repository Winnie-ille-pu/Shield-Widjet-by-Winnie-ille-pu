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
        public const int templateIndex = 1300;

        public ItemCuirass() : base(ItemGroups.Armor, templateIndex)
        {
        }

        public override int CurrentVariant
        {
            set
            {
                if (VanillaArmorReplacer.Instance.textureArchive == 3)
                {
                    base.CurrentVariant = 0;
                    if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetLeatherMaterialValue(nativeMaterialValue))
                    {
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassLeather;
                    }
                    else if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetChainMaterialValue(nativeMaterialValue))
                    {
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassChain;
                    }
                    else
                    {
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassPlate;
                    }

                    //set variant
                    if (NativeMaterialValue != VanillaArmorReplacer.Instance.GetLeatherMaterialValue(nativeMaterialValue) && nativeMaterialValue != (int)ArmorMaterialTypes.Leather)
                    {
                        if (message < 0 || message > 1)
                            message = Random.Range(0, 2);
                    }
                    else
                        message = 0;
                }
                else if (VanillaArmorReplacer.Instance.textureArchive == 2 && (nativeMaterialValue == (int)ArmorMaterialTypes.Leather || nativeMaterialValue == (int)ArmorMaterialTypes.Chain))
                {
                    base.CurrentVariant = 0;
                    if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetLeatherMaterialValue(nativeMaterialValue))
                    {
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassLeather;
                    }
                    else if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetChainMaterialValue(nativeMaterialValue))
                    {
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassChain;
                    }
                    else
                    {
                        base.CurrentVariant = Mathf.Clamp(value, 1, 3);
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassPlate;
                    }
                }
                else if (VanillaArmorReplacer.Instance.textureArchive == 1) //Vanilla textures with Leather and Chain
                {
                    if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetLeatherMaterialValue(nativeMaterialValue))
                    {
                        base.CurrentVariant = 0;
                        if (!HasCustomEnchantments && !HasLegacyEnchantments)
                            shortName = VanillaArmorReplacer.Instance.nameCuirassLeather;
                    }
                    else if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetChainMaterialValue(nativeMaterialValue))
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
                            if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetLeatherMaterialValue(nativeMaterialValue))
                                shortName = VanillaArmorReplacer.Instance.nameCuirassLeather;
                            else if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetChainMaterialValue(nativeMaterialValue))
                                shortName = VanillaArmorReplacer.Instance.nameCuirassChain;
                            else
                                shortName = VanillaArmorReplacer.Instance.nameCuirassPlate;
                        }
                    }
                }
            }
        }

        //set gender and phenotype

        //set gender and phenotype
        public override int InventoryTextureArchive
        {
            get
            {
                if (
                    VanillaArmorReplacer.Instance.textureArchive == 3 ||
                    (VanillaArmorReplacer.Instance.textureArchive == 2 && (nativeMaterialValue == (int)ArmorMaterialTypes.Leather || nativeMaterialValue == (int)ArmorMaterialTypes.Chain))
                    )
                {
                    int offset = PlayerTextureArchive - ItemBuilder.firstFemaleArchive;

                    if (offset < 4)
                        return 112354;
                    else
                        return 112350;
                }
                else
                    return base.InventoryTextureArchive;
            }
        }

        public override int InventoryTextureRecord
        {
            get
            {
                if (VanillaArmorReplacer.Instance.textureArchive == 3 ||
                    (VanillaArmorReplacer.Instance.textureArchive == 2 && (nativeMaterialValue == (int)ArmorMaterialTypes.Leather || nativeMaterialValue == (int)ArmorMaterialTypes.Chain))
                    )
                {
                    int offset = 1;

                    if (VanillaArmorReplacer.Instance.textureArchive == 3)
                    {
                        //get Typed archive
                        if (NativeMaterialValue >= (int)ArmorMaterialTypes.Iron)
                            offset += 200;
                        else if (NativeMaterialValue == VanillaArmorReplacer.Instance.GetChainMaterialValue(nativeMaterialValue) || nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                            offset += 100;

                        //get variant
                        if (message != -1)
                            offset += 1000 * message;
                    }
                    else
                    {
                        //get custom archive
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
                    }


                    //get Material variant
                    if (nativeMaterialValue == (int)ArmorMaterialTypes.Daedric)
                        offset += 90;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Orcish)
                        offset += 80;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Ebony)
                        offset += 70;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Adamantium)
                        offset += 60;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Mithril)
                        offset += 50;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Dwarven)
                        offset += 40;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Elven)
                        offset += 30;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Silver)
                        offset += 20;
                    else if (nativeMaterialValue == (int)ArmorMaterialTypes.Steel)
                        offset += 10;

                    dyeColor = DyeColors.Silver;

                    return offset;
                }
                else
                {
                    dyeColor = DaggerfallUnity.Instance.ItemHelper.GetArmorDyeColor((ArmorMaterialTypes)nativeMaterialValue);
                    return base.InventoryTextureRecord;
                }
            }
        }

        public override int NativeMaterialValue
        {
            get
            {
                if (nativeMaterialValue == (int)ArmorMaterialTypes.Leather || nativeMaterialValue == (int)ArmorMaterialTypes.Chain)
                    return nativeMaterialValue;
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Iron)
                {
                    if (VanillaArmorReplacer.Instance.armorIron == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorIron == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Steel)
                {
                    if (VanillaArmorReplacer.Instance.armorSteel == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorSteel == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Silver)
                {
                    if (VanillaArmorReplacer.Instance.armorSilver == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorSilver == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Elven)
                {
                    if (VanillaArmorReplacer.Instance.armorElven == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorElven == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Dwarven)
                {
                    if (VanillaArmorReplacer.Instance.armorDwarven == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorDwarven == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Mithril)
                {
                    if (VanillaArmorReplacer.Instance.armorMithril == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorMithril == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Adamantium)
                {
                    if (VanillaArmorReplacer.Instance.armorAdamantium == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorAdamantium == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Ebony)
                {
                    if (VanillaArmorReplacer.Instance.armorEbony == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorEbony == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Orcish)
                {
                    if (VanillaArmorReplacer.Instance.armorOrcish == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorOrcish == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
                    else
                        return nativeMaterialValue;
                }
                else if (nativeMaterialValue == (int)ArmorMaterialTypes.Daedric)
                {
                    if (VanillaArmorReplacer.Instance.armorDaedric == 1)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Chain);
                    else
                    if (VanillaArmorReplacer.Instance.armorDaedric == 2)
                        return VanillaArmorReplacer.Instance.GetNativeMaterialValueBitwise(nativeMaterialValue, (int)ArmorMaterialTypes.Leather);
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

