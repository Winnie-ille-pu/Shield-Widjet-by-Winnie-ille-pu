using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;

namespace VanillaArmorReplacer
{
    public class VanillaArmorReplacer : MonoBehaviour
    {
        static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]

        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<VanillaArmorReplacer>();
        }

        public static VanillaArmorReplacer Instance;

        public bool initializeOnLoad;
        public bool feedbackMessage;

        //use to install or uninstall
        //0 = do nothing
        //1 = modded
        //2 = vanilla
        public int convertTo = 0;

        //determine modded armor appearance
        //0 = vanilla textures
        //1 = vanilla textures with leather and chain
        //2 = custom texture archive
        public int textureArchive = 0;

        //armor type settings
        public int armorIron = 0;
        public int armorSteel = 0;
        public int armorSilver = 0;
        public int armorElven = 0;
        public int armorDwarven = 0;
        public int armorMithril = 0;
        public int armorAdamantium = 0;
        public int armorEbony = 0;
        public int armorOrcish = 0;
        public int armorDaedric = 0;

        //armor name settings
        public string nameCuirassLeather;
        public string nameGauntletsLeather;
        public string nameGreavesLeather;
        public string namePauldronLeather;
        public string nameHelmLeather;
        public string nameBootsLeather;
        public string nameCuirassChain;
        public string nameGauntletsChain;
        public string nameGreavesChain;
        public string namePauldronChain;
        public string nameHelmChain;
        public string nameBootsChain;
        public string nameCuirassPlate;
        public string nameGauntletsPlate;
        public string nameGreavesPlate;
        public string namePauldronPlate;
        public string nameHelmPlate;
        public string nameBootsPlate;

        //custom texture archive stuff

        //custom dye stuff
        /*byte[] dyeLeather = new byte[] { 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F };
        byte[] dyeChain = new byte[] { 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F };
        byte[] dyeIron = new byte[] { 0x77, 0x78, 0x57, 0x79, 0x58, 0x59, 0x7A, 0x5A, 0x7B, 0x5B, 0x7C, 0x5C, 0x7D, 0x5D, 0x5E, 0x5F };
        byte[] dyeSteel = new byte[] { 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F };
        byte[] dyeSilver = new byte[] { 0xE0, 0x70, 0x50, 0x71, 0x51, 0x72, 0x73, 0x52, 0x74, 0x53, 0x75, 0x54, 0x55, 0x56, 0x57, 0x58 };
        byte[] dyeElven = new byte[] { 0xE0, 0x70, 0x50, 0x71, 0x51, 0x72, 0x73, 0x52, 0x74, 0x53, 0x75, 0x54, 0x55, 0x56, 0x57, 0x58 };
        byte[] dyeDwarven = new byte[] { 0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F };
        byte[] dyeMithril = new byte[] { 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE };
        byte[] dyeAdamantium = new byte[] { 0x5A, 0x5B, 0x7C, 0x5C, 0x7D, 0x5D, 0x7E, 0x5E, 0x7F, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE };
        byte[] dyeEbony = new byte[] { 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE };
        byte[] dyeOrcish = new byte[] { 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD };
        byte[] dyeDaedric = new byte[] { 0xEF, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE };*/

        void Awake()
        {
            ModSettings settings = mod.GetSettings();

            Instance = this;

            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();

            InitMod();

            mod.IsReady = true;
        }

        private static void InitMod()
        {
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemCuirass.templateIndex, ItemGroups.Armor, typeof(ItemCuirass));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemGauntlets.templateIndex, ItemGroups.Armor, typeof(ItemGauntlets));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemGreaves.templateIndex, ItemGroups.Armor, typeof(ItemGreaves));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemPauldronLeft.templateIndex, ItemGroups.Armor, typeof(ItemPauldronLeft));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemPauldronRight.templateIndex, ItemGroups.Armor, typeof(ItemPauldronRight));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemHelm.templateIndex, ItemGroups.Armor, typeof(ItemHelm));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemBoots.templateIndex, ItemGroups.Armor, typeof(ItemBoots));

            PlayerActivate.OnLootSpawned += OnLootSpawned;
            LootTables.OnLootSpawned += OnDungeonLootSpawned;
            EnemyDeath.OnEnemyDeath += OnEnemyDeath;

            SaveLoadManager.OnLoad += OnLoad;
        }

        public static void OnLoad(SaveData_v1 saveData)
        {
            if (Instance.initializeOnLoad)
                Instance.ConvertAllArmor();
        }

        private void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            if (change.HasChanged("Initialization"))
            {
                initializeOnLoad = settings.GetValue<bool>("Initialization", "InitializeOnLoad");
                feedbackMessage = settings.GetValue<bool>("Initialization", "ShowMessages");
            }

            if (change.HasChanged("Replacement"))
            {
                convertTo = settings.GetValue<int>("Replacement", "ConvertInventory");
            }

            if (change.HasChanged("Textures"))
            {
                textureArchive = settings.GetValue<int>("Textures", "TextureArchive");
            }

            if (change.HasChanged("Types"))
            {
                armorIron = settings.GetValue<int>("Types", "Iron");
                armorSteel = settings.GetValue<int>("Types", "Steel");
                armorSilver = settings.GetValue<int>("Types", "Silver");
                armorElven = settings.GetValue<int>("Types", "Elven");
                armorDwarven = settings.GetValue<int>("Types", "Dwarven");
                armorMithril = settings.GetValue<int>("Types", "Mithril");
                armorAdamantium = settings.GetValue<int>("Types", "Adamantium");
                armorEbony = settings.GetValue<int>("Types", "Ebony");
                armorOrcish = settings.GetValue<int>("Types", "Orcish");
                armorDaedric = settings.GetValue<int>("Types", "Daedric");
            }

            if (change.HasChanged("Names"))
            {
                nameCuirassLeather = settings.GetString("Names", "LeatherCuirass");
                nameGauntletsLeather = settings.GetString("Names", "LeatherGauntlets");
                nameGreavesLeather = settings.GetString("Names", "LeatherGreaves");
                namePauldronLeather = settings.GetString("Names", "LeatherPauldron");
                nameHelmLeather = settings.GetString("Names", "LeatherHelm");
                nameBootsLeather = settings.GetString("Names", "LeatherBoots");
                nameCuirassChain = settings.GetString("Names", "ChainCuirass");
                nameGauntletsChain = settings.GetString("Names", "ChainGauntlets");
                nameGreavesChain = settings.GetString("Names", "ChainGreaves");
                namePauldronChain = settings.GetString("Names", "ChainPauldron");
                nameHelmChain = settings.GetString("Names", "ChainHelm");
                nameBootsChain = settings.GetString("Names", "ChainBoots");
                nameCuirassPlate = settings.GetString("Names", "PlateCuirass");
                nameGauntletsPlate = settings.GetString("Names", "PlateGauntlets");
                nameGreavesPlate = settings.GetString("Names", "PlateGreaves");
                namePauldronPlate = settings.GetString("Names", "PlatePauldron");
                nameHelmPlate = settings.GetString("Names", "PlateHelm");
                nameBootsPlate = settings.GetString("Names", "PlateBoots");
            }

            if (change.HasChanged("Replacement"))
                ConvertAllArmor();
            else if (change.HasChanged("Textures") || change.HasChanged("Types") || change.HasChanged("Names"))
                UpdateAllArmor();

        }

        public static void OnLootSpawned(object sender, ContainerLootSpawnedEventArgs e)
        {
            if (Instance.convertTo == 1)
                Instance.ReplaceVanillaArmor(e.Loot);
        }

        public static void OnDungeonLootSpawned(object sender, TabledLootSpawnedEventArgs e)
        {
            if (Instance.convertTo == 1)
                Instance.ReplaceVanillaArmor(e.Items);
        }

        public static void OnEnemyDeath(object sender, EventArgs e)
        {
            EnemyDeath enemyDeath = sender as EnemyDeath;

            if (enemyDeath != null)
            {
                DaggerfallEntityBehaviour entityBehaviour = enemyDeath.GetComponent<DaggerfallEntityBehaviour>();
                if (entityBehaviour != null)
                {
                    EnemyEntity enemyEntity = entityBehaviour.Entity as EnemyEntity;
                    if (enemyEntity != null)
                    {
                        if (entityBehaviour.CorpseLootContainer != null)
                        {
                            ItemCollection items = entityBehaviour.CorpseLootContainer.Items;
                            if (items != null)
                            {
                                Instance.ReplaceVanillaArmor(items);
                            }
                        }
                    }
                }
            }
        }

        public void ConvertAllArmor()
        {
            if (convertTo == 1)
            {
                if (feedbackMessage)
                    DaggerfallUI.Instance.PopupMessage("Replacing vanilla armor");

                //change textures of existing modded armors
                UpdateCustomArmor(GameManager.Instance.PlayerEntity.Items, true);
                //replace existing vanilla armor with modded
                ReplaceVanillaArmor(GameManager.Instance.PlayerEntity.Items, true);
                ReplaceVanillaArmor(GameManager.Instance.PlayerEntity.WagonItems);
                DaggerfallLoot[] loots = GameObject.FindObjectsOfType<DaggerfallLoot>();
                if (loots.Length > 0)
                {
                    foreach (DaggerfallLoot loot in loots)
                    {
                        if (loot.Items.Count > 0)
                        {
                            //change textures of existing modded armors
                            UpdateCustomArmor(loot.Items);
                            ReplaceVanillaArmor(loot.Items);
                        }
                    }
                }
            }
            else if (convertTo == 0)
            {
                if (feedbackMessage)
                    DaggerfallUI.Instance.PopupMessage("Restoring vanilla armor");

                //replace existing modded armor with vanilla
                ReplaceCustomArmor(GameManager.Instance.PlayerEntity.Items, true);
                ReplaceCustomArmor(GameManager.Instance.PlayerEntity.WagonItems);
                DaggerfallLoot[] loots = GameObject.FindObjectsOfType<DaggerfallLoot>();
                if (loots.Length > 0)
                {
                    foreach (DaggerfallLoot loot in loots)
                    {
                        if (loot.Items.Count > 0)
                            ReplaceCustomArmor(loot.Items);
                    }
                }
            }
        }
        public void UpdateAllArmor()
        {
            if (feedbackMessage)
                DaggerfallUI.Instance.PopupMessage("Updating armor textures");

            //change textures of existing modded armors
            UpdateCustomArmor(GameManager.Instance.PlayerEntity.Items, true);
            UpdateCustomArmor(GameManager.Instance.PlayerEntity.WagonItems);
            DaggerfallLoot[] loots = GameObject.FindObjectsOfType<DaggerfallLoot>();
            if (loots.Length > 0)
            {
                foreach (DaggerfallLoot loot in loots)
                {
                    if (loot.Items.Count > 0)
                        UpdateCustomArmor(loot.Items);
                }
            }
        }

        void ReplaceVanillaArmor(ItemCollection collection, bool inventory = false)
        {
            if (collection.Count < 1)
                return;

            List<DaggerfallUnityItem> itemsToRemove = new List<DaggerfallUnityItem>();

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            for (int i = 0; i < collection.Count; i++)
            {
                DaggerfallUnityItem item = collection.GetItem(i);

                if (item.IsArtifact)
                    continue;

                int templateIndex = item.TemplateIndex;

                //check if vanilla armor
                if (templateIndex >= 102 && templateIndex <= 108)
                {
                    itemsToRemove.Add(item);

                    DaggerfallUnityItem newItem = null;

                    bool equipped = item.IsEquipped;

                    switch (templateIndex)
                    {
                        case 102:
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 600);
                            break;  //cuirass
                        case 103:
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 601);
                            break;  //gauntlets
                        case 104:
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 602);
                            break;  //greaves
                        case 105:
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 603);
                            break;  //left pauldron
                        case 106:
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 604);
                            break;  //right pauldron
                        case 107:
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 605);
                            break;  //helm
                        case 108:
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 606);
                            break;  //boots
                    }

                    if (newItem != null)
                    {
                        //save the variant
                        ItemCuirass hauberk = newItem as ItemCuirass;
                        ItemGauntlets gauntlets = newItem as ItemGauntlets;
                        ItemGreaves greaves = newItem as ItemGreaves;
                        ItemPauldronLeft leftPauldron = newItem as ItemPauldronLeft;
                        ItemPauldronRight rightPauldron = newItem as ItemPauldronRight;
                        ItemHelm helm = newItem as ItemHelm;
                        ItemBoots boots = newItem as ItemBoots;

                        if (hauberk != null)
                            hauberk.message = item.CurrentVariant;
                        if (gauntlets != null)
                            gauntlets.message = item.CurrentVariant;
                        if (greaves != null)
                            greaves.message = item.CurrentVariant;
                        if (leftPauldron != null)
                            leftPauldron.message = item.CurrentVariant;
                        if (rightPauldron != null)
                            rightPauldron.message = item.CurrentVariant;
                        if (helm != null)
                            helm.message = item.CurrentVariant;
                        if (boots != null)
                            boots.message = item.CurrentVariant;

                        //Debug.Log(item.ItemName + " variant is " + item.CurrentVariant.ToString());

                        ItemBuilder.ApplyArmorSettings(newItem, player.Gender, player.Race, (ArmorMaterialTypes)item.nativeMaterialValue,item.CurrentVariant);
                        //newItem.LowerCondition(item.maxCondition-item.currentCondition);
                        //newItem.currentCondition = newItem.maxCondition * (item.currentCondition/item.maxCondition);
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
                        {
                            //player.ItemEquipTable.UnequipItem(item);
                            player.ItemEquipTable.EquipItem(newItem,true);
                        }
                    }
                }
            }
            if (itemsToRemove.Count > 0)
            {
                foreach (DaggerfallUnityItem item in itemsToRemove)
                    collection.RemoveItem(item);
            }
        }

        //Do this before uninstalling
        void ReplaceCustomArmor(ItemCollection collection, bool inventory = false)
        {
            if (collection.Count < 1)
                return;

            List<DaggerfallUnityItem> itemsToRemove = new List<DaggerfallUnityItem>();

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            for (int i = 0; i < collection.Count; i++)
            {
                DaggerfallUnityItem item = collection.GetItem(i);

                if (item.IsArtifact)
                    continue;

                int templateIndex = item.TemplateIndex;

                //check if custom armor
                if (templateIndex >= 600 && templateIndex <= 606)
                {
                    itemsToRemove.Add(item);

                    DaggerfallUnityItem newItem = null;

                    bool equipped = item.IsEquipped;

                    switch (templateIndex)
                    {
                        case 600:
                            //newItem = ItemBuilder.CreateArmor(player.Gender, player.Race, Armor.Cuirass, (ArmorMaterialTypes)item.nativeMaterialValue);
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor,102);
                            break;  //cuirass
                        case 601:
                            //newItem = ItemBuilder.CreateArmor(player.Gender, player.Race, Armor.Gauntlets, (ArmorMaterialTypes)item.nativeMaterialValue);
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 103);
                            break;  //gauntlets
                        case 602:
                            //newItem = ItemBuilder.CreateArmor(player.Gender, player.Race, Armor.Greaves, (ArmorMaterialTypes)item.nativeMaterialValue);
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 104);
                            break;  //greaves
                        case 603:
                            //newItem = ItemBuilder.CreateArmor(player.Gender, player.Race, Armor.Left_Pauldron, (ArmorMaterialTypes)item.nativeMaterialValue);
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 105);
                            break;  //left pauldron
                        case 604:
                            //newItem = ItemBuilder.CreateArmor(player.Gender, player.Race, Armor.Right_Pauldron, (ArmorMaterialTypes)item.nativeMaterialValue);
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 106);
                            break;  //right pauldron
                        case 605:
                            //newItem = ItemBuilder.CreateArmor(player.Gender, player.Race, Armor.Helm, (ArmorMaterialTypes)item.nativeMaterialValue);
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 107);
                            break;  //helm
                        case 606:
                            //newItem = ItemBuilder.CreateArmor(player.Gender, player.Race, Armor.Boots, (ArmorMaterialTypes)item.nativeMaterialValue);
                            newItem = ItemBuilder.CreateItem(ItemGroups.Armor, 108);
                            break;  //boots
                    }

                    if (newItem != null)
                    {
                        //set the variant
                        int variant = -1;
                        ItemCuirass hauberk = item as ItemCuirass;
                        ItemGauntlets gauntlets = item as ItemGauntlets;
                        ItemGreaves greaves = item as ItemGreaves;
                        ItemPauldronLeft leftPauldron = item as ItemPauldronLeft;
                        ItemPauldronRight rightPauldron = item as ItemPauldronRight;
                        ItemHelm helm = item as ItemHelm;
                        ItemBoots boots = item as ItemBoots;

                        if (hauberk != null)
                            variant = hauberk.message;
                        if (gauntlets != null)
                            variant = gauntlets.message;
                        if (greaves != null)
                            variant = greaves.message;
                        if (leftPauldron != null)
                            variant = leftPauldron.message;
                        if (rightPauldron != null)
                            variant = rightPauldron.message;
                        if (helm != null)
                            variant = helm.message;
                        if (boots != null)
                            variant = boots.message;

                        //Debug.Log(newItem.ItemName + " variant is " + variant.ToString());

                        ItemBuilder.ApplyArmorSettings(newItem, player.Gender, player.Race, (ArmorMaterialTypes)item.nativeMaterialValue, variant);

                        //newItem.LowerCondition(item.maxCondition - item.currentCondition);
                        //newItem.currentCondition = newItem.maxCondition * (item.currentCondition / item.maxCondition);
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
                            player.ItemEquipTable.EquipItem(newItem,true);
                    }
                }
            }
            if (itemsToRemove.Count > 0)
            {
                foreach (DaggerfallUnityItem item in itemsToRemove)
                    collection.RemoveItem(item);
            }
        }
        void UpdateCustomArmor(ItemCollection collection, bool inventory = false)
        {
            if (collection.Count < 1)
                return;

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            for (int i = 0; i < collection.Count; i++)
            {
                DaggerfallUnityItem item = collection.GetItem(i);

                int templateIndex = item.TemplateIndex;

                //check if custom armor
                if (templateIndex >= 600 && templateIndex <= 606)
                {
                    //set the variant
                    int variant = -1;
                    ItemCuirass hauberk = item as ItemCuirass;
                    ItemGauntlets gauntlets = item as ItemGauntlets;
                    ItemGreaves greaves = item as ItemGreaves;
                    ItemPauldronLeft leftPauldron = item as ItemPauldronLeft;
                    ItemPauldronRight rightPauldron = item as ItemPauldronRight;
                    ItemHelm helm = item as ItemHelm;
                    ItemBoots boots = item as ItemBoots;

                    if (hauberk != null)
                        variant = hauberk.message;
                    if (gauntlets != null)
                        variant = gauntlets.message;
                    if (greaves != null)
                        variant = greaves.message;
                    if (leftPauldron != null)
                        variant = leftPauldron.message;
                    if (rightPauldron != null)
                        variant = rightPauldron.message;
                    if (helm != null)
                        variant = helm.message;
                    if (boots != null)
                        variant = boots.message;

                    //item.SetItem(ItemGroups.None,templateIndex);
                    //ItemBuilder.ApplyArmorSettings(item, player.Gender, player.Race, (ArmorMaterialTypes)item.nativeMaterialValue, variant);

                    item.CurrentVariant = variant;
                }
            }
        }
    }
}
