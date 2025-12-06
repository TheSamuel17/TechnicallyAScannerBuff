using BepInEx;
using RoR2;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TimedSeeSChest
{
    // Dependencies
    [BepInDependency(PrefabAPI.PluginGUID)]

    // Metadata
    [BepInPlugin("Samuel17.TechnicallyAScannerBuff", "TechnicallyAScannerBuff", "1.0.0")]

    public class Main : BaseUnityPlugin
    {
        // Fields
        public GameObject newCloakedChestPrefab;
        public InteractableSpawnCard iscChest2Stealthed;

        // Config fields
        public static int chestCount = 1;

        // Load addressables
        public static GameObject cloakedChestPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1StealthedVariant/Chest1StealthedVariant.prefab").WaitForCompletion();
        public static BasicPickupDropTable largeChestDropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Chest2/dtChest2.asset").WaitForCompletion();
        public static InteractableSpawnCard iscChest1Stealthed = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Chest1StealthedVariant/iscChest1Stealthed.asset").WaitForCompletion();

        public void Awake()
        {
            // Logging!
            Log.Init(Logger);

            // Configs
            chestCount = Config.Bind("Cloaked Chests", "Chest Count", 1, "The amount of guaranteed Cloaked Chests per stage.").Value;

            // Create new Cloaked Chest type
            SetupNewCloakedChest();

            // Populate stages with it
            On.RoR2.SceneDirector.PopulateScene += SpawnCloakedChest;
        }

        private void SetupNewCloakedChest()
        {
            // Clone a new prefab
            newCloakedChestPrefab = PrefabAPI.InstantiateClone(cloakedChestPrefab, "Chest2StealthedVariant");

            // Set its loot table to match a Large Chest
            newCloakedChestPrefab.GetComponent<ChestBehavior>().dropTable = largeChestDropTable;

            // Prevent it from being locked during the Teleporter event. Yes we're gonna need a wholeass hook for this.
            newCloakedChestPrefab.GetComponent<PurchaseInteraction>().setUnavailableOnTeleporterActivated = false;
            On.RoR2.PurchaseInteraction.IsLockable += UnlockNewCloakedChest;

            // Clone a new spawn card
            iscChest2Stealthed = Instantiate(iscChest1Stealthed);
            iscChest2Stealthed.name = "iscChest2Stealthed";
            iscChest2Stealthed.prefab = newCloakedChestPrefab;
        }

        private void SpawnCloakedChest(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            orig(self);

            if (!SceneInfo.instance.countsAsStage && !SceneInfo.instance.sceneDef.allowItemsToSpawnObjects)
            {
                return;
            }

            for (int j = 0; j < chestCount; j++)
            {
                Xoroshiro128Plus xoroshiro128Plus = new Xoroshiro128Plus(self.rng.nextUlong);
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
