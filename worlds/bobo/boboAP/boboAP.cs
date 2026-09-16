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

        [HarmonyPostfix]
        public static void Postfix(CompetitionSO __instance, ref bool __result)
        {
            if (__instance == null) return;

            if (ArchipelagoItemHandler.GoalCompetitionAssetNames.Contains(__instance.name))
            {
                __result = ArchipelagoItemHandler.BoboTicketsReceived >= ArchipelagoItemHandler.BoboTicketsRequired;
                return;
            }

            if (__instance.isSaga && __instance.saga != null)
            {
                __result = !__instance.saga.locked;
                return;
            }

            if (ArchipelagoItemHandler.CompetitionUnlockThresholds.TryGetValue(__instance.name, out int required))
            {
                __result = ArchipelagoItemHandler.ProgressiveCompetitionsReceived >= required;
                return;
            }

            if (__instance.rank == Grade.E){ __result = true; }
            else if (__instance.rank == Grade.D){ __result = ArchipelagoItemHandler.DRankUnlocked; }
            else if (__instance.rank == Grade.C){ __result = ArchipelagoItemHandler.CRankUnlocked; }
            else if (__instance.rank == Grade.B){ __result = ArchipelagoItemHandler.BRankUnlocked; }
            else if (__instance.rank == Grade.A){ __result = ArchipelagoItemHandler.ARankUnlocked; }
            else if (__instance.rank == Grade.S){ __result = ArchipelagoItemHandler.SRankUnlocked; }
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

                if (ArchipelagoItemHandler.GoalCompetitionAssetNames.Contains(competition.name))
                {
                    forceAvailable = ArchipelagoItemHandler.BoboTicketsReceived >= ArchipelagoItemHandler.BoboTicketsRequired;
                }
                else
                if (competition.isSaga && competition.saga != null)
                {
                    forceAvailable = !competition.saga.locked;
                }
                else if (ArchipelagoItemHandler.CompetitionUnlockThresholds.TryGetValue(competition.name, out int required))
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

    [HarmonyPatch(typeof(CompetitionOrganizer), "SetTodaysCompetitions")]
    public static class SagaAvailabilityPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            foreach (var saga in Resources.FindObjectsOfTypeAll<CompetitionSeriesSO_Saga>())
            {
                if (saga == null) continue;
                if (ArchipelagoItemHandler.SagaUnlockThresholds.TryGetValue(saga.name, out int required))
                {
                    saga.locked = ArchipelagoItemHandler.ProgressiveSagasReceived < required;
                }
                else 
                {
                    saga.locked = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GardenManager), "Awake")]
    public static class NormalCompetitionDumpPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                var all = Resources.FindObjectsOfTypeAll<CompetitionSO>();
                var standalone = all.Where(c => c != null && !c.isSaga).ToList();

                Plugin.Log?.LogInfo($"[APDebug] Found {all.Length} total CompetitionSO, {standalone.Count} standalone (non-saga).");

                foreach (var comp in standalone.OrderBy(c => c.rank).ThenBy(c => c.name))
                {
                    string title = Traverse.Create(comp).Field("title").GetValue<string>();
                    bool known = CompetitionLocations.All.ContainsKey(comp.name);
                    string status = known ? $"OK -> {CompetitionLocations.All[comp.name]}" : "MISSING";

                    Plugin.Log?.LogInfo($"[APDebug] comp: {comp.name} (rank={comp.rank}, type={comp.type}, title='{title}') [{status}]");
                }

                // Flag anything in the dictionary that no longer matches a real competition
                // (stale entries, or the duplicate-key situation from before)
                var realNames = standalone.Select(c => c.name).ToHashSet();
                foreach (var key in CompetitionLocations.All.Keys)
                {
                    if (!realNames.Contains(key))
                        Plugin.Log?.LogWarning($"[APDebug] Dictionary entry '{key}' does not match any live standalone competition.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[APDebug] NormalCompetitionDumpPatch threw: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(CompetitionSO), "Unlocked")]
    public static class SagaUnlockDiagnosticPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CompetitionSO __instance, ref bool __result)
        {
            if (__instance == null || !__instance.isSaga) return;
            Plugin.Log?.LogInfo($"[APDebug] Saga comp '{__instance.name}': saga='{__instance.saga?.name}', saga.locked={__instance.saga?.locked}, Unlocked()={__result}");
        }
    }

    [HarmonyPatch(typeof(GardenManager), "Awake")]
    public static class SagaRosterDumpPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                var sagas = Resources.FindObjectsOfTypeAll<CompetitionSeriesSO_Saga>();
                Plugin.Log?.LogInfo($"[APDebug] Found {sagas.Length} CompetitionSeriesSO_Saga instance(s).");

                foreach (var saga in sagas)
                {
                    if (saga == null) continue;
                    string title = Traverse.Create(saga).Field("title").GetValue<string>();
                    Plugin.Log?.LogInfo($"[APDebug] SAGA '{saga.name}' (title='{title}', locked={saga.locked})");

                    foreach (var comp in saga.competitions ?? new List<CompetitionSO>())
                    {
                        string compTitle = Traverse.Create(comp).Field("title").GetValue<string>();
                        Plugin.Log?.LogInfo($"[APDebug]   comp: {comp.name} (rank={comp.rank}, title='{compTitle}')");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[APDebug] SagaRosterDumpPatch threw: {ex}");
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
            
            if (!CompetitionLocations.All.TryGetValue(soName, out long locationID))
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
        public static ConfigEntry<int> BoboTicketsRequiredEntry;
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
            ArchipelagoItemHandler.ProgressiveCompetitionsReceivedEntry = Config.Bind("Archipelago", "ProgressiveCompetitionsReceived", 0,
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
        public static ConfigEntry<int> ProgressiveSagasReceivedEntry;

        public static ConfigEntry<bool> DRankUnlockedEntry, 
            CRankUnlockedEntry, 
            BRankUnlockedEntry, 
            ARankUnlockedEntry, 
            SRankUnlockedEntry;
        public static ConfigEntry<int> ItemsGrantedIndexEntry;
        public const long DRankUnlockItemId = 20050050;
        public const long BoboTicketId = 20050000;
        public const long ProgressiveCompetitionsId = 20050001;
        public const long ProgressiveSagasId = 20050002;
        public static bool DRankUnlocked => DRankUnlockedEntry?.Value ?? false;
        public static bool CRankUnlocked => CRankUnlockedEntry?.Value ?? false;
        public static bool BRankUnlocked => BRankUnlockedEntry?.Value ?? false;
        public static bool ARankUnlocked => ARankUnlockedEntry?.Value ?? false;
        public static bool SRankUnlocked => SRankUnlockedEntry?.Value ?? false;
        public static int BoboTicketsReceived => BoboTicketsReceivedEntry?.Value ?? 0;
        public static int BoboTicketsRequired => Plugin.BoboTicketsRequiredEntry?.Value ?? 3;
        public static float CurrentSnackMultiplier { get; private set; } = 1f;
        public static bool UnlimitedSnacksEnabled { get; private set; } = false;
        private static readonly int BaseMinStat = Common.DEFAULT_MINSTATUPDATE;
        private static readonly int BaseMaxStat = Common.DEFAULT_MAXSTATUPDATE;
        public static Dictionary<string, int> SagaUnlockThresholds = new Dictionary<string, int>();
        public static int ProgressiveSagasReceived => ProgressiveSagasReceivedEntry?.Value ?? 0;

        public static Dictionary<string, int> CompetitionUnlockThresholds = new Dictionary<string, int>();
        public static int ProgressiveCompetitionsReceived => ArchipelagoItemHandler.ProgressiveCompetitionsReceivedEntry?.Value ?? 0;
        
        // goals go here!
        public static readonly HashSet<string> GoalCompetitionAssetNames = new HashSet<string>
        {
            "BigJam_Race_D",
        };
        
        public static void BindProgressEntriesForSeed(string seed)
        {
            string safeSeed = new string(seed.Where(c => char.IsLetterOrDigit(c)).ToArray());
            if (string.IsNullOrEmpty(safeSeed)) safeSeed = "unknown";
            string section = "Progress_" + safeSeed;

            BoboTicketsReceivedEntry = Plugin.ConfigFile.Bind(section, "BoboTicketsReceived", 0,
                "Progress for this specific seed — how many Bobo Tickets received.");
            ProgressiveCompetitionsReceivedEntry = Plugin.ConfigFile.Bind(section, "ProgressiveCompetitionsReceived", 0,
                "Progress for this specific seed — how many Progressive Competitions received.");
            ProgressiveSagasReceivedEntry = Plugin.ConfigFile.Bind(section, "ProgressiveSagasReceived", 0,
                "Progress for this specific seed — how many Progressive Sagas received.");
            ItemsGrantedIndexEntry = Plugin.ConfigFile.Bind(section, "ItemsGrantedIndex", 0,
                "Progress for this specific seed — count of AP items already granted.");

            Plugin.Log?.LogInfo($"[Archipelago] Bound progress entries for seed section '{section}'.");
        }

        public static void GrantProgressiveCompetitions()
        {
            // want to convert from unlocking competitions by rank
            // to unlocking sets of competitions to stagger the progression
            if (ArchipelagoItemHandler.ProgressiveCompetitionsReceivedEntry != null)
            ArchipelagoItemHandler.ProgressiveCompetitionsReceivedEntry.Value++;

            Plugin.Log?.LogInfo($"[Archipelago] Progressive Competitions received ({ProgressiveCompetitionsReceived}).");

        }
        public static void GrantBoboTicket()
        {
            if (BoboTicketsReceivedEntry != null)
                BoboTicketsReceivedEntry.Value++;

            Plugin.Log?.LogInfo($"[Archipelago] Bobo Ticket received ({BoboTicketsReceived}/{BoboTicketsRequired}).");
        }
        public static void SetBoboTicketsRequired(int required)
        {
            if (Plugin.BoboTicketsRequiredEntry != null)
                Plugin.BoboTicketsRequiredEntry.Value = required;
        }

        public static void ForceRefreshCompetitions()
        {
            var organizer = UnityEngine.Object.FindObjectOfType(typeof(BobosWorld.CompetitionOrganizer)) as BobosWorld.CompetitionOrganizer;
            if (organizer == null) return; // not in-game yet (e.g. connected from main menu) — nothing to refresh

            var garden = BobosWorld.Garden.Current;
            if (garden == null) return;

            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            var dateTimeSOField = typeof(BobosWorld.Garden).GetField("_curDateTime", flags);
            object dateTimeSO = dateTimeSOField?.GetValue(garden);
            var valueProp = dateTimeSO?.GetType().GetProperty("Value", flags);
            object currentDate = valueProp?.GetValue(dateTimeSO);
            if (currentDate == null) return;

            var method = typeof(BobosWorld.CompetitionOrganizer).GetMethod("SetTodaysCompetitions", flags);
            method?.Invoke(organizer, new object[] { currentDate });
            Plugin.Log?.LogInfo("[Archipelago] Forced competition/saga refresh after loading thresholds.");
        }
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
            // Trait items
            { 20050400, "Trait_BalancePole" },
            { 20050401, "Trait_Skimboard" },
            { 20050402, "Trait_SteelChair" },
            { 20050403, "Trait_TrainerCube" },
            { 20050404, "Trait_PlyoBox" },
            { 20050405, "Trait_Lockpick" },
            { 20050406, "Trait_ResistanceBand" },
            { 20050407, "Trait_FakeID" },
            { 20050408, "Trait_Teapot" },
            { 20050409, "Trait_Glider" },
            { 20050410, "Trait_Gun" },
            { 20050411, "Trait_SlotMachine" },
            { 20050412, "Trait_Dumbbell" },
            { 20050413, "Trait_FishingRod" },
            { 20050414, "Trait_Shovel" },
            { 20050415, "Trait_JumpRope" },
            { 20050416, "Trait_Skillet" },
            { 20050417, "Trait_Trident" },
            { 20050418, "Trait_LumiStar" },
            { 20050419, "Trait_StickyHand" },
            { 20050420, "Trait_PocketSand" },
            { 20050421, "Trait_Teleporter" },
            { 20050422, "Trait_Funnel" },
            { 20050423, "Trait_Clipboard" },
            { 20050424, "Trait_BoxingGloves" },
            { 20050425, "Trait_IceAxes" },
            { 20050426, "Trait_GoodLuckCharm" },
            { 20050427, "Trait_MoodStabilizer" },
            { 20050428, "Trait_BananaPeel" },
            { 20050429, "Trait_FlintAndSteel" },
            { 20050430, "Trait_Skateboard" },
            { 20050431, "Trait_Sword" },
            { 20050432, "Trait_Inhaler" },
            
            // Other items
            { 20050500, "Bed_Cute_Pink" },
            { 20050501, "Bed_RoundTent_Red" },
            { 20050502, "Bed_BasicSleepingBag_Black" },
            { 20050503, "Bed_Grave" },
            { 20050504, "Bed_RaceCar_Blue" },
            { 20050505, "Bed_RaceCar_Red" },
            { 20050506, "Bed_RoundTent_Yellow" },
            { 20050507, "Bed_CatBed_Purple" },
            { 20050508, "Bed_Cute_Red" },
            { 20050509, "Bed_Crib_Blue" },
            { 20050510, "Bed_RoundTent_Blue" },
            { 20050511, "Bed_RoundTent_Black" },
            { 20050512, "Medicine" },
            { 20050513, "Bed_BasicSleepingBag_Purple" },
            { 20050514, "Bed_BasicSleepingBag_White" },
            { 20050515, "Bed_BasicSleepingBag_Blue" },
            { 20050516, "Bed_BasicSleepingBag_Red" },
            { 20050517, "Bed_BasicSleepingBag_Yellow" },
            { 20050518, "Bed_BasicSleepingBag_Green" },
            { 20050519, "Bed_Cute_Black" },
            { 20050520, "Bed_LilyPad" },
            { 20050521, "Bed_Trash" },
            { 20050522, "Bed_Raft" },
            { 20050523, "Bed_RaceCar_Green" },
            { 20050524, "Bed_CatBed_Blue" },
            { 20050525, "Bed_Crib_Pink" },
            { 20050526, "Bed_RaceCar_Black" },
            
            { 20050050, "Bunny_Cracker" },
            { 20050051, "Crackthrust_Hype" },
            { 20050052, "ToyItem_StuffedAnimal" },


            // Junk items
            // 75 money is 20050090
            { 20050091, "Gumball_ConcernedEyes" },
            { 20050092, "Pie - Banana Cream" },
            { 20050093, "Pie - Key Lime" },
            { 20050094, "Pie - Blueberry" },
            { 20050095, "Pie - Pecan" },
            { 20050096, "Thick Pie" },
            { 20050097, "Thick Pie with Love" },
            { 20050098, "Baked_Cake" },
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

        public static void GrantProgressiveSagas()
        {
            if (ProgressiveSagasReceivedEntry != null)
                ProgressiveSagasReceivedEntry.Value++;
            Plugin.Log?.LogInfo($"[Archipelago] Progressive Sagas received ({ProgressiveSagasReceived}).");
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
                case ProgressiveSagasId:
                    GrantProgressiveSagas();
                    break;
                case BoboTicketId:
                    GrantBoboTicket();
                    break;
                case 20050090:
                    GrantMoney(75);
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
                        if (success.SlotData.TryGetValue("saga_unlock_thresholds", out object sagaThresholdsObj))
                        {
                            ArchipelagoItemHandler.SagaUnlockThresholds = ParseThresholds(sagaThresholdsObj);
                            Plugin.Log?.LogInfo($"[Archipelago] Loaded {ArchipelagoItemHandler.SagaUnlockThresholds.Count} saga unlock threshold(s).");
                        }

                        ArchipelagoItemHandler.ForceRefreshCompetitions();
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