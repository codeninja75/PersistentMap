﻿using BattleTech;
using Harmony;
using HBS;
using PersistentMapAPI;
using System;
using System.Linq;

namespace PersistentMapClient {

    [HarmonyPatch(typeof(Starmap), "PopulateMap", new Type[] { typeof(SimGameState) })]
    public static class Starmap_PopulateMap_Patch {
        static void Postfix(SimGameState simGame) {
            try {
                StarMap map = Helper.GetStarMap();
                foreach (PersistentMapAPI.System system in map.systems) {
                    StarSystem system2 = simGame.StarSystems.Find(x => x.Name.Equals(system.name));
                    if (system2 != null) {
                        Faction newOwner = system.controlList.OrderByDescending(x => x.percentage).First().faction;
                        Faction oldOwner = system2.Owner;
                        AccessTools.Method(typeof(StarSystemDef), "set_Owner").Invoke(system2.Def, new object[] {
                            newOwner });
                        system2.Tags.Remove(Helper.GetFactionTag(oldOwner));
                        system2.Tags.Add(Helper.GetFactionTag(newOwner));
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract_Patch {
        static void Postfix(Contract __instance, BattleTech.MissionResult result) {
            try {
                PersistentMapAPI.MissionResult mresult = new PersistentMapAPI.MissionResult();
                mresult.employer = __instance.Override.employerTeam.faction;
                mresult.target = __instance.Override.targetTeam.faction;
                mresult.result = result;
                GameInstance game = LazySingletonBehavior<UnityGameInstance>.Instance.Game;
                StarSystem system = game.Simulation.StarSystems.Find(x => x.ID == __instance.TargetSystem);
                mresult.systemName = system.Name;
                Helper.PostMissionResult(mresult);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}