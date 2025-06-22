using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;

namespace WeaponFeaturesAddonsMod
{
    public class WeaponFeaturesAddons : MonoBehaviour
    {
        private static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<WeaponFeaturesAddons>();

            mod.IsReady = true;
        }

        public static WeaponFeaturesAddons Instance;

        Mod weaponWidget;
        Mod tomeOfBattle;

        bool crossbow = false;
        public int crossbowItemTemplateIndex = 1307;
        string crossbowFilename = "CROSSBOW";
        public Vector2Int crossbowDamage = new Vector2Int(8,24);
        float crossbowReloadTime = 3;
        float crossbowDrawZoom = 2;
        float crossbowDrawScale = 0;
        float crossbowDrawTime = 999;
        float crossbowMissileSpeed = 2;
        float crossbowMissileSpread = 0f;

        bool spear = false;
        public int spearItemTemplateIndex = 1308;
        string spearFilename = "SPEAR";
        public Vector2Int spearDamage = new Vector2Int(4, 16);
        bool spearMirrorOverride = true;
        bool spearRecoveryOverride = true;
        bool spearAlignmentOverride = false;
        float spearReach = 5;
        int spearCleave = 300;

        bool knuckle = false;
        int knuckleItemTemplateIndex = 1309;
        string knuckleFilename = "KNUCKLES";
        public Vector2Int knuckleDamage = new Vector2Int(1, 4);
        float knuckleReach = 2;
        int knuckleCleave = 100;

        FieldInfo CustomItems;
        Dictionary<string, Type> customItems;

        private void Start()
        {
            Instance = this;

            CustomItems = typeof(ItemCollection).GetField("customItems", BindingFlags.NonPublic | BindingFlags.Static);
            if (CustomItems != null)
                Debug.Log("TOME OF BATTLE ADDENDUM - FOUND CUSTOMITEMS FIELD.");

            SaveLoadManager.OnLoad += OnLoad;

            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
        }
        private void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            bool setup = false;

            if (change.HasChanged("Crossbow"))
            {
                crossbow = settings.GetValue<bool>("Crossbow", "Enabled");
                crossbowDamage = new Vector2Int(settings.GetTupleInt("Crossbow","Damage").First,settings.GetTupleInt("Crossbow", "Damage").Second);
                crossbowReloadTime = settings.GetValue<float>("Crossbow", "ReloadTime");
                crossbowDrawZoom = settings.GetValue<float>("Crossbow", "DrawZoom");
                crossbowMissileSpeed = settings.GetValue<float>("Crossbow", "MissileSpeed");
                crossbowMissileSpread = settings.GetValue<float>("Crossbow", "MissileSpread");
                setup = true;
            }

            if (change.HasChanged("Spear"))
            {
                spear = settings.GetValue<bool>("Spear", "Enabled");
                spearDamage = new Vector2Int(settings.GetTupleInt("Spear", "Damage").First, settings.GetTupleInt("Spear", "Damage").Second);
                spearReach = settings.GetValue<float>("Spear", "Reach");
                spearCleave = settings.GetValue<int>("Spear", "Cleave") * 100;
                spearMirrorOverride = settings.GetValue<bool>("Spear", "Mirror");
                setup = true;
            }

            if (change.HasChanged("Knuckles"))
            {
                knuckle = settings.GetValue<bool>("Knuckles", "Enabled");
                knuckleDamage = new Vector2Int(settings.GetTupleInt("Knuckles", "Damage").First, settings.GetTupleInt("Knuckles", "Damage").Second);
                knuckleReach = settings.GetValue<float>("Knuckles", "Reach");
                knuckleCleave = settings.GetValue<int>("Knuckles", "Cleave") * 100;
                setup = true;
            }

            if (setup)
                SetupWeapons();
        }

        void SetupWeapons()
        {
            if (weaponWidget == null)
                weaponWidget = ModManager.Instance.GetModFromGUID("9f301f2b-298b-43d8-8f3f-c54deaa841e0");

            if (tomeOfBattle == null)
                tomeOfBattle = ModManager.Instance.GetModFromGUID("a166c215-0a5a-4582-8bf3-8be8df80d5e5");

            //register custom weapons
            customItems = CustomItems.GetValue(null) as Dictionary<string, Type>;
            if (crossbow && !customItems.ContainsKey(typeof(ItemCrossbow).ToString()))
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(crossbowItemTemplateIndex, ItemGroups.Weapons, typeof(ItemCrossbow));

            if (spear && !customItems.ContainsKey(typeof(ItemSpear).ToString()))
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(spearItemTemplateIndex, ItemGroups.Weapons, typeof(ItemSpear));

            if (knuckle && !customItems.ContainsKey(typeof(ItemKnuckles).ToString()))
                DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(knuckleItemTemplateIndex, ItemGroups.Weapons, typeof(ItemKnuckles));

            //set texture override in Weapon Widget
            if (weaponWidget != null)
            {
                object[] crossbowData = new object[7]
                {
                    crossbowItemTemplateIndex,
                    crossbowFilename,
                    false,
                    false,
                    false,
                    false,
                    true
                };
                ModManager.Instance.SendModMessage(weaponWidget.Title, "registerCustomWeapon", crossbowData);

                object[] spearData = new object[7]
                {
                    spearItemTemplateIndex,
                    spearFilename,
                    true,
                    spearRecoveryOverride,
                    spearAlignmentOverride,
                    spearMirrorOverride,
                    true,
                };
                ModManager.Instance.SendModMessage(weaponWidget.Title, "registerCustomWeapon", spearData);

                object[] knuckleData = new object[7]
                {
                    knuckleItemTemplateIndex,
                    knuckleFilename,
                    false,
                    false,
                    false,
                    true,
                    false
                };
                ModManager.Instance.SendModMessage(weaponWidget.Title, "registerCustomWeapon", knuckleData);
            }

            //set weapon behavior in Tome of Battle
            if (tomeOfBattle != null)
            {
                object[] crossbowData = new object[9]
                {
                    crossbowItemTemplateIndex,
                    0f,
                    (int)0,
                    crossbowReloadTime,
                    crossbowDrawZoom,
                    crossbowDrawScale,
                    crossbowDrawTime,
                    crossbowMissileSpeed,
                    crossbowMissileSpread
                };
                ModManager.Instance.SendModMessage(tomeOfBattle.Title, "registerWeaponOverride", crossbowData);

                object[] spearData = new object[9]
                {
                    spearItemTemplateIndex,
                    spearReach,
                    spearCleave,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f
                };
                ModManager.Instance.SendModMessage(tomeOfBattle.Title, "registerWeaponOverride", spearData);

                object[] knuckleData = new object[9]
                {
                    knuckleItemTemplateIndex,
                    knuckleReach,
                    knuckleCleave,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f,
                    0f
                };
                ModManager.Instance.SendModMessage(tomeOfBattle.Title, "registerWeaponOverride", knuckleData);
            }
        }

        void OnLoad(SaveData_v1 saveData)
        {
            FixOutdatedWeapons();
        }

        void FixOutdatedWeapons()
        {
            //look for outdated weapon items in inventory and wagon
            UpdateWeapons(GameManager.Instance.PlayerEntity.Items, true);
            UpdateWeapons(GameManager.Instance.PlayerEntity.WagonItems);
            DaggerfallLoot[] loots = GameObject.FindObjectsOfType<DaggerfallLoot>();
            if (loots.Length > 0)
            {
                foreach (DaggerfallLoot loot in loots)
                {
                    if (loot.Items.Count > 0)
                        UpdateWeapons(loot.Items);
                }
            }
        }

        bool IsCustomWeapon(DaggerfallUnityItem weapon)
        {
            if ((weapon is ItemCrossbow && weapon.TemplateIndex != crossbowItemTemplateIndex) ||
                (weapon is ItemSpear && weapon.TemplateIndex != spearItemTemplateIndex) ||
                (weapon is ItemKnuckles && weapon.TemplateIndex != knuckleItemTemplateIndex)
                )
                return true;

            return false;
        }

        void UpdateWeapons(ItemCollection collection, bool inventory = false)
        {
            if (collection.Count < 1)
                return;

            List<DaggerfallUnityItem> itemsToRemove = new List<DaggerfallUnityItem>();

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            for (int i = 0; i < collection.Count; i++)
            {
                DaggerfallUnityItem item = collection.GetItem(i);

                int templateIndex = item.TemplateIndex;

                if (IsCustomWeapon(item))
                {
                    //is outdated custom weapon
                    //regenerate it
                    itemsToRemove.Add(item);

                    DaggerfallUnityItem newItem = null;

                    bool equipped = item.IsEquipped;

                    if (item is ItemCrossbow)
                        newItem = ItemBuilder.CreateItem(ItemGroups.Weapons, crossbowItemTemplateIndex);
                    else if (item is ItemSpear)
                        newItem = ItemBuilder.CreateItem(ItemGroups.Weapons, spearItemTemplateIndex);
                    else if (item is ItemKnuckles)
                        newItem = ItemBuilder.CreateItem(ItemGroups.Armor, knuckleItemTemplateIndex);

                    if (newItem != null)
                    {
                        //set the variant
                        int variant = item.message;

                        //Debug.Log(newItem.ItemName + " variant is " + variant.ToString());

                        ItemBuilder.ApplyArmorSettings(newItem, player.Gender, player.Race, (ArmorMaterialTypes)item.nativeMaterialValue, variant);

                        newItem.currentCondition = item.currentCondition;

                        if (item.HasLegacyEnchantments || item.HasCustomEnchantments)
                        {
                            if (item.HasLegacyEnchantments)
                                newItem.legacyMagic = item.legacyMagic;

                            if (item.HasCustomEnchantments)
                                newItem.customMagic = item.customMagic;

                            newItem.shortName = item.shortName;
                        }

                        collection.AddItem(newItem);

                        if (item.IsIdentified)
                            newItem.IdentifyItem();

                        if (equipped && inventory)
                            player.ItemEquipTable.EquipItem(newItem, true, false);
                    }
                }
            }

            if (itemsToRemove.Count > 0)
            {
                foreach (DaggerfallUnityItem item in itemsToRemove)
                    collection.RemoveItem(item);
            }
        }
    }
}
