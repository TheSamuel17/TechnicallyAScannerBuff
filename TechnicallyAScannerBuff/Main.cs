using BepInEx;
using RoR2;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace TimedSeeSChest
{
    // Dependencies
    [BepInDependency(PrefabAPI.PluginGUID)]

    // Metadata
    [BepInPlugin("Samuel17.TechnicallyAScannerBuff", "TechnicallyAScannerBuff", "1.0.1")]

    public class Main : BaseUnityPlugin
    {
        // Fields
        public static GameObject newCloakedChestPrefab;
        public static InteractableSpawnCard iscChest2Stealthed;
        public static bool scannerUsedThisStage = false;
        public static SceneDirector currentSceneDirector;

        // Config fields
        public static ConfigEntry<int> chestCount { get; private set; }
        public static ConfigEntry<bool> largeRarity { get; private set; }
        public static ConfigEntry<bool> scannerRequired { get; private set; }

        // Load addressables
        public static GameObject cloakedChestPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1StealthedVariant/Chest1StealthedVariant.prefab").WaitForCompletion();
        public static BasicPickupDropTable largeChestDropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Chest2/dtChest2.asset").WaitForCompletion();
        public static InteractableSpawnCard iscChest1Stealthed = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Chest1StealthedVariant/iscChest1Stealthed.asset").WaitForCompletion();

        public void Awake()
        {
            // Logging!
            Log.Init(Logger);

            // Configs
            chestCount = Config.Bind("Cloaked Chests", "Chest Count", 1, "The amount of guaranteed Cloaked Chests per stage.");
            largeRarity = Config.Bind("Cloaked Chests", "Improved Item Rarity", true, "Item rarity is equal to a Large Chest. False makes it equal to a regular Chest.");
            scannerRequired = Config.Bind("Cloaked Chests", "Scanner is Required", false, "If set to true, the guaranteed Cloaked Chests can only be found by activating the Radar Scanner at least once.");

            // Create new Cloaked Chest type
            SetupNewCloakedChest();

            // Populate stages with it
            SceneDirector.onPostPopulateSceneServer += StageStart;
            if (scannerRequired.Value) { On.RoR2.EquipmentSlot.FireScanner += SpawnCloakedChestWithRadar; } 
        }

        private void SetupNewCloakedChest()
        {
            // Clone a new prefab
            newCloakedChestPrefab = PrefabAPI.InstantiateClone(cloakedChestPrefab, "Chest2StealthedVariant");

            // Set its loot table to match a Large Chest
            if (largeRarity.Value)
            {
                newCloakedChestPrefab.GetComponent<ChestBehavior>().dropTable = largeChestDropTable;
            }

            // Prevent it from being locked during the Teleporter event. Yes we're gonna need a wholeass hook for this.
            newCloakedChestPrefab.GetComponent<PurchaseInteraction>().setUnavailableOnTeleporterActivated = false;
            On.RoR2.PurchaseInteraction.IsLockable += UnlockNewCloakedChest;

            // Clone a new spawn card
            iscChest2Stealthed = Instantiate(iscChest1Stealthed);
            iscChest2Stealthed.name = "iscChest2Stealthed";
            iscChest2Stealthed.prefab = newCloakedChestPrefab;
        }

        private void StageStart(SceneDirector sceneDirector)
        {
            currentSceneDirector = sceneDirector;
            scannerUsedThisStage = false;

            if (!scannerRequired.Value)
            {
                SpawnCloakedChest(sceneDirector);
            }
        }

        private bool SpawnCloakedChestWithRadar(On.RoR2.EquipmentSlot.orig_FireScanner orig, EquipmentSlot self)
        {
            if (currentSceneDirector && !scannerUsedThisStage)
            {
                scannerUsedThisStage = true;
                SpawnCloakedChest(currentSceneDirector);
            }
            return orig(self);
        }

        private void SpawnCloakedChest(SceneDirector sceneDirector)
        {
            if (!SceneInfo.instance.countsAsStage && !SceneInfo.instance.sceneDef.allowItemsToSpawnObjects)
            {
                return;
            }

            for (int j = 0; j < chestCount.Value; j++)
            {
                Xoroshiro128Plus xoroshiro128Plus = new Xoroshiro128Plus(sceneDirector.rng.nextUlong);
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(iscChest2Stealthed, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                }, xoroshiro128Plus));
            }
        }

        private bool UnlockNewCloakedChest(On.RoR2.PurchaseInteraction.orig_IsLockable orig, PurchaseInteraction self)
        {
            if (self.gameObject.name == "Chest2StealthedVariant(Clone)") {
                return false;
            } else {
                return orig(self);
            }
        }
    }
}
