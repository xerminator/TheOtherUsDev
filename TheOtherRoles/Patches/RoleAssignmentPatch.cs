﻿using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using AmongUs.GameOptions;
using TheOtherRoles.Players;
using TheOtherRoles.Utilities;
using static TheOtherRoles.TheOtherRoles;
using TheOtherRoles.CustomGameModes;

namespace TheOtherRoles.Patches {
    [HarmonyPatch(typeof(RoleOptionsData), nameof(RoleOptionsData.GetNumPerGame))]
    class RoleOptionsDataGetNumPerGamePatch{
        public static void Postfix(ref int __result) {
            if (CustomOptionHolder.activateRoles.getBool()) __result = 0; // Deactivate Vanilla Roles if the mod roles are active
        }
    }

    [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
    class GameOptionsDataGetAdjustedNumImpostorsPatch {
        public static void Postfix(ref int __result) {
            if (MapOptionsTor.gameMode == CustomGamemodes.HideNSeek) {
                int impCount = Mathf.RoundToInt(CustomOptionHolder.hideNSeekHunterCount.getFloat());
                __result = impCount; ; // Set Imp Num
            } else if (GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal) {  // Ignore Vanilla impostor limits in TOR Games.
                __result = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.NumImpostors, 1, 3);
            } 
        }
    }

    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Validate))]
    class GameOptionsDataValidatePatch {
        public static void Postfix(GameOptionsData __instance) {
            if (MapOptionsTor.gameMode == CustomGamemodes.HideNSeek || GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.Normal) return;
            __instance.NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class RoleManagerSelectRolesPatch {
        private static int crewValues;
        private static int impValues;
        private static List<Tuple<byte, byte>> playerRoleMap = new List<Tuple<byte, byte>>();
        public static bool isGuesserGamemode { get { return MapOptionsTor.gameMode == CustomGamemodes.Guesser; } }
        public static void Postfix() {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ResetVaribles, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.resetVariables();
            if (MapOptionsTor.gameMode == CustomGamemodes.HideNSeek || GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek) return; // Don't assign Roles in Hide N Seek
            if (CustomOptionHolder.activateRoles.getBool()) // Don't assign Roles in Tutorial or if deactivated
                assignRoles();
        }

        private static void assignRoles() {
            var data = getRoleAssignmentData();
            assignSpecialRoles(data); // Assign special roles like mafia and lovers first as they assign a role to multiple players and the chances are independent of the ticket system
            selectFactionForFactionIndependentRoles(data);
            assignEnsuredRoles(data); // Assign roles that should always be in the game next
            assignDependentRoles(data); // Assign roles that may have a dependent role
            assignChanceRoles(data); // Assign roles that may or may not be in the game last
            assignModifiers();
            assignRoleTargets(data);
			if (isGuesserGamemode) assignGuesserGamemode();
          //  setRolesAgain(); //bugged
        }

        public static RoleAssignmentData getRoleAssignmentData() {
            // Get the players that we want to assign the roles to. Crewmate and Neutral roles are assigned to natural crewmates. Impostor roles to impostors.
            List<PlayerControl> crewmates = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            crewmates.RemoveAll(x => x.Data.Role.IsImpostor);
            List<PlayerControl> impostors = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            impostors.RemoveAll(x => !x.Data.Role.IsImpostor);

            var crewmateMin = CustomOptionHolder.crewmateRolesCountMin.getSelection();
            var crewmateMax = CustomOptionHolder.crewmateRolesCountMax.getSelection();
            var neutralMin = CustomOptionHolder.neutralRolesCountMin.getSelection();
            var neutralMax = CustomOptionHolder.neutralRolesCountMax.getSelection();
            var impostorMin = CustomOptionHolder.impostorRolesCountMin.getSelection();
            var impostorMax = CustomOptionHolder.impostorRolesCountMax.getSelection();
            
            // Make sure min is less or equal to max
            if (crewmateMin > crewmateMax) crewmateMin = crewmateMax;
            if (neutralMin > neutralMax) neutralMin = neutralMax;
            if (impostorMin > impostorMax) impostorMin = impostorMax;

            // Get the maximum allowed count of each role type based on the minimum and maximum option
            int crewCountSettings = rnd.Next(crewmateMin, crewmateMax + 1);
            int neutralCountSettings = rnd.Next(neutralMin, neutralMax + 1);
            int impCountSettings = rnd.Next(impostorMin, impostorMax + 1);

            // Potentially lower the actual maximum to the assignable players
            int maxCrewmateRoles = Mathf.Min(crewmates.Count, crewCountSettings);
            int maxNeutralRoles = Mathf.Min(crewmates.Count, neutralCountSettings);
            int maxImpostorRoles = Mathf.Min(impostors.Count, impCountSettings);

            // Fill in the lists with the roles that should be assigned to players. Note that the special roles (like Mafia or Lovers) are NOT included in these lists
            Dictionary<byte, int> impSettings = new Dictionary<byte, int>();
            Dictionary<byte, int> neutralSettings = new Dictionary<byte, int>();
            Dictionary<byte, int> crewSettings = new Dictionary<byte, int>();
            
            impSettings.Add((byte)RoleId.Morphling, CustomOptionHolder.morphlingSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Camouflager, CustomOptionHolder.camouflagerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Vampire, CustomOptionHolder.vampireSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Eraser, CustomOptionHolder.eraserSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Trickster, CustomOptionHolder.tricksterSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Cleaner, CustomOptionHolder.cleanerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Undertaker, CustomOptionHolder.undertakerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Miner, CustomOptionHolder.minerSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Warlock, CustomOptionHolder.warlockSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.BountyHunter, CustomOptionHolder.bountyHunterSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Witch, CustomOptionHolder.witchSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Escapist, CustomOptionHolder.escapistSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Ninja, CustomOptionHolder.ninjaSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Bomber, CustomOptionHolder.bomberSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Poucher, CustomOptionHolder.poucherSpawnRate.getSelection());
			impSettings.Add((byte)RoleId.Mimic, CustomOptionHolder.mimicSpawnRate.getSelection());
            impSettings.Add((byte)RoleId.Blackmailer, CustomOptionHolder.blackmailerSpawnRate.getSelection());
            // Don't spawn cultist normally : impSettings.Add((byte)RoleId.Cultist, CustomOptionHolder.cultistSpawnRate.getSelection());

            neutralSettings.Add((byte)RoleId.Jester, CustomOptionHolder.jesterSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Prosecutor, CustomOptionHolder.prosecutorSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Amnisiac, CustomOptionHolder.amnisiacSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Arsonist, CustomOptionHolder.arsonistSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Jackal, CustomOptionHolder.jackalSpawnRate.getSelection());
            // Don't assign Swooper unless Both option is on
            if (!CustomOptionHolder.swooperAsWell.getBool()) 
                neutralSettings.Add((byte)RoleId.Swooper, CustomOptionHolder.swooperSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Werewolf, CustomOptionHolder.werewolfSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Vulture, CustomOptionHolder.vultureSpawnRate.getSelection());
            neutralSettings.Add((byte)RoleId.Thief, CustomOptionHolder.thiefSpawnRate.getSelection());

            if (false) // Lawyer or Prosecutor
                neutralSettings.Add((byte)RoleId.Prosecutor, CustomOptionHolder.lawyerSpawnRate.getSelection());
            else
                neutralSettings.Add((byte)RoleId.Lawyer, CustomOptionHolder.lawyerSpawnRate.getSelection());

            crewSettings.Add((byte)RoleId.Mayor, CustomOptionHolder.mayorSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Portalmaker, CustomOptionHolder.portalmakerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Engineer, CustomOptionHolder.engineerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.PrivateInvestigator, CustomOptionHolder.privateInvestigatorSpawnRate.getSelection());
       //     crewSettings.Add((byte)RoleId.Lighter, CustomOptionHolder.lighterSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.BodyGuard, CustomOptionHolder.bodyGuardSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Detective, CustomOptionHolder.detectiveSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.TimeMaster, CustomOptionHolder.timeMasterSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Veteren, CustomOptionHolder.veterenSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Medic, CustomOptionHolder.medicSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Swapper,CustomOptionHolder.swapperSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Seer, CustomOptionHolder.seerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Hacker, CustomOptionHolder.hackerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Tracker, CustomOptionHolder.trackerSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Snitch, CustomOptionHolder.snitchSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Medium, CustomOptionHolder.mediumSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.NiceGuesser, CustomOptionHolder.guesserSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Trapper, CustomOptionHolder.trapperSpawnRate.getSelection());
            if (impostors.Count > 1) {
                // Only add Spy if more than 1 impostor as the spy role is otherwise useless
                crewSettings.Add((byte)RoleId.Spy, CustomOptionHolder.spySpawnRate.getSelection());
            }
            crewSettings.Add((byte)RoleId.SecurityGuard, CustomOptionHolder.securityGuardSpawnRate.getSelection());
            crewSettings.Add((byte)RoleId.Jumper, CustomOptionHolder.jumperSpawnRate.getSelection());

            return new RoleAssignmentData {
                crewmates = crewmates,
                impostors = impostors,
                crewSettings = crewSettings,
                neutralSettings = neutralSettings,
                impSettings = impSettings,
                maxCrewmateRoles = maxCrewmateRoles,
                maxNeutralRoles = maxNeutralRoles,
                maxImpostorRoles = maxImpostorRoles
            };
        }

        private static void assignSpecialRoles(RoleAssignmentData data) {
            
            // //Assign Cultist
            if (Cultist.isCultistGame) {
                setRoleToRandomPlayer((byte)RoleId.Cultist, data.impostors);
            }
             if (data.impostors.Count < 2 && data.maxImpostorRoles < 2 && (rnd.Next(1, 101) <= CustomOptionHolder.cultistSpawnRate.getSelection() * 10))
             {
          //       var index = rnd.Next(0, data.impostors.Count);
            //     PlayerControl playerControl = data.impostors[index];

             //    Helpers.turnToCrewmate(playerControl);
                
             //    data.impostors.RemoveAt(index);
             //    data.crewmates.Add(playerControl);
           //      setRoleToRandomPlayer((byte)RoleId.Cultist, data.impostors);
                 //data.impostors.Count = 1;
                 data.impostors.Capacity = 1;
                 data.maxImpostorRoles = 1;


             }
            // Assign Mafia
            if (data.impostors.Count >= 3 && data.maxImpostorRoles >= 3 && (rnd.Next(1, 101) <= CustomOptionHolder.mafiaSpawnRate.getSelection() * 10)) {
                setRoleToRandomPlayer((byte)RoleId.Godfather, data.impostors);
                setRoleToRandomPlayer((byte)RoleId.Janitor, data.impostors);
                setRoleToRandomPlayer((byte)RoleId.Mafioso, data.impostors);
                data.maxImpostorRoles -= 3;
            }
        }

        private static void selectFactionForFactionIndependentRoles(RoleAssignmentData data) {
            // Assign Guesser (chance to be impostor based on setting)
            // isEvilGuesser =  rnd.Next(1, 101) <= CustomOptionHolder.guesserIsImpGuesserRate.getSelection() * 10;
            // if ((CustomOptionHolder.guesserSpawnBothRate.getSelection() > 0 && 
                // CustomOptionHolder.guesserSpawnRate.getSelection() == 10) || 
                // CustomOptionHolder.guesserSpawnBothRate.getSelection() == 0) {
                    // if (isEvilGuesser) data.impSettings.Add((byte)RoleId.EvilGuesser, CustomOptionHolder.guesserSpawnRate.getSelection());
                    // else data.crewSettings.Add((byte)RoleId.NiceGuesser, CustomOptionHolder.guesserSpawnRate.getSelection());

            // }

            // Assign Sheriff
            if ((CustomOptionHolder.deputySpawnRate.getSelection() > 0 &&
                CustomOptionHolder.sheriffSpawnRate.getSelection() == 10) ||
                CustomOptionHolder.deputySpawnRate.getSelection() == 0) 
                    data.crewSettings.Add((byte)RoleId.Sheriff, CustomOptionHolder.sheriffSpawnRate.getSelection());


            crewValues = data.crewSettings.Values.ToList().Sum();
            impValues = data.impSettings.Values.ToList().Sum();
        }

        private static void assignEnsuredRoles(RoleAssignmentData data) {
            // Get all roles where the chance to occur is set to 100%
            List<byte> ensuredCrewmateRoles = data.crewSettings.Where(x => x.Value == 10).Select(x => x.Key).ToList();
            List<byte> ensuredNeutralRoles = data.neutralSettings.Where(x => x.Value == 10).Select(x => x.Key).ToList();
            List<byte> ensuredImpostorRoles = data.impSettings.Where(x => x.Value == 10).Select(x => x.Key).ToList();

            // Assign roles until we run out of either players we can assign roles to or run out of roles we can assign to players
            while (
                (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && ensuredImpostorRoles.Count > 0) || 
                (data.crewmates.Count > 0 && (
                    (data.maxCrewmateRoles > 0 && ensuredCrewmateRoles.Count > 0) || 
                    (data.maxNeutralRoles > 0 && ensuredNeutralRoles.Count > 0)
                ))) {
                    
                Dictionary<RoleType, List<byte>> rolesToAssign = new Dictionary<RoleType, List<byte>>();
                if (data.crewmates.Count > 0 && data.maxCrewmateRoles > 0 && ensuredCrewmateRoles.Count > 0) rolesToAssign.Add(RoleType.Crewmate, ensuredCrewmateRoles);
                if (data.crewmates.Count > 0 && data.maxNeutralRoles > 0 && ensuredNeutralRoles.Count > 0) rolesToAssign.Add(RoleType.Neutral, ensuredNeutralRoles);
                if (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && ensuredImpostorRoles.Count > 0) rolesToAssign.Add(RoleType.Impostor, ensuredImpostorRoles);
                
                // Randomly select a pool of roles to assign a role from next (Crewmate role, Neutral role or Impostor role) 
                // then select one of the roles from the selected pool to a player 
                // and remove the role (and any potentially blocked role pairings) from the pool(s)
                var roleType = rolesToAssign.Keys.ElementAt(rnd.Next(0, rolesToAssign.Keys.Count())); 
                var players = roleType == RoleType.Crewmate || roleType == RoleType.Neutral ? data.crewmates : data.impostors;
                var index = rnd.Next(0, rolesToAssign[roleType].Count);
                var roleId = rolesToAssign[roleType][index];
                setRoleToRandomPlayer(rolesToAssign[roleType][index], players);
                rolesToAssign[roleType].RemoveAt(index);

                if (CustomOptionHolder.blockedRolePairings.ContainsKey(roleId)) {
                    foreach(var blockedRoleId in CustomOptionHolder.blockedRolePairings[roleId]) {
                        // Set chance for the blocked roles to 0 for chances less than 100%
                        if (data.impSettings.ContainsKey(blockedRoleId)) data.impSettings[blockedRoleId] = 0;
                        if (data.neutralSettings.ContainsKey(blockedRoleId)) data.neutralSettings[blockedRoleId] = 0;
                        if (data.crewSettings.ContainsKey(blockedRoleId)) data.crewSettings[blockedRoleId] = 0;
                        // Remove blocked roles even if the chance was 100%
                        foreach(var ensuredRolesList in rolesToAssign.Values) {
                            ensuredRolesList.RemoveAll(x => x == blockedRoleId);
                        }
                    }
                }

                // Adjust the role limit
                switch (roleType) {
                    case RoleType.Crewmate: data.maxCrewmateRoles--; crewValues -= 10; break;
                    case RoleType.Neutral: data.maxNeutralRoles--; break;
                    case RoleType.Impostor: data.maxImpostorRoles--; impValues -= 10;  break;
                }
            }
        }

        private static void assignDependentRoles(RoleAssignmentData data) {
            // Roles that prob have a dependent role
            bool sheriffFlag = CustomOptionHolder.deputySpawnRate.getSelection() > 0 
                && CustomOptionHolder.sheriffSpawnRate.getSelection() > 0;

            // if (isGuesserGamemode) guesserFlag = false;
            if (!sheriffFlag) return; // assignDependentRoles is not needed

            int crew = data.crewmates.Count < data.maxCrewmateRoles ? data.crewmates.Count : data.maxCrewmateRoles; // Max number of crew loops
            int imp = data.impostors.Count < data.maxImpostorRoles ? data.impostors.Count : data.maxImpostorRoles; // Max number of imp loops
            int crewSteps = crew / data.crewSettings.Keys.Count(); // Avarage crewvalues deducted after each loop 
            int impSteps = imp / data.impSettings.Keys.Count(); // Avarage impvalues deducted after each loop

            // set to false if needed, otherwise we can skip the loop
            bool isSheriff = !sheriffFlag; 
            // bool isGuesser = !guesserFlag;

            // --- Simulate Crew & Imp ticket system ---
            while (crew > 0 && (!isSheriff /* || (!isEvilGuesser && !isGuesser )*/)) {
                if (!isSheriff && rnd.Next(crewValues) < CustomOptionHolder.sheriffSpawnRate.getSelection()) isSheriff = true;
                // if (!isEvilGuesser && !isGuesser && rnd.Next(crewValues) < CustomOptionHolder.guesserSpawnRate.getSelection()) isGuesser = true;
                crew--;
                crewValues -= crewSteps;
            }

            // --- Assign Main Roles if they won the lottery ---
            if (isSheriff && Sheriff.sheriff == null && data.crewmates.Count > 0 && data.maxCrewmateRoles > 0 && sheriffFlag) { // Set Sheriff cause he won the lottery
                byte sheriff = setRoleToRandomPlayer((byte)RoleId.Sheriff, data.crewmates);
                data.crewmates.ToList().RemoveAll(x => x.PlayerId == sheriff);
                data.maxCrewmateRoles--;
            }

            // --- Assign Dependent Roles if main role exists ---
            if (Sheriff.sheriff != null) { // Deputy
                if (CustomOptionHolder.deputySpawnRate.getSelection() == 10 && data.crewmates.Count > 0 && data.maxCrewmateRoles > 0) { // Force Deputy
                    byte deputy = setRoleToRandomPlayer((byte)RoleId.Deputy, data.crewmates);
                    data.crewmates.ToList().RemoveAll(x => x.PlayerId == deputy);
                    data.maxCrewmateRoles--;
                } else if (CustomOptionHolder.deputySpawnRate.getSelection() < 10) // Dont force, add Deputy to the ticket system
                    data.crewSettings.Add((byte)RoleId.Deputy, CustomOptionHolder.deputySpawnRate.getSelection());
            }

        }
        private static void assignChanceRoles(RoleAssignmentData data) {
            // Get all roles where the chance to occur is set grater than 0% but not 100% and build a ticket pool based on their weight
            List<byte> crewmateTickets = data.crewSettings.Where(x => x.Value > 0 && x.Value < 10).Select(x => Enumerable.Repeat(x.Key, x.Value)).SelectMany(x => x).ToList();
            List<byte> neutralTickets = data.neutralSettings.Where(x => x.Value > 0 && x.Value < 10).Select(x => Enumerable.Repeat(x.Key, x.Value)).SelectMany(x => x).ToList();
            List<byte> impostorTickets = data.impSettings.Where(x => x.Value > 0 && x.Value < 10).Select(x => Enumerable.Repeat(x.Key, x.Value)).SelectMany(x => x).ToList();

            // Assign roles until we run out of either players we can assign roles to or run out of roles we can assign to players
            while (
                (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && impostorTickets.Count > 0) || 
                (data.crewmates.Count > 0 && (
                    (data.maxCrewmateRoles > 0 && crewmateTickets.Count > 0) || 
                    (data.maxNeutralRoles > 0 && neutralTickets.Count > 0)
                ))) {
                
                Dictionary<RoleType, List<byte>> rolesToAssign = new Dictionary<RoleType, List<byte>>();
                if (data.crewmates.Count > 0 && data.maxCrewmateRoles > 0 && crewmateTickets.Count > 0) rolesToAssign.Add(RoleType.Crewmate, crewmateTickets);
                if (data.crewmates.Count > 0 && data.maxNeutralRoles > 0 && neutralTickets.Count > 0) rolesToAssign.Add(RoleType.Neutral, neutralTickets);
                if (data.impostors.Count > 0 && data.maxImpostorRoles > 0 && impostorTickets.Count > 0) rolesToAssign.Add(RoleType.Impostor, impostorTickets);
                
                // Randomly select a pool of role tickets to assign a role from next (Crewmate role, Neutral role or Impostor role) 
                // then select one of the roles from the selected pool to a player 
                // and remove all tickets of this role (and any potentially blocked role pairings) from the pool(s)
                var roleType = rolesToAssign.Keys.ElementAt(rnd.Next(0, rolesToAssign.Keys.Count()));
                var players = roleType == RoleType.Crewmate || roleType == RoleType.Neutral ? data.crewmates : data.impostors;
                var index = rnd.Next(0, rolesToAssign[roleType].Count);
                var roleId = rolesToAssign[roleType][index];
                setRoleToRandomPlayer(roleId, players);
                rolesToAssign[roleType].RemoveAll(x => x == roleId);

                if (CustomOptionHolder.blockedRolePairings.ContainsKey(roleId)) {
                    foreach(var blockedRoleId in CustomOptionHolder.blockedRolePairings[roleId]) {
                        // Remove tickets of blocked roles from all pools
                        crewmateTickets.RemoveAll(x => x == blockedRoleId);
                        neutralTickets.RemoveAll(x => x == blockedRoleId);
                        impostorTickets.RemoveAll(x => x == blockedRoleId);
                    }
                }

                // Adjust the role limit
                switch (roleType) {
                    case RoleType.Crewmate: data.maxCrewmateRoles--; break;
                    case RoleType.Neutral: data.maxNeutralRoles--;break;
                    case RoleType.Impostor: data.maxImpostorRoles--;break;
                }
            }
        }

        private static void assignRoleTargets(RoleAssignmentData data) {
            // Set Lawyer or Prosecutor Target
            if (Lawyer.lawyer != null) {
                var possibleTargets = new List<PlayerControl>();
                foreach (PlayerControl p in CachedPlayer.AllPlayers) {
                    if (!p.Data.IsDead && !p.Data.Disconnected && p != Lovers.lover1 && p != Lovers.lover2 && (p.Data.Role.IsImpostor ||  p == Jackal.jackal || p == Swooper.swooper || p == Werewolf.werewolf || (Lawyer.targetCanBeJester && p == Jester.jester)))
                        possibleTargets.Add(p);
                }
                
                if (possibleTargets.Count == 0) {
                    MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.LawyerPromotesToPursuer, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(w);
                    RPCProcedure.lawyerPromotesToPursuer();
                } else {
                    var target = possibleTargets[TheOtherRoles.rnd.Next(0, possibleTargets.Count)];
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.LawyerSetTarget, Hazel.SendOption.Reliable, -1);
                    writer.Write(target.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.lawyerSetTarget(target.PlayerId);
                }
            }
			
            // Start Set Prosecutor Target
            if (Prosecutor.prosecutor != null) {
                var possibleTargets = new List<PlayerControl>();
                foreach (PlayerControl p in PlayerControl.AllPlayerControls) {
                    if (p.Data.IsDead || p.Data.Disconnected) continue; // Don't assign dead people
                    if (p == Lovers.lover1 || p == Lovers.lover2) continue; // Don't allow a lover target
                    if (p.Data.Role.IsImpostor ||  p == Jackal.jackal || p == Swooper.swooper) continue; // Dont allow imp / jackal target
					if (p == Spy.spy) continue; // Dont allow Spy to be target
					if (p == Prosecutor.prosecutor) continue; // Dont allow self target
					// I simply don't want these targets, as they can hard counter Prosecutor
					if (p == Mayor.mayor || p == Sheriff.sheriff || p == Swapper.swapper || p == Shifter.shifter) continue;
                    if (Helpers.isNeutral(p)) continue; // Don't allow neutral target
                    possibleTargets.Add(p);
                }
                if (possibleTargets.Count == 0) {
/*
                    MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ProsecutorToPursuer, Hazel.SendOption.Reliable, -1);
                    w.Write(Prosecutor.prosecutor.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(w);
                    RPCProcedure.prosecutorToPursuer(Prosecutor.prosecutor.PlayerId);
*/
                } else {
                    var target = possibleTargets[TheOtherRoles.rnd.Next(0, possibleTargets.Count)];
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ProsecutorSetTarget, Hazel.SendOption.Reliable, -1);
                    writer.Write(target.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.prosecutorSetTarget(target.PlayerId);
                }
            }
            // End Set Prosecutor Target
        }

        private static void assignModifiers() {
            var modifierMin = CustomOptionHolder.modifiersCountMin.getSelection();
            var modifierMax = CustomOptionHolder.modifiersCountMax.getSelection();
            if (modifierMin > modifierMax) modifierMin = modifierMax;
            int modifierCountSettings = rnd.Next(modifierMin, modifierMax + 1);
            List<PlayerControl> players = PlayerControl.AllPlayerControls.ToArray().ToList();
            if (isGuesserGamemode && !CustomOptionHolder.guesserGamemodeHaveModifier.getBool())
                players.RemoveAll(x => GuesserGM.isGuesser(x.PlayerId));

            List<PlayerControl> impPlayer = new List<PlayerControl>(players);
            List<PlayerControl> neutralPlayer = new List<PlayerControl>(players);
            List<PlayerControl> impPlayerL = new List<PlayerControl>(players);
                List<PlayerControl> crewPlayer = new List<PlayerControl>(players);
                impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
                neutralPlayer.RemoveAll(x => !Helpers.isNeutral(x));
                impPlayerL.RemoveAll(x => !x.Data.Role.IsImpostor);
                crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor);

            int modifierCount = Mathf.Min(players.Count, modifierCountSettings);

            if (modifierCount == 0) return;

            List<RoleId> allModifiers = new List<RoleId>();
            List<RoleId> ensuredModifiers = new List<RoleId>();
            List<RoleId> chanceModifiers = new List<RoleId>();

            List<RoleId> impModifiers = new List<RoleId>();
            List<RoleId> ensuredImpModifiers = new List<RoleId>();
            List<RoleId> chanceImpModifiers = new List<RoleId>();
/* brb phantom
            List<RoleId> neutralModifiers = new List<RoleId>();
            List<RoleId> ensuredNeutralModifiers = new List<RoleId>();
            List<RoleId> chanceNeutralModifiers = new List<RoleId>();
*/
            allModifiers.AddRange(new List<RoleId> {
                RoleId.Tiebreaker,
                RoleId.Mini,
                RoleId.Bait,
                RoleId.Bloody,
                RoleId.AntiTeleport,
                RoleId.Sunglasses,
                RoleId.Torch,
                RoleId.Multitasker,
                RoleId.Vip,
                RoleId.Invert,
           //     RoleId.LifeGuard,
                RoleId.Indomitable,
                RoleId.Tunneler,
                RoleId.Slueth,
                RoleId.Blind,
                RoleId.Watcher,
                RoleId.Radar,
                RoleId.Disperser,
         //       RoleId.EvilGuesser,
          //      RoleId.NiceGuesser,
				RoleId.Cursed,
                RoleId.Chameleon
         //       RoleId.Shifter
            });

            impModifiers.AddRange(new List<RoleId>
            {
                RoleId.EvilGuesser
            });
/* brb phantom
            neutralModifiers.AddRange(new List<RoleId>
            {
                RoleId.PhantomAbility
            });
*/
            if (rnd.Next(1, 101) <= CustomOptionHolder.modifierLover.getSelection() * 10) { // Assign lover
                bool isEvilLover = rnd.Next(1, 101) <= CustomOptionHolder.modifierLoverImpLoverRate.getSelection() * 10;
                byte firstLoverId;

                impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor); //testing
                crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || x == Lawyer.lawyer); //testing

                if (!Cultist.isCultistGame) {
                if (isEvilLover) firstLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, impPlayerL);
                else firstLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, crewPlayer);
                byte secondLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, crewPlayer, 1);
                

                players.RemoveAll(x => x.PlayerId == firstLoverId || x.PlayerId == secondLoverId);
                modifierCount--;
                }

                if (Cultist.isCultistGame) {
                firstLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, crewPlayer);
                byte secondLoverId = setModifierToRandomPlayer((byte)RoleId.Lover, crewPlayer, 1);
                

                players.RemoveAll(x => x.PlayerId == firstLoverId || x.PlayerId == secondLoverId);
                modifierCount--;
                }
            }

            foreach (RoleId m in allModifiers) {
                if (getSelectionForRoleId(m) == 10) ensuredModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true) / 10));
                else chanceModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true)));
            }
            foreach (RoleId m in impModifiers)
            {
                if (getSelectionForRoleId(m) == 10) ensuredImpModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true) / 10));
                else chanceImpModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true)));
            }
            /* brb phantom
            foreach (RoleId m in neutralModifiers)
            {
                if (getSelectionForRoleId(m) == 10) ensuredNeutralModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true) / 10));
                else chanceNeutralModifiers.AddRange(Enumerable.Repeat(m, getSelectionForRoleId(m, true)));
            }
*/

            assignModifiersToPlayers(ensuredImpModifiers, impPlayer, modifierCount); // Assign ensured imp modifier
            /* brb phantom
            assignModifiersToPlayers(ensuredNeutralModifiers, neutralPlayer, modifierCount); // Assign ensured neutral modifier
            */

            assignModifiersToPlayers(ensuredModifiers, players, modifierCount); // Assign ensured modifier

      //brb phantom      modifierCount -= ensuredImpModifiers.Count + ensuredNeutralModifiers.Count + ensuredModifiers.Count;
            modifierCount -= ensuredImpModifiers.Count + ensuredModifiers.Count;
            if (modifierCount <= 0) return;
            int chanceModifierCount = Mathf.Min(modifierCount, chanceModifiers.Count);
            List<RoleId> chanceModifierToAssign = new List<RoleId>();
            while (chanceModifierCount > 0 && chanceModifiers.Count > 0) {
                var index = rnd.Next(0, chanceModifiers.Count);
                RoleId modifierId = chanceModifiers[index];
                chanceModifierToAssign.Add(modifierId);

                int modifierSelection = getSelectionForRoleId(modifierId);
                while (modifierSelection > 0) {
                    chanceModifiers.Remove(modifierId);
                    modifierSelection--;
                }
                chanceModifierCount--;
            }

            assignModifiersToPlayers(chanceModifierToAssign, players, modifierCount); // Assign chance modifier

            int chanceImpModifierCount = Mathf.Min(modifierCount, chanceImpModifiers.Count);
            List<RoleId> chanceImpModifierToAssign = new List<RoleId>();
            while (chanceImpModifierCount > 0 && chanceImpModifiers.Count > 0)
            {
                var index = rnd.Next(0, chanceImpModifiers.Count);
                RoleId modifierId = chanceImpModifiers[index];
                chanceImpModifierToAssign.Add(modifierId);

                int modifierSelection = getSelectionForRoleId(modifierId);
                while (modifierSelection > 0)
                {
                    chanceImpModifiers.Remove(modifierId);
                    modifierSelection--;
                }
                chanceImpModifierCount--;
            }
            assignModifiersToPlayers(chanceImpModifierToAssign, impPlayer, modifierCount); // Assign chance Imp modifier
/* brb phantom
            int chanceNeutralModifierCount = Mathf.Min(modifierCount, chanceNeutralModifiers.Count);
            List<RoleId> chanceNeutralModifierToAssign = new List<RoleId>();
            while (chanceNeutralModifierCount > 0 && chanceNeutralModifiers.Count > 0)
            {
                var index = rnd.Next(0, chanceNeutralModifiers.Count);
                RoleId modifierId = chanceNeutralModifiers[index];
                chanceNeutralModifierToAssign.Add(modifierId);

                int modifierSelection = getSelectionForRoleId(modifierId);
                while (modifierSelection > 0)
                {
                    chanceNeutralModifiers.Remove(modifierId);
                    modifierSelection--;
                }
                chanceNeutralModifierCount--;
            }
            assignModifiersToPlayers(chanceNeutralModifierToAssign, neutralPlayer, modifierCount); // Assign chance Imp modifier
			*/
        }


        private static void assignGuesserGamemode() {
            List<PlayerControl> impPlayer = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            List<PlayerControl> neutralPlayer = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            List<PlayerControl> crewPlayer = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
            impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
            neutralPlayer.RemoveAll(x => !Helpers.isNeutral(x));
            crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || Helpers.isNeutral(x));
            assignGuesserGamemodeToPlayers(crewPlayer, Mathf.RoundToInt(CustomOptionHolder.guesserGamemodeCrewNumber.getFloat()));
            assignGuesserGamemodeToPlayers(neutralPlayer, Mathf.RoundToInt(CustomOptionHolder.guesserGamemodeNeutralNumber.getFloat()), CustomOptionHolder.guesserForceJackalGuesser.getBool());
            assignGuesserGamemodeToPlayers(impPlayer, Mathf.RoundToInt(CustomOptionHolder.guesserGamemodeImpNumber.getFloat()));
        }

        private static void assignGuesserGamemodeToPlayers(List<PlayerControl> playerList, int count, bool forceJackal = false) {
            for (int i = 0; i < count && playerList.Count > 0; i++) {
                var index = rnd.Next(0, playerList.Count);
                if (forceJackal) {
                    if (Jackal.jackal != null)
                        index = playerList.FindIndex(x => x == Jackal.jackal);
                    forceJackal = false;
                }
                byte playerId = playerList[index].PlayerId;
                playerList.RemoveAt(index);

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetGuesserGm, Hazel.SendOption.Reliable, -1);
                writer.Write(playerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.setGuesserGm(playerId);
            }
        }

        private static byte setRoleToRandomPlayer(byte roleId, List<PlayerControl> playerList, bool removePlayer = true) {
            var index = rnd.Next(0, playerList.Count);
            byte playerId = playerList[index].PlayerId;
            if (removePlayer) playerList.RemoveAt(index);
            playerRoleMap.Add(new Tuple<byte, byte>(playerId, roleId));

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetRole, Hazel.SendOption.Reliable, -1);
            writer.Write(roleId);
            writer.Write(playerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.setRole(roleId, playerId);

            if (roleId == (byte)RoleId.Jackal && CustomOptionHolder.swooperAsWell.getBool()) {
                if (rnd.Next(1, 101) <= CustomOptionHolder.swooperSpawnRate.getSelection() * 10) {
                    MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetRole, Hazel.SendOption.Reliable, -1);
                    writer2.Write((byte)RoleId.Swooper);
                    writer2.Write(playerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    RPCProcedure.setRole((byte)RoleId.Swooper, playerId);
                }
            }

            return playerId;
        }

        private static byte setModifierToRandomPlayer(byte modifierId, List<PlayerControl> playerList, byte flag = 0) {
            if (playerList.Count == 0) return Byte.MaxValue;
            var index = rnd.Next(0, playerList.Count);
            byte playerId = playerList[index].PlayerId;
            playerList.RemoveAt(index);

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetModifier, Hazel.SendOption.Reliable, -1);
            writer.Write(modifierId);
            writer.Write(playerId);
            writer.Write(flag);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.setModifier(modifierId, playerId, flag);
            return playerId;
        }

        private static void assignModifiersToPlayers(List<RoleId> modifiers, List<PlayerControl> playerList, int modifierCount) {
            modifiers = modifiers.OrderBy(x => rnd.Next()).ToList(); // randomize list

            while (modifierCount < modifiers.Count) {
                var index = rnd.Next(0, modifiers.Count);
                modifiers.RemoveAt(index);
            }

            byte playerId;
            // Remove Guesser if isGuesserGamemode
			if (isGuesserGamemode) {
				modifiers.RemoveAll(x => x == RoleId.EvilGuesser);
				modifiers.RemoveAll(x => x == RoleId.NiceGuesser);
			}
            
      //      if (modifiers.Contains(RoleId.EvilGuesser)) {
      //          List<PlayerControl> impPlayer = new List<PlayerControl>(playerList);
      //          impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
      //          playerId = setModifierToRandomPlayer((byte)RoleId.EvilGuesser, impPlayer);
     //           playerList.RemoveAll(x => x.PlayerId == playerId);
     //           modifiers.RemoveAll(x => x == RoleId.EvilGuesser);
     //       }

            if (modifiers.Contains(RoleId.EvilGuesser)) {
                List<PlayerControl> impPlayer = new List<PlayerControl>(playerList); //testing
                impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
                
                int assassinCount = 0;
                while (assassinCount < modifiers.FindAll(x => x == RoleId.EvilGuesser).Count) {
                    playerId = setModifierToRandomPlayer((byte)RoleId.EvilGuesser, impPlayer);
                 //   crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                    playerList.RemoveAll(x => x.PlayerId == playerId);
                    assassinCount++;
                }
                modifiers.RemoveAll(x => x == RoleId.EvilGuesser);
            }

            if (modifiers.Contains(RoleId.Disperser)) {
                List<PlayerControl> impPlayer = new List<PlayerControl>(playerList); //testing
                impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
                    playerId = setModifierToRandomPlayer((byte)RoleId.Disperser, impPlayer);
                 //   crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                    playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Disperser);
            }

            
      //      if (modifiers.Contains(RoleId.NiceGuesser)) {
      //          List<PlayerControl> crewPlayerNG = new List<PlayerControl>(playerList);
      //          crewPlayerNG.RemoveAll(x => x.Data.Role.IsImpostor);
      //          playerId = setModifierToRandomPlayer((byte)RoleId.NiceGuesser, crewPlayerNG);
      //          playerList.RemoveAll(x => x.PlayerId == playerId);
      //          modifiers.RemoveAll(x => x == RoleId.NiceGuesser);
      //      }


            if (modifiers.Contains(RoleId.Cursed)) {
                List<PlayerControl> crewPlayerC = new List<PlayerControl>(playerList);
                crewPlayerC.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                playerId = setModifierToRandomPlayer((byte)RoleId.Cursed, crewPlayerC);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Cursed);
            }


            if (modifiers.Contains(RoleId.Tunneler)) {
                List<PlayerControl> crewPlayerT = new List<PlayerControl>(playerList);
                crewPlayerT.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral) || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.roleId == RoleId.Engineer));
                playerId = setModifierToRandomPlayer((byte)RoleId.Tunneler, crewPlayerT);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Tunneler);
            }

            if (modifiers.Contains(RoleId.Chameleon)) {
                List<PlayerControl> crewPlayerCh = new List<PlayerControl>(playerList);
                crewPlayerCh.RemoveAll(x => RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                playerId = setModifierToRandomPlayer((byte)RoleId.Chameleon, crewPlayerCh);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Chameleon);
            }

            if (modifiers.Contains(RoleId.Watcher)) {
                List<PlayerControl> crewPlayerW = new List<PlayerControl>(playerList);
                crewPlayerW.RemoveAll(x => x.Data.Role.IsImpostor);
                playerId = setModifierToRandomPlayer((byte)RoleId.Watcher, crewPlayerW);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Watcher);
            }

            if (modifiers.Contains(RoleId.PhantomAbility)) {
                List<PlayerControl> neutralPlayer = new List<PlayerControl>(playerList); //testing
                neutralPlayer.RemoveAll(x => !Helpers.isNeutral(x));
                playerId = setModifierToRandomPlayer((byte)RoleId.PhantomAbility, neutralPlayer);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.PhantomAbility);
            }

     //       if (modifiers.Contains(RoleId.LifeGuard)) {
     //           List<PlayerControl> crewPlayerLG = new List<PlayerControl>(playerList);
     //           crewPlayerLG.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
     //           playerId = setModifierToRandomPlayer((byte)RoleId.LifeGuard, crewPlayerLG);
     //           playerList.RemoveAll(x => x.PlayerId == playerId);
     //           modifiers.RemoveAll(x => x == RoleId.LifeGuard);
     //       }
/*
            if (modifiers.Contains(RoleId.Indomitable)) {
                List<PlayerControl> crewPlayerI = new List<PlayerControl>(playerList);
                crewPlayerI.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                playerId = setModifierToRandomPlayer((byte)RoleId.Indomitable, crewPlayerI);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Indomitable);
            }

            if (modifiers.Contains(RoleId.Bait)) {
                List<PlayerControl> crewPlayerB = new List<PlayerControl>(playerList);
                crewPlayerB.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                playerId = setModifierToRandomPlayer((byte)RoleId.Bait, crewPlayerB);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Bait);
            }

            if (modifiers.Contains(RoleId.Blind)) {
                List<PlayerControl> crewPlayerBl = new List<PlayerControl>(playerList);
                crewPlayerBl.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
                playerId = setModifierToRandomPlayer((byte)RoleId.Blind, crewPlayerBl);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Blind);
            }
*/

            modifiers.RemoveAll(x => x == RoleId.NiceGuesser);
            modifiers.RemoveAll(x => x == RoleId.EvilGuesser); //testing
			modifiers.RemoveAll(x => x == RoleId.Cursed);
			
            List<PlayerControl> crewPlayer = new List<PlayerControl>(playerList);
            crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
            if (modifiers.Contains(RoleId.Shifter)) {
                var crewPlayerShifter = new List<PlayerControl>(crewPlayer);
                crewPlayerShifter.RemoveAll(x => x == Spy.spy);
                playerId = setModifierToRandomPlayer((byte)RoleId.Shifter, crewPlayerShifter);
                crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                playerList.RemoveAll(x => x.PlayerId == playerId);
                modifiers.RemoveAll(x => x == RoleId.Shifter);
            }
            if (modifiers.Contains(RoleId.Sunglasses)) {
                int sunglassesCount = 0;
                while (sunglassesCount < modifiers.FindAll(x => x == RoleId.Sunglasses).Count) {
                    playerId = setModifierToRandomPlayer((byte)RoleId.Sunglasses, crewPlayer);
                    crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                    playerList.RemoveAll(x => x.PlayerId == playerId);
                    sunglassesCount++;
                }
                modifiers.RemoveAll(x => x == RoleId.Sunglasses);
            }

            if (modifiers.Contains(RoleId.Torch)) {
                int torchCount = 0;
                while (torchCount < modifiers.FindAll(x => x == RoleId.Torch).Count) {
                    playerId = setModifierToRandomPlayer((byte)RoleId.Torch, crewPlayer);
                    crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                    playerList.RemoveAll(x => x.PlayerId == playerId);
                    torchCount++;
                }
                modifiers.RemoveAll(x => x == RoleId.Torch);
            }

            if (modifiers.Contains(RoleId.Multitasker)) {
                int multitaskerCount = 0;
                while (multitaskerCount < modifiers.FindAll(x => x == RoleId.Multitasker).Count) {
                    playerId = setModifierToRandomPlayer((byte)RoleId.Multitasker, crewPlayer);
                    crewPlayer.RemoveAll(x => x.PlayerId == playerId);
                    playerList.RemoveAll(x => x.PlayerId == playerId);
                    multitaskerCount++;
                }
                modifiers.RemoveAll(x => x == RoleId.Multitasker);
            }

   //         if (modifiers.Contains(RoleId.LifeGuard)) {
   //             crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || RoleInfo.getRoleInfoForPlayer(x).Any(r => r.isNeutral));
   //             playerId = setModifierToRandomPlayer((byte)RoleId.LifeGuard, crewPlayer);
   //             playerList.RemoveAll(x => x.PlayerId == playerId);
   //             modifiers.RemoveAll(x => x == RoleId.LifeGuard);
   //         }




            foreach (RoleId modifier in modifiers) {
                if (playerList.Count == 0) break;
                playerId = setModifierToRandomPlayer((byte)modifier, playerList);
                playerList.RemoveAll(x => x.PlayerId == playerId);
            }
        }

        private static int getSelectionForRoleId(RoleId roleId, bool multiplyQuantity = false) {
            int selection = 0;
            switch (roleId) {
                case RoleId.Lover:
                    selection = CustomOptionHolder.modifierLover.getSelection(); break;
                case RoleId.Tiebreaker:
                    selection = CustomOptionHolder.modifierTieBreaker.getSelection(); break;
                case RoleId.Indomitable:
                    selection = CustomOptionHolder.modifierIndomitable.getSelection(); break;
                case RoleId.Cursed:
                    selection = CustomOptionHolder.modifierCursed.getSelection(); break;
                case RoleId.Slueth:
                    selection = CustomOptionHolder.modifierSlueth.getSelection(); break;
                case RoleId.Blind:
                    selection = CustomOptionHolder.modifierBlind.getSelection(); break;
                case RoleId.Watcher:
                    selection = CustomOptionHolder.modifierWatcher.getSelection(); break;
                case RoleId.Radar:
                    selection = CustomOptionHolder.modifierRadar.getSelection(); break;
                case RoleId.Disperser:
                    selection = CustomOptionHolder.modifierDisperser.getSelection(); break;
                case RoleId.Mini:
                    selection = CustomOptionHolder.modifierMini.getSelection(); break;
     //           case RoleId.EvilGuesser:
     //               selection = 0;
     //               if (CustomOptionHolder.guesserSpawnRate.getBool())
     //                   selection = CustomOptionHolder.guesserIsImpGuesserRate.getSelection();
     //               break;
      //          case RoleId.NiceGuesser:
     //               selection = 0;
    //                if (CustomOptionHolder.guesserSpawnRate.getBool())
    //                    selection = CustomOptionHolder.guesserSpawnBothRate.getSelection(); break;
                case RoleId.Bait:
                    selection = CustomOptionHolder.modifierBait.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierBaitQuantity.getQuantity();
                    break;
                case RoleId.Bloody:
                    selection = CustomOptionHolder.modifierBloody.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierBloodyQuantity.getQuantity();
                    break;
                case RoleId.AntiTeleport:
                    selection = CustomOptionHolder.modifierAntiTeleport.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierAntiTeleportQuantity.getQuantity();
                    break;
      //          case RoleId.LifeGuard:
      //              selection = CustomOptionHolder.modifierLifeGuard.getSelection(); break;
                case RoleId.Tunneler:
                    selection = CustomOptionHolder.modifierTunneler.getSelection(); break;
       //         case RoleId.Sunglasses:
       //             selection = CustomOptionHolder.modifierSunglasses.getSelection();
      //              if (multiplyQuantity) selection *= CustomOptionHolder.modifierSunglassesQuantity.getQuantity();
     //               break;
                case RoleId.Torch:
                    selection = CustomOptionHolder.modifierTorch.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierTorchQuantity.getQuantity();
                    break;
                case RoleId.Multitasker:
                    selection = CustomOptionHolder.modifierMultitasker.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierMultitaskerQuantity.getQuantity();
                    break;
                case RoleId.Vip:
                    selection = CustomOptionHolder.modifierVip.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierVipQuantity.getQuantity();
                    break;
                case RoleId.Invert:
                    selection = CustomOptionHolder.modifierInvert.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierInvertQuantity.getQuantity();
                    break;
                case RoleId.Chameleon:
                    selection = CustomOptionHolder.modifierChameleon.getSelection();
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierChameleonQuantity.getQuantity();
                    break;
                case RoleId.PhantomAbility:
                    selection = CustomOptionHolder.modifierPhantomAbility.getSelection();
                    break;
      //          case RoleId.Shifter:
     //               selection = CustomOptionHolder.modifierShifter.getSelection(); break;
                case RoleId.EvilGuesser:
                    selection = CustomOptionHolder.modifierAssassin.getSelection();
                    if (!Cultist.isCultistGame){
                    if (multiplyQuantity) selection *= CustomOptionHolder.modifierAssassinQuantity.getQuantity();
                    }
                    break; 
            }
                 
            return selection;
        }

     //   private static void setRolesAgain()
    //    {

     //       while (playerRoleMap.Any())
     //       {
//                byte amount = (byte)Math.Min(playerRoleMap.Count, 20);
    //            var writer = AmongUsClient.Instance!.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.WorkaroundSetRoles, SendOption.Reliable, -1);
   //             writer.Write(amount);
   //             for (int i = 0; i < amount; i++)
   //             {
   //                 var option = playerRoleMap[0];
   //                 playerRoleMap.RemoveAt(0);
   //                 writer.WritePacked((uint)option.Item1);
   //                 writer.WritePacked((uint)option.Item2);
  //              }
   //             AmongUsClient.Instance.FinishRpcImmediately(writer);
 //           }
 //       }
//
        public class RoleAssignmentData {
            public List<PlayerControl> crewmates {get;set;}
            public List<PlayerControl> impostors {get;set;}
            public Dictionary<byte, int> impSettings = new Dictionary<byte, int>();
            public Dictionary<byte, int> neutralSettings = new Dictionary<byte, int>();
            public Dictionary<byte, int> crewSettings = new Dictionary<byte, int>();
            public int maxCrewmateRoles {get;set;}
            public int maxNeutralRoles {get;set;}
            public int maxImpostorRoles {get;set;}
        }
        
        private enum RoleType {
            Crewmate = 0,
            Neutral = 1,
            Impostor = 2
        }

    }
}
