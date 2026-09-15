using BobosWorld;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Enums;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoboBayArchipelago
{
    [HarmonyPatch(typeof(CompetitionSO), "Unlocked")]
    public static class CompetitionUnlockPatch
    {
        
        // goals will go here
        private static readonly HashSet<string> GoalCompetitionAssetNames = new HashSet<string>
        {
            "BigJam_Race_D",
        };

        [HarmonyPostfix]
        public static void Postfix(CompetitionSO __instance, ref bool __result)
        {
            if (__instance == null) return;

            if (GoalCompetitionAssetNames.Contains(__instance.name))
            {
                __result = ArchipelagoItemHandler.BoboTicketsReceived >= ArchipelagoItemHandler.BoboTicketsRequired;
                return;
            }
            if (ArchipelagoItemHandler.CompetitionUnlockThresholds.TryGetValue(__instance.name, out int required))
            {
                __result = ArchipelagoItemHandler.ProgressiveCompetitionsReceived >= required;
                return;
            }

            if (__instance.rank == Grade.E){ __result = true; }
            else if (__instance.rank == Grade.D){ __result = ArchipelagoItemHandler.DRankUnlocked; }
        }
    }

    [HarmonyPatch(typeof(CompetitionOrganizer), "SetTodaysCompetitions")]
    public static class CompetitionAvailabilityPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            foreach (var competition in Resources.FindObjectsOfTypeAll<CompetitionSO>())
            {
                if (competition == null) continue;

                bool forceAvailable;
                if (ArchipelagoItemHandler.CompetitionUnlockThresholds.TryGetValue(competition.name, out int required))
                {
                    forceAvailable = ArchipelagoItemHandler.ProgressiveCompetitionsReceived >= required;
                }
                else
                {
                    forceAvailable = competition.rank == Grade.E
                        || (competition.rank == Grade.D && ArchipelagoItemHandler.DRankUnlocked);
                }

                if (!forceAvailable) continue;

                competition.dayMonday = true;
                competition.dayTuesday = true;
                competition.dayWednesday = true;
                competition.dayThursday = true;
                competition.dayFriday = true;
                competition.daySaturday = true;
                competition.daySunday = true;
                competition.seasonSpring = true;
                competition.seasonSummer = true;
                competition.seasonFall = true;
                competition.seasonWinter = true;

                if (competition.days == null)
                    competition.days = new List<DaysByWeek>();
                foreach (DaysByWeek day in Enum.GetValues(typeof(DaysByWeek)))
                {
                    if (!competition.days.Contains(day))
                        competition.days.Add(day);
                }

                if (competition.seasons == null)
                    competition.seasons = new List<NFKUtilities.Season>();
                foreach (NFKUtilities.Season season in Enum.GetValues(typeof(NFKUtilities.Season)))
                {
                    if (!competition.seasons.Contains(season))
                        competition.seasons.Add(season);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Bobo), "UpdateStatPoints")]
    public static class SnackStatMultiplierPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref float multiplier)
        {
            if (ArchipelagoItemHandler.CurrentSnackMultiplier > 1f)
            {
                multiplier *= ArchipelagoItemHandler.CurrentSnackMultiplier;
            }
        }
    }
    
    [HarmonyPatch(typeof(BoboData), "NewFullDay")]
    public static class UnlimitedSnacksPatch
    {
        [HarmonyPostfix]
        static void Postfix(BoboData __instance)
        {
            if (!ArchipelagoItemHandler.UnlimitedSnacksEnabled) return;
            __instance.FoodMod = 999;
        }
    }

    [HarmonyPatch(typeof(CompetitionManager), "EndEvent")]
    public static class CompetitionPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CompetitionManager __instance)
        {
            var trav = Traverse.Create(__instance);
            var currentCompSO = trav.Field("_currentCompetitionSO").GetValue();
            if (currentCompSO == null) { Plugin.Log?.LogInfo("[APDebug] _currentCompetitionSO is null"); return; }

            // calculate location ID based on competition SO
            // example: AP loc ID = 20000 + CompID
            var innerSO = Traverse.Create(__instance)
                .Field("_currentCompetitionSO")
                .Field("so")
                .GetValue();

            if (innerSO == null) return;
            
            Plugin.Log?.LogInfo($"[APDebug] Competition asset name: '{currentCompSO}'");
            Plugin.Log?.LogInfo($"[APDebug] Inner SO: '{innerSO}' (type: {innerSO.GetType().Name})");
            foreach (var f in innerSO.GetType().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                try { Plugin.Log?.LogInfo($"[APDebug]   {f.Name} = {f.GetValue(innerSO)}"); }
                catch { }
            }

            string soName = (string)typeof(UnityEngine.Object)
                .GetProperty("name")
                .GetValue(innerSO);
            
            if (!CompetitionLocationIDs.TryGetValue(soName, out long locationID))
            {
                Plugin.Log?.LogWarning($"[AP] No location ID mapped for competition: '{soName}'");
                return;
            }
            Plugin.Log?.LogInfo($"[AP] Competition '{soName}' -> location {locationID}");
            ArchipelagoManager.CheckLocation(locationID);

            if (locationID == 20050125) // bigjam
            {
                ArchipelagoManager.SendGoalComplete();
            }
        }
        public static readonly Dictionary<string, long> CompetitionLocationIDs = new Dictionary<string, long>
        {
            { "BabysFirstSteps_Race_E", 20050100 },
            { "LetsTryClimbing_Race_E", 20050101 },
            { "ICanClimbThat_Race_E", 20050102 },
            { "UpToTheMoon_Race_E", 20050103 },
            { "TheNewMe_Race_E", 20050104 },
            { "CometOClock_Race_E", 20050105 },
            { "SwimLessons_Race_E", 20050106 },
            { "StillCrawling_Race_E", 20050107 },
            { "BiggerWaterTrain_Race_E", 20050108 },
            { "LittleWaterTrain_Race_E", 20050109 },
            { "Qwench_Race_E", 20050110 },
            { "InTheDrink_Brawl_E", 20050111 },
            { "PunchTheBaby_Brawl_E", 20050112 },
            { "BeamWithAView_Race_D", 20050113 },
            { "WalkAlongHigh_Race_D", 20050114 },
            { "ChannelIt_Race_D", 20050115 },
            { "TestTheJump_Race_D", 20050116 },
            { "PunchIt_Race_D", 20050117 },
            { "SplishBlock_Race_D", 20050118 },
            { "LittleLongerNow_Race_D", 20050119 },
            { "UndertheScramble_Brawl_D", 20050120 },
            { "Double Kee Laps_Race_D", 20050121 },
            { "VerticalDoggyPaddle_Race_D", 20050122 },
            { "WaveyBaby_Race_D", 20050123 },
            { "Woovy_Race_D", 20050124 },
            { "BigJam_Race_D", 20050125 },
            { "Beefy_Brawl_D", 20050126 },
            { "Pow_Brawl_D", 20050127 },
            { "Muckula_Race_D", 20050128 },
            { "MiniMatch_Brawl_D", 20050129 },
            { "YouJumpIClimb_Race_D", 20050130 },
            { "CrawfishCookie_Brawl_D", 20050131 },
            { "BoyAlphard_Race_D", 20050132 },
            { "Pumpkick_Brawl_D", 20050133 },
            { "Drippy_Brawl_D", 20050134 },
            { "TakeMeOut_Brawl_D", 20050135 }
            // to add more here
        };
    }
    
    [HarmonyPatch(typeof(GardenBoboAITree))]
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.bobobay.archipelago";
        public const string PluginName = "BoboBay.Archipelago";
        public const string PluginVersion = "1.0.0";

        // Persistent static logging and configuration references
        public static ManualLogSource Log { get; private set; }
        public static ConfigFile ConfigFile { get; private set; }

        // BepInEx Configuration Entries
        public static ConfigEntry<string> ServerAddressEntry;
        public static ConfigEntry<string> SlotNameEntry;
        public static ConfigEntry<string> PasswordEntry;
        public static ConfigEntry<bool> AutoConnectEntry;
        public static ConfigEntry<int> ItemsGrantedIndexEntry;
        public static ConfigEntry<bool> DRankUnlockedEntry;
        public static ConfigEntry<int> BoboTicketsReceivedEntry;
        public static ConfigEntry<int> BoboTicketsRequiredEntry;
        public static ConfigEntry<int> ProgressiveCompetitionsReceivedEntry;
        public static ConfigEntry<string> LastSeedEntry;
        private static bool _foodModPending = false;
        
        public static ConfigEntryBase[] BoboManagerSettings;

        private void Awake()
        {
            
            Log = Logger;
            ConfigFile = Config;

            // Bind configuration settings
            ServerAddressEntry = Config.Bind("Archipelago", "ServerAddress", "archipelago.gg:38281", "Host address and port for the Archipelago server.");
            SlotNameEntry = Config.Bind("Archipelago", "SlotName", "Player", "Slot name registered in the Archipelago multiworld.");
            PasswordEntry = Config.Bind("Archipelago", "Password", "", "Password for the room (if required).");
            AutoConnectEntry = Config.Bind("Archipelago", "AutoConnect", false, "Automatically attempt connection on startup.");
            ProgressiveCompetitionsReceivedEntry = Config.Bind("Archipelago", "ProgressiveCompetitionsReceived", 0,
                "Internal: how many Progressive Competitions items received so far.");
            LastSeedEntry = Config.Bind("Archipelago", "LastSeed", "", "Internal: tracks the last connected seed, to auto-reset progress counters on a new seed.");
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Expose settings array for dynamic UI generation
            BoboManagerSettings = new ConfigEntryBase[]
            {
                ServerAddressEntry,
                SlotNameEntry,
                PasswordEntry,
                AutoConnectEntry
            };

            // Apply Harmony patches
            Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly(), PluginGuid);

            // Hook static handlers to scene events (survives MonoBehaviour destruction)
            SceneManager.sceneLoaded += OnSceneLoaded;

            BoboTicketsRequiredEntry = Config.Bind("Archipelago", "BoboTicketsRequiredEntry", 3,
                "Internal: how many bobo tickets unlock the goal competition.");

            Log.LogInfo($"{PluginName} v{PluginVersion} initialized successfully.");
        }

        private void Update()
        {
            if (ArchipelagoManager.IsConnected && ArchipelagoManager.IsInBay)
            {
                ArchipelagoManager.ProcessPendingItems();
            }

            if (_foodModPending && ArchipelagoManager.IsInBay){
                if (ArchipelagoItemHandler.ApplyFoodModToCurrentGarden() > 0){
                    _foodModPending = false;
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log?.LogInfo($"Archipelago: Scene loaded '{scene.name}' (Mode: {mode}).");

            if (scene.name == "Bay")
            {
                ArchipelagoManager.IsInBay = true;
                Log?.LogInfo($"Archipelago: Bay loaded, IsInBay = true.");
                if (ArchipelagoItemHandler.UnlimitedSnacksEnabled) _foodModPending = true;
            }

            // Example trigger: Auto-connect on the main menu or initial world load
            if (AutoConnectEntry.Value && !ArchipelagoManager.IsConnected)
            {
                ArchipelagoManager.Connect();
            }
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == "Bay")
            {
                ArchipelagoManager.IsInBay = false;
                Log?.LogInfo($"Archipelago: Bay unloaded, IsInBay = false.");
            } 
        }

        private static void DumpItemSpawnMembers()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            void Dump(Type t, string nameFilter)
            {
                foreach (var m in t.GetMethods(flags))
                {
                    if (m.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (m.IsSpecialName) continue;
                    var ps = string.Join(", ", Array.ConvertAll(m.GetParameters(),
                        p => p.ParameterType.Name + " " + p.Name));
                    Log.LogInfo($"METHOD  {t.Name}.{m.Name}({ps}) -> {m.ReturnType.Name}");
                }
            }

            Dump(typeof(Item), "Spawn");
            Dump(typeof(Item), "Despawn");
            Dump(typeof(Garden), "ItemInGarden");
        }
    }

    public static class ArchipelagoItemHandler
    {
        public static ConfigEntry<int> BoboTicketsReceivedEntry;
        public static ConfigEntry<int> ProgressiveCompetitionsReceivedEntry;
        public static ConfigEntry<bool> DRankUnlockedEntry;
        public static ConfigEntry<int> ItemsGrantedIndexEntry;
        public const long DRankUnlockItemId = 20050050;
        public const long ProgressiveCompetitionsId = 20050051;
        public const long BoboTicketId = 20050000;
        public static bool DRankUnlocked => Plugin.DRankUnlockedEntry?.Value ?? false;
        public static int BoboTicketsReceived => Plugin.BoboTicketsReceivedEntry?.Value ?? 0;
        public static int BoboTicketsRequired => Plugin.BoboTicketsRequiredEntry?.Value ?? 3;

        public static Dictionary<string, int> CompetitionUnlockThresholds = new Dictionary<string, int>();
        public static int ProgressiveCompetitionsReceived => Plugin.ProgressiveCompetitionsReceivedEntry?.Value ?? 0;

        public static void BindProgressEntriesForSeed(string seed)
        {
            string safeSeed = new string(seed.Where(c => char.IsLetterOrDigit(c)).ToArray());
            if (string.IsNullOrEmpty(safeSeed)) safeSeed = "unknown";
            string section = "Progress_" + safeSeed;

            BoboTicketsReceivedEntry = Plugin.ConfigFile.Bind(section, "BoboTicketsReceived", 0,
                "Progress for this specific seed — how many Bobo Tickets received.");
            ProgressiveCompetitionsReceivedEntry = Plugin.ConfigFile.Bind(section, "ProgressiveCompetitionsReceived", 0,
                "Progress for this specific seed — how many Progressive Competitions received.");
            DRankUnlockedEntry = Plugin.ConfigFile.Bind(section, "DRankUnlocked", false,
                "Progress for this specific seed — legacy D-rank unlock flag.");
            ItemsGrantedIndexEntry = Plugin.ConfigFile.Bind(section, "ItemsGrantedIndex", 0,
                "Progress for this specific seed — count of AP items already granted.");

            Plugin.Log?.LogInfo($"[Archipelago] Bound progress entries for seed section '{section}'.");
        }

        public static void UnlockDRank()
        {
            if (Plugin.DRankUnlockedEntry != null)
                Plugin.DRankUnlockedEntry.Value = true;

            Plugin.Log?.LogInfo("[Archipelago] D-rank competitions unlocked.");
        }

        public static void GrantProgressiveCompetitions()
        {
            // want to convert from unlocking competitions by rank
            // to unlocking sets of competitions to stagger the progression
            if (Plugin.ProgressiveCompetitionsReceivedEntry != null)
            Plugin.ProgressiveCompetitionsReceivedEntry.Value++;

            Plugin.Log?.LogInfo($"[Archipelago] Progressive Competitions received ({ProgressiveCompetitionsReceived}).");

        }
        public static void GrantBoboTicket()
        {
            if (Plugin.BoboTicketsReceivedEntry != null)
                Plugin.BoboTicketsReceivedEntry.Value++;

            Plugin.Log?.LogInfo($"[Archipelago] Bobo Ticket received ({BoboTicketsReceived}/{BoboTicketsRequired}).");
        }
        public static void SetBoboTicketsRequired(int required)
        {
            if (Plugin.BoboTicketsRequiredEntry != null)
                Plugin.BoboTicketsRequiredEntry.Value = required;
        }

        public static float CurrentSnackMultiplier { get; private set; } = 1f;
        public static bool UnlimitedSnacksEnabled { get; private set; } = false;
        private static readonly int BaseMinStat = Common.DEFAULT_MINSTATUPDATE;
        private static readonly int BaseMaxStat = Common.DEFAULT_MAXSTATUPDATE;

        public static void ApplySnackMultiplier(float multiplier)
        {
            if (multiplier <= 0f) multiplier = 1f;
            CurrentSnackMultiplier = multiplier;
            Common.DEFAULT_MINSTATUPDATE = (int)(BaseMinStat * multiplier);
            Common.DEFAULT_MAXSTATUPDATE = (int)(BaseMaxStat * multiplier);
            Plugin.Log?.LogInfo($"[Archipelago] Applied Snack Multiplier {multiplier}x: Min={Common.DEFAULT_MINSTATUPDATE}, Max={Common.DEFAULT_MAXSTATUPDATE}");
        }

        public static void SetUnlimitedSnacks(bool enabled)
        {
            UnlimitedSnacksEnabled = enabled;
            Plugin.Log?.LogInfo($"[Archipelago] Unlimited Snacks set to {enabled}");
        }

        public static readonly Dictionary<long, string> ItemAssetNames = new Dictionary<long, string>
        {
            { 20050001, "Leftover Pizza" },
            { 20050002, "Palmwelon" },
            { 20050003, "Gumball_ConcernedEyes" },
            { 20050004, "Goldenana" },
            { 20050005, "Bunny_Cracker" },
            { 20050006, "Skyberry" },
            { 20050007, "Juice_EnergyDrink_Hype" },
            { 20050008, "BusinessGlasses_Gold" },
            { 20050009, "SagaMedal_HotTop" },
            { 20050010, "DarkGlasses_Yellow" },
            { 20050011, "Shoes_A_White" },
            { 20050012, "SagaMedal_Gear" },
            { 20050013, "CoolHelmet_Secondary" },
            { 20050014, "SagaMedal_DashClassic" },
            { 20050015, "CopGlasses_Silver" },
            { 20050016, "KnitHat_Blue" },
            { 20050017, "TopHat_Big_Black" },
            { 20050018, "ToyItem_StuffedAnimal" },
            // Junk items
            // 150 money is 20050020
            { 20050021, "Gumball_ConcernedEyes" },
            { 20050022, "Skyberry" },
        };

        public static void SpawnItemByAsset(string assetName)
        {
            var itemSO = UnityEngine.Resources.FindObjectsOfTypeAll<ItemScriptableObject>()
                .FirstOrDefault(i => i.name == assetName);

            if (itemSO == null)
            {
                Plugin.Log?.LogWarning($"[Archipelago] Could not find ItemScriptableObject for: '{assetName}'");
                return;
            }

            var garden = BobosWorld.Garden.Current;
            if (garden == null)
            {
                Plugin.Log?.LogWarning($"[Archipelago] Garden.Current is null; couldn't queue item '{assetName}'.");
                return;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var itemInGardenType = typeof(BobosWorld.Garden).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "ItemInGarden");
            if (itemInGardenType == null)
            {
                Plugin.Log?.LogWarning("[Archipelago] Could not find type ItemInGarden.");
                return;
            }

            var ctor = itemInGardenType.GetConstructor(flags, null,
                new[] { typeof(ItemScriptableObject), typeof(Vector4), typeof(int) }, null);
            if (ctor == null)
            {
                Plugin.Log?.LogWarning("[Archipelago] Could not find ItemInGarden(ItemScriptableObject, Vector4, int) constructor.");
                return;
            }

            object itemInGarden = ctor.Invoke(new object[] { itemSO, Vector4.zero, 0 });

            var queueField = typeof(BobosWorld.Garden).GetField("_itemsToSpawnOnPlayer", flags);
            object queue = queueField?.GetValue(garden);
            if (queue == null)
            {
                Plugin.Log?.LogWarning("[Archipelago] Could not find _itemsToSpawnOnPlayer on Garden.Current.");
                return;
            }

            var addMethod = queue.GetType().GetMethod("Add", flags, null, new[] { itemInGardenType }, null);
            if (addMethod == null)
            {
                Plugin.Log?.LogWarning("[Archipelago] Could not find Add(ItemInGarden) on _itemsToSpawnOnPlayer.");
                return;
            }

            addMethod.Invoke(queue, new object[] { itemInGarden });

            Plugin.Log?.LogInfo($"[Archipelago] Queued item to spawn on player: '{itemSO.name}' ({itemSO.LocalizedName})");
        }

        public static int ApplyFoodModToCurrentGarden()
        {
            var garden = BobosWorld.Garden.Current;
            if (garden == null) return 0;

            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var controllerListField = typeof(BobosWorld.Garden).GetField("_controllerList", flags);
            var controllerList = controllerListField?.GetValue(garden) as System.Collections.IEnumerable;
            if (controllerList == null) return 0;
            
            int count = 0;
            foreach (var controller in controllerList)
            {
                count++;
                if (controller == null) { Plugin.Log?.LogWarning("[APDebug] FoodMod: null controller in list."); continue; }

                Plugin.Log?.LogInfo($"[APDebug] FoodMod: controller type = {controller.GetType().FullName}");

                var boboMember = controller.GetType().GetProperty("Bobo", flags) as MemberInfo
                            ?? controller.GetType().GetField("Bobo", flags);
                object bobo = boboMember == null ? null
                    : (boboMember is PropertyInfo bp ? bp.GetValue(controller) : ((FieldInfo)boboMember).GetValue(controller));

                if (bobo == null) { Plugin.Log?.LogWarning($"[APDebug] FoodMod: no 'Bobo' member found on {controller.GetType().Name}, or its value was null."); continue; }
                Plugin.Log?.LogInfo($"[APDebug] FoodMod: bobo type = {bobo.GetType().FullName}");

                var dataMember = bobo.GetType().GetProperty("Data", flags) as MemberInfo
                            ?? bobo.GetType().GetField("Data", flags);
                object data = dataMember == null ? null
                    : (dataMember is PropertyInfo dp ? dp.GetValue(bobo) : ((FieldInfo)dataMember).GetValue(bobo));

                if (data == null) { Plugin.Log?.LogWarning($"[APDebug] FoodMod: no 'Data' member found on {bobo.GetType().Name}, or its value was null."); continue; }
                Plugin.Log?.LogInfo($"[APDebug] FoodMod: data type = {data.GetType().FullName}");

                var foodModProp = data.GetType().GetProperty("FoodMod", flags);
                if (foodModProp == null) { Plugin.Log?.LogWarning($"[APDebug] FoodMod: no 'FoodMod' property on {data.GetType().Name}."); continue; }

                int before = (int)foodModProp.GetValue(data);
                foodModProp.SetValue(data, 999);
                int after = (int)foodModProp.GetValue(data);
                Plugin.Log?.LogInfo($"[APDebug] FoodMod: set on {bobo.GetType().Name} — before={before}, after={after}");
            }

            if (count > 0) Plugin.Log?.LogInfo($"[APDebug] FoodMod: applied to {count} controller(s).");
            return count;
        }

        private static void GrantMoney(int amount)
        {
            var save = SaveSystem.Instance;
            if (save == null)
            {
                Plugin.Log?.LogWarning("Archipelago: SaveSystem.Instance not ready yet, can't grant money.");
                return;
            }

            var moneyField = typeof(SaveSystem).GetField("_money", BindingFlags.NonPublic | BindingFlags.Instance);
            object moneyVar = moneyField?.GetValue(save);
            if (moneyVar == null)
            {
                Plugin.Log?.LogError("Archipelago: couldn't find _money field on SaveSystem.");
                return;
            }

            var addMethod = moneyVar.GetType().GetMethod("Add", new[] { typeof(int) });
            if (addMethod != null)
            {
                addMethod.Invoke(moneyVar, new object[] { amount });
            }
            else
            {
                var valueProp = moneyVar.GetType().GetProperty("Value");
                int current = (int)valueProp.GetValue(moneyVar);
                valueProp.SetValue(moneyVar, current + amount);
            }

            save.SaveMoney();
            Plugin.Log?.LogInfo($"Archipelago: granted {amount} bobo bucks.");
        }

        public static void GrantReceivedItem(long itemID)
        {
            if (ItemAssetNames.TryGetValue(itemID, out string assetName))
            {
                SpawnItemByAsset(assetName);
                return;
            }

            switch (itemID)
            {
                case ProgressiveCompetitionsId:
                    GrantProgressiveCompetitions();
                    break;
                case BoboTicketId:
                    GrantBoboTicket();
                    break;
                case 20050020:
                    GrantMoney(150);
                    break;
                case 20050099:
                    Plugin.Log?.LogInfo("[Archipelago] VICTORY item received! Congratulations!");
                    break;
                default:
                    Plugin.Log?.LogWarning($"[Archipelago] Received unmapped item ID: {itemID}");
                    break;
            }
        }
    }

    /// <summary>
    /// Static Network and Logic Manager for Archipelago.
    /// Operates independently of Unity MonoBehaviour lifecycles.
    /// </summary>
    public static class ArchipelagoManager
    {
        private static ArchipelagoSession _session;
        public static bool IsInBay { get;set; }

        public static void ProcessPendingItems()
        {
            if (_session == null) return;

            while (_session.Items.Any())
            {
                var item = _session.Items.DequeueItem();
                string itemName = _session.Items.GetItemName(item.ItemId) ?? $"Item {item.ItemId}";
                try
                {
                    Plugin.Log?.LogInfo($"[Archipelago] Processing received item, id={item.ItemId}");
                    ArchipelagoItemHandler.GrantReceivedItem(item.ItemId);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"[Archipelago] Exception while processing item {item.ItemId}: {ex}");
                }
            }
        }

        public static void SendGoalComplete()
        {
            if (_session == null || !IsConnected)
            {
                Plugin.Log?.LogWarning("[Archipelago] Tried to send goal completion while disconnected.");
                return;
            }

            var packet = new StatusUpdatePacket { Status = ArchipelagoClientState.ClientGoal };
            _session.Socket.SendPacket(packet);
            Plugin.Log?.LogInfo("[Archipelago] Sent goal completion (ClientGoal) to server.");
        }

        private static Dictionary<string, int> ParseThresholds(object raw)
        {
            var result = new Dictionary<string, int>();
            if (raw == null) return result;

            Plugin.Log?.LogInfo($"[APDebug] competition_unlock_thresholds runtime type: {raw.GetType().FullName}");

            if (raw is Newtonsoft.Json.Linq.JObject jObj)
            {
                foreach (var prop in jObj.Properties())
                    result[prop.Name] = prop.Value.ToObject<int>();
                return result;
            }

            if (raw is System.Collections.IDictionary dict)
            {
                foreach (System.Collections.DictionaryEntry entry in dict)
                    result[entry.Key.ToString()] = Convert.ToInt32(entry.Value);
                return result;
            }

            Plugin.Log?.LogWarning($"[Archipelago] Unrecognized threshold payload type: {raw.GetType().FullName}");
            return result;
        }

        private static void DumpSeedIdentifierCandidates(LoginSuccessful success)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            void DumpMembers(object obj, string label)
            {
                if (obj == null) { Plugin.Log?.LogWarning($"[APDebug] {label} is null."); return; }
                foreach (var p in obj.GetType().GetProperties(flags))
                {
                    if (p.Name.IndexOf("seed", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    try { Plugin.Log?.LogInfo($"[APDebug] {label}.{p.Name} = {p.GetValue(obj)}"); }
                    catch (Exception ex) { Plugin.Log?.LogInfo($"[APDebug] {label}.{p.Name} threw: {ex.Message}"); }
                }
            }

            DumpMembers(success, "LoginSuccessful");
            DumpMembers(_session?.RoomState, "RoomState");
        }

        public static bool IsConnected { get; private set; } = false;
        public static string StatusMessage { get; private set; } = "Disconnected";

        public static void Connect()
        {
            string host = Plugin.ServerAddressEntry?.Value ?? "localhost:38281";
            string slot = Plugin.SlotNameEntry?.Value ?? "Player";
            string pass = Plugin.PasswordEntry?.Value ?? "";

            UpdateStatus($"Connecting to {host} as {slot}...");
            
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    _session = ArchipelagoSessionFactory.CreateSession(host);
                    var result = _session.TryConnectAndLogin(
                        "Bobo Bay",                     // must match your apworld's game name
                        slot,
                        ItemsHandlingFlags.AllItems,
                        version: new Version(0, 6, 7),
                        password: string.IsNullOrEmpty(pass) ? null : pass);

                    if (result.Successful)
                    {
                        var success = (LoginSuccessful)result;
                        IsConnected = true;
                        UpdateStatus($"Connected to {host}");

                        string currentSeed = _session.RoomState?.Seed ?? "";
                        ArchipelagoItemHandler.BindProgressEntriesForSeed(currentSeed);

                        if (success.SlotData.TryGetValue("bobo_tickets_required", out object ticketsObj))
                        {
                            ArchipelagoItemHandler.SetBoboTicketsRequired(Convert.ToInt32(ticketsObj));
                        }
                        if (success.SlotData.TryGetValue("snack_multiplier", out object multObj))
                        {
                            float mult = Convert.ToSingle(multObj);
                            ArchipelagoItemHandler.ApplySnackMultiplier(mult);
                        }
                        if (success.SlotData.TryGetValue("unlimited_snacks", out object unlimitedObj))
                        {
                            bool unlimited = Convert.ToBoolean(unlimitedObj);
                            ArchipelagoItemHandler.SetUnlimitedSnacks(unlimited);
                        }
                        if (success.SlotData.TryGetValue("competition_unlock_thresholds", out object thresholdsObj))
                        {
                            ArchipelagoItemHandler.CompetitionUnlockThresholds = ParseThresholds(thresholdsObj);
                            Plugin.Log?.LogInfo($"[Archipelago] Loaded {ArchipelagoItemHandler.CompetitionUnlockThresholds.Count} competition unlock threshold(s).");
                        }
                    }
                    else
                    {
                        var failure = (LoginFailure)result;
                        IsConnected = false;
                        UpdateStatus("Connection Failed: " + string.Join(", ", failure.Errors));
                    }
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    UpdateStatus($"Connection Failed: {ex.Message}");
                    Plugin.Log?.LogError($"Archipelago connection exception: {ex}");
                }
            });
        }

        public static void Disconnect()
        {
            _session?.Socket.DisconnectAsync();
            IsConnected = false;
            UpdateStatus("Disconnected");
        }

        public static void CheckLocation(long locationId)
        {
            if (!IsConnected) { Plugin.Log?.LogWarning($"Location {locationId} checked while offline."); return; }
            _session.Locations.CompleteLocationChecks(locationId);
        }

        private static void UpdateStatus(string message)
        {
            StatusMessage = message;
            Plugin.Log?.LogInfo($"Archipelago Status: {message}");
        }
    }
}