using HarmonyLib;
using System;
using UnityEngine;
using static TheOtherRoles.TheOtherRoles;
using TheOtherRoles.Objects;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Players;
using TheOtherRoles.Utilities;
using TheOtherRoles.CustomGameModes;
using Hazel;

namespace TheOtherRoles.Patches {
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    class HudManagerUpdatePatch
    {
        private static Dictionary<byte, (string name, Color color)> TagColorDict = new();
        static void resetNameTagsAndColors() {
            var localPlayer = CachedPlayer.LocalPlayer.PlayerControl;
            var myData = CachedPlayer.LocalPlayer.Data;
            var amImpostor = myData.Role.IsImpostor;
            var morphTimerNotUp = Morphling.morphTimer > 0f;
            var morphTargetNotNull = Morphling.morphTarget != null;

            var dict = TagColorDict;
            dict.Clear();
            
            foreach (var data in GameData.Instance.AllPlayers.GetFastEnumerator())
            {
                var player = data.Object;
                string text = data.PlayerName;
                Color color;
                if (player)
                {
                    var playerName = text;
                    if (morphTimerNotUp && morphTargetNotNull && Morphling.morphling == player) playerName = Morphling.morphTarget.Data.PlayerName;
                    var nameText = player.cosmetics.nameText;
                
                    nameText.text = Helpers.hidePlayerName(localPlayer, player) ? "" : playerName;
                    nameText.color = color = amImpostor && data.Role.IsImpostor ? Palette.ImpostorRed : Color.white;
                    nameText.color = nameText.color.SetAlpha(Chameleon.visibility(player.PlayerId));
                }
                else
                {
                    color = Color.white;
                }
                
                
                dict.Add(data.PlayerId, (text, color));
            }
            
            if (MeetingHud.Instance != null) 
            {
                foreach (PlayerVoteArea playerVoteArea in MeetingHud.Instance.playerStates)
                {
                    var data = dict[playerVoteArea.TargetPlayerId];
                    var text = playerVoteArea.NameText;
                    text.text = data.name;
                    text.color = data.color;
                }
            }
        }

        static void setPlayerNameColor(PlayerControl p, Color color) {
            p.cosmetics.nameText.color = color.SetAlpha(Chameleon.visibility(p.PlayerId));
            if (MeetingHud.Instance != null)
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                    if (player.NameText != null && p.PlayerId == player.TargetPlayerId)
                        player.NameText.color = color;
        }
        
        static void updateBlindReport() {
            if (Blind.blind != null && CachedPlayer.LocalPlayer.PlayerControl == Blind.blind) {
                DestroyableSingleton<HudManager>.Instance.ReportButton.SetActive(false);
                // Sadly the report button cannot be hidden due to preventing R to report
            }
        }
        
        static void setNameColors() {
            var localPlayer = CachedPlayer.LocalPlayer.PlayerControl;
            var localRole = RoleInfo.getRoleInfoForPlayer(localPlayer, false).FirstOrDefault();
            setPlayerNameColor(localPlayer, localRole.color);

            if (Jester.jester != null && Jester.jester == localPlayer)
                setPlayerNameColor(Jester.jester, Jester.color);
            else if (Prosecutor.prosecutor != null && Prosecutor.prosecutor == CachedPlayer.LocalPlayer.PlayerControl) { // Make Prosecutor see target
                // Prosecutor can see their target
                setPlayerNameColor(Prosecutor.target, Prosecutor.targetColor);
	    } else if (Mayor.mayor != null && Mayor.mayor == CachedPlayer.LocalPlayer.PlayerControl)
                setPlayerNameColor(Mayor.mayor, Mayor.color);
            else if (Engineer.engineer != null && Engineer.engineer == localPlayer)
                setPlayerNameColor(Engineer.engineer, Engineer.color);
            else if (Sheriff.sheriff != null && Sheriff.sheriff == localPlayer) {
                setPlayerNameColor(Sheriff.sheriff, Sheriff.color);
                if (Deputy.deputy != null && Deputy.knowsSheriff) {
                    setPlayerNameColor(Deputy.deputy, Deputy.color);
                }
            } else
            if (Deputy.deputy != null && Deputy.deputy == localPlayer) {
                setPlayerNameColor(Deputy.deputy, Deputy.color);
                if (Sheriff.sheriff != null && Deputy.knowsSheriff) {
                    setPlayerNameColor(Sheriff.sheriff, Sheriff.color);
                }
            } else if (Portalmaker.portalmaker != null && Portalmaker.portalmaker == localPlayer)
                setPlayerNameColor(Portalmaker.portalmaker, Portalmaker.color);
            else if (Lighter.lighter != null && Lighter.lighter == localPlayer)
                setPlayerNameColor(Lighter.lighter, Lighter.color);
            else if (Detective.detective != null && Detective.detective == localPlayer)
                setPlayerNameColor(Detective.detective, Detective.color);
            else if (TimeMaster.timeMaster != null && TimeMaster.timeMaster == localPlayer)
                setPlayerNameColor(TimeMaster.timeMaster, TimeMaster.color);
            else if (Medic.medic != null && Medic.medic == localPlayer)
                setPlayerNameColor(Medic.medic, Medic.color);
            else if (Swapper.swapper != null && Swapper.swapper == localPlayer)
                setPlayerNameColor(Swapper.swapper, Swapper.color);
            else if (Seer.seer != null && Seer.seer == localPlayer)
                setPlayerNameColor(Seer.seer, Seer.color);
            else if (Hacker.hacker != null && Hacker.hacker == localPlayer)
                setPlayerNameColor(Hacker.hacker, Hacker.color);
            else if (Tracker.tracker != null && Tracker.tracker == localPlayer)
                setPlayerNameColor(Tracker.tracker, Tracker.color);
            else if (Snitch.snitch != null && Snitch.snitch == localPlayer)
                setPlayerNameColor(Snitch.snitch, Snitch.color);
            else if (Jackal.jackal != null && Jackal.jackal == localPlayer) {
                // Jackal can see his sidekick
                if (Jackal.jackal != Swooper.swooper) setPlayerNameColor(Jackal.jackal, Jackal.color);
                if (Jackal.jackal == Swooper.swooper) setPlayerNameColor(Jackal.jackal, Swooper.color);
                if (Sidekick.sidekick != null) {
                    setPlayerNameColor(Sidekick.sidekick, Jackal.color);
                }
                if (Jackal.fakeSidekick != null) {
                    setPlayerNameColor(Jackal.fakeSidekick, Jackal.color);
                }
            }
            else if (Spy.spy != null && Spy.spy == localPlayer) {
                setPlayerNameColor(Spy.spy, Spy.color);
            } else if (SecurityGuard.securityGuard != null && SecurityGuard.securityGuard == localPlayer) {
                setPlayerNameColor(SecurityGuard.securityGuard, SecurityGuard.color);
            } else if (Arsonist.arsonist != null && Arsonist.arsonist == localPlayer) {
                setPlayerNameColor(Arsonist.arsonist, Arsonist.color);
            } else if (Vulture.vulture != null && Vulture.vulture == localPlayer) {
                setPlayerNameColor(Vulture.vulture, Vulture.color);
            } else if (Medium.medium != null && Medium.medium == localPlayer) {
                setPlayerNameColor(Medium.medium, Medium.color);
            } else if (Trapper.trapper != null && Trapper.trapper == localPlayer) {
                setPlayerNameColor(Trapper.trapper, Trapper.color);
            } else if (Lawyer.lawyer != null && Lawyer.lawyer == localPlayer) {
                setPlayerNameColor(Lawyer.lawyer, Lawyer.color);
            } else if (Pursuer.pursuer != null && Pursuer.pursuer == localPlayer) {
                setPlayerNameColor(Pursuer.pursuer, Pursuer.color);
            }
            else if (Swooper.swooper != null && Swooper.swooper == localPlayer) {
                setPlayerNameColor(Swooper.swooper, Swooper.color);
            }

            // No else if here, as a Lover of team Jackal needs the colors
            if (Sidekick.sidekick != null && Sidekick.sidekick == localPlayer) {
                // Sidekick can see the jackal
                setPlayerNameColor(Sidekick.sidekick, Sidekick.color);
                if (Jackal.jackal != null) {
                    if (Jackal.jackal == Swooper.swooper) setPlayerNameColor(Jackal.jackal, Swooper.color);
                    else setPlayerNameColor(Jackal.jackal, Jackal.color);
                }
            }
            
            // No else if here, as the Impostors need the Spy name to be colored
            if (Spy.spy != null && localPlayer.Data.Role.IsImpostor) {
                setPlayerNameColor(Spy.spy, Spy.color);
            }

            if (CachedPlayer.LocalPlayer.Data.IsDead && Prosecutor.prosecutor != null && !Prosecutor.prosecutor.Data.IsDead) {
                setPlayerNameColor(Prosecutor.target, Prosecutor.targetColor);
            }
            
            if (CachedPlayer.LocalPlayer.Data.IsDead && Arsonist.arsonist != null && !Arsonist.arsonist.Data.IsDead) {
                foreach (PlayerControl p in Arsonist.dousedPlayers) {
                    if (p.Data.IsDead || p.Data.Disconnected) continue;
                    setPlayerNameColor(p, Arsonist.color);
                }
            }

            if (Sidekick.sidekick != null && Sidekick.wasTeamRed && localPlayer.Data.Role.IsImpostor) {
                setPlayerNameColor(Sidekick.sidekick, Spy.color);
            }
            if (Jackal.jackal != null && Jackal.wasTeamRed && localPlayer.Data.Role.IsImpostor) {
                setPlayerNameColor(Jackal.jackal, Spy.color);
            }

            // Crewmate roles with no changes: Mini
            // Impostor roles with no changes: Morphling, Camouflager, Vampire, Godfather, Eraser, Janitor, Cleaner, Warlock, BountyHunter,  Witch and Mafioso
        }

        static void setNameTags() {
            // Mafia
            if (CachedPlayer.LocalPlayer != null && CachedPlayer.LocalPlayer.Data.Role.IsImpostor) {
                foreach (PlayerControl player in CachedPlayer.AllPlayers)
                    if (Godfather.godfather != null && Godfather.godfather == player)
                            player.cosmetics.nameText.text = player.Data.PlayerName + " (G)";
                    else if (Mafioso.mafioso != null && Mafioso.mafioso == player)
                            player.cosmetics.nameText.text = player.Data.PlayerName + " (M)";
                    else if (Janitor.janitor != null && Janitor.janitor == player)
                            player.cosmetics.nameText.text = player.Data.PlayerName + " (J)";
                if (MeetingHud.Instance != null)
                    foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                        if (Godfather.godfather != null && Godfather.godfather.PlayerId == player.TargetPlayerId)
                            player.NameText.text = Godfather.godfather.Data.PlayerName + " (G)";
                        else if (Mafioso.mafioso != null && Mafioso.mafioso.PlayerId == player.TargetPlayerId)
                            player.NameText.text = Mafioso.mafioso.Data.PlayerName + " (M)";
                        else if (Janitor.janitor != null && Janitor.janitor.PlayerId == player.TargetPlayerId)
                            player.NameText.text = Janitor.janitor.Data.PlayerName + " (J)";
            }

            // Lovers
            if (Lovers.lover1 != null && Lovers.lover2 != null && (Lovers.lover1 == CachedPlayer.LocalPlayer.PlayerControl || Lovers.lover2 == CachedPlayer.LocalPlayer.PlayerControl)) {
                string suffix = Helpers.cs(Lovers.color, " ♥");
                Lovers.lover1.cosmetics.nameText.text += suffix;
                Lovers.lover2.cosmetics.nameText.text += suffix;

                if (MeetingHud.Instance != null)
                    foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                        if (Lovers.lover1.PlayerId == player.TargetPlayerId || Lovers.lover2.PlayerId == player.TargetPlayerId)
                            player.NameText.text += suffix;
            }
/*
            // Lawyer or Prosecutor
            if ((Lawyer.lawyer != null && Lawyer.target != null && Lawyer.lawyer == CachedPlayer.LocalPlayer.PlayerControl)) {
                Color color = Lawyer.color;
                PlayerControl target = Lawyer.target;
                string suffix = Helpers.cs(color, " §");
                target.cosmetics.nameText.text += suffix;

                if (MeetingHud.Instance != null)
                    foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                        if (player.TargetPlayerId == target.PlayerId)
                            player.NameText.text += suffix;
            }
*/
            bool localIsLawyer = Lawyer.lawyer != null && Lawyer.target != null && Lawyer.lawyer == PlayerControl.LocalPlayer;
            bool localIsKnowingTarget = Lawyer.lawyer != null && Lawyer.target != null && Lawyer.targetKnows && Lawyer.target == PlayerControl.LocalPlayer;
            if (localIsLawyer || (localIsKnowingTarget && !Lawyer.lawyer.Data.IsDead)) {
                string suffix = Helpers.cs(Lawyer.color, " §");
                Lawyer.target.cosmetics.nameText.text += suffix;

                if (MeetingHud.Instance != null)
                    foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                        if (player.TargetPlayerId == Lawyer.target.PlayerId)
                            player.NameText.text += suffix;
            }
/*
            // Lawyer
            bool localIsLawyer = Lawyer.lawyer != null && Lawyer.target != null && Lawyer.lawyer == CachedPlayer.LocalPlayer.PlayerControl;
            bool localIsKnowingTarget = Lawyer.lawyer != null && Lawyer.target != null && Lawyer.targetKnows && Lawyer.target == CachedPlayer.LocalPlayer.PlayerControl;
            if (localIsLawyer || (localIsKnowingTarget && !Lawyer.lawyer.Data.IsDead)) {
                Color color = Lawyer.color;
                PlayerControl target = Lawyer.target;
                string suffix = Helpers.cs(color, " §");
                target.cosmetics.nameText.text += suffix;

                if (MeetingHud.Instance != null)
                    foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                        if (player.TargetPlayerId == target.PlayerId)
                            player.NameText.text += suffix;
            }
*/
            // Former Thief
            if (Thief.formerThief != null && (Thief.formerThief == CachedPlayer.LocalPlayer.PlayerControl || CachedPlayer.LocalPlayer.PlayerControl.Data.IsDead)) {
                string suffix = Helpers.cs(Thief.color, " $");
                Thief.formerThief.cosmetics.nameText.text += suffix;
                if (MeetingHud.Instance != null)
                    foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                        if (player.TargetPlayerId == Thief.formerThief.PlayerId)
                            player.NameText.text += suffix;
            }

            // Display lighter / darker color for all alive players
            if (CachedPlayer.LocalPlayer != null && MeetingHud.Instance != null && MapOptionsTor.showLighterDarker) {
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates) {
                    var target = Helpers.playerById(player.TargetPlayerId);
                    if (target != null)  player.NameText.text += $" ({(Helpers.isLighterColor(target.Data.DefaultOutfit.ColorId) ? "L" : "D")})";
                }
            }
        }
   //     static void shakeScreenIfReactorSabotage() {
 //           if (MapOptionsTor.shakeScreenReactor && Helpers.gameStarted) {
  //              foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks) {
  //                  if (task.TaskType == TaskTypes.ResetReactor || task.TaskType == TaskTypes.ResetSeismic) {
  //                      HudManager.Instance.PlayerCam.shakeAmount = 0.025f;
  //                      HudManager.Instance.PlayerCam.shakePeriod = 400;
  //                  }
 //                       else HudManager.Instance.PlayerCam.shakeAmount = 0f;
  //                      HudManager.Instance.PlayerCam.shakePeriod = 10000000;
  //                  
  //              }
  //          }
  //      }

        static void updateShielded() {
            if (Medic.shielded == null) return;

            if (Medic.shielded.Data.IsDead || Medic.medic == null || Medic.medic.Data.IsDead) {
                Medic.shielded = null;
            }
        }

        static void timerUpdate() {
            var dt = Time.deltaTime;
            Hacker.hackerTimer -= dt;
            Lighter.lighterTimer -= dt;
            Trickster.lightsOutTimer -= dt;
            Tracker.corpsesTrackingTimer -= dt;
            Ninja.invisibleTimer -= dt;
			Swooper.swoopTimer -= dt;
            HideNSeek.timer -= dt;
            foreach (byte key in Deputy.handcuffedKnows.Keys)
                Deputy.handcuffedKnows[key] -= dt;
        }

        public static void miniUpdate() {
            if (Mini.mini == null || Camouflager.camouflageTimer > 0f || Mini.mini == Morphling.morphling && Morphling.morphTimer > 0f || Mini.mini == Ninja.ninja && Ninja.isInvisble || Mini.mini == Swooper.swooper && Swooper.isInvisable || Helpers.isActiveCamoComms()) return;
                
            float growingProgress = Mini.growingProgress();
            float scale = growingProgress * 0.35f + 0.35f;
            string suffix = "";
            if (growingProgress != 1f)
                suffix = " <color=#FAD934FF>(" + Mathf.FloorToInt(growingProgress * 18) + ")</color>"; 
            if (!Mini.isGrowingUpInMeeting && MeetingHud.Instance != null && Mini.ageOnMeetingStart != 0 && !(Mini.ageOnMeetingStart >= 18))
                suffix = " <color=#FAD934FF>(" + Mini.ageOnMeetingStart + ")</color>";

            Mini.mini.cosmetics.nameText.text += suffix;
            if (MeetingHud.Instance != null) {
                foreach (PlayerVoteArea player in MeetingHud.Instance.playerStates)
                    if (player.NameText != null && Mini.mini.PlayerId == player.TargetPlayerId)
                        player.NameText.text += suffix;
            }

            if (Morphling.morphling != null && Morphling.morphTarget == Mini.mini && Morphling.morphTimer > 0f)
                Morphling.morphling.cosmetics.nameText.text += suffix;
        }

        static void updateImpostorKillButton(HudManager __instance) {
            if (!CachedPlayer.LocalPlayer.Data.Role.IsImpostor) return;
            if (MeetingHud.Instance) {
                __instance.KillButton.Hide();
                return;
            }
            bool enabled = true;
            if (Vampire.vampire != null && Vampire.vampire == CachedPlayer.LocalPlayer.PlayerControl)
                enabled = false;
            else if (Mafioso.mafioso != null && Mafioso.mafioso == CachedPlayer.LocalPlayer.PlayerControl && Godfather.godfather != null && !Godfather.godfather.Data.IsDead)
                enabled = false;
            else if (Janitor.janitor != null && Janitor.janitor == CachedPlayer.LocalPlayer.PlayerControl)
                enabled = false;
            else if (Cultist.cultist != null && Cultist.cultist == CachedPlayer.LocalPlayer.PlayerControl && Cultist.needsFollower)
		{
			enabled = false;
		}
            
            if (enabled) __instance.KillButton.Show();
            else __instance.KillButton.Hide();

            if (Deputy.handcuffedKnows.ContainsKey(CachedPlayer.LocalPlayer.PlayerId) && Deputy.handcuffedKnows[CachedPlayer.LocalPlayer.PlayerId] > 0) __instance.KillButton.Hide();
        }

        static void updateReportButton(HudManager __instance) {
            if (Deputy.handcuffedKnows.ContainsKey(CachedPlayer.LocalPlayer.PlayerId) && Deputy.handcuffedKnows[CachedPlayer.LocalPlayer.PlayerId] > 0 || MeetingHud.Instance) __instance.ReportButton.Hide();
            else if (!__instance.ReportButton.isActiveAndEnabled) __instance.ReportButton.Show();
        }
         
        static void updateVentButton(HudManager __instance)
        {
            if (Deputy.handcuffedKnows.ContainsKey(CachedPlayer.LocalPlayer.PlayerId) && Deputy.handcuffedKnows[CachedPlayer.LocalPlayer.PlayerId] > 0 || MeetingHud.Instance) __instance.ImpostorVentButton.Hide();
            else if (CachedPlayer.LocalPlayer.PlayerControl.roleCanUseVents() && !__instance.ImpostorVentButton.isActiveAndEnabled) __instance.ImpostorVentButton.Show();

        }

        static void updateUseButton(HudManager __instance) {
            if (MeetingHud.Instance) __instance.UseButton.Hide();
        }

        static void updateSabotageButton(HudManager __instance) {
            if (MeetingHud.Instance) __instance.SabotageButton.Hide();if (MeetingHud.Instance || MapOptionsTor.gameMode == CustomGamemodes.HideNSeek) __instance.SabotageButton.Hide();
        }

        static void updateMapButton(HudManager __instance) {
            if (Trapper.trapper == null || !(CachedPlayer.LocalPlayer.PlayerId == Trapper.trapper.PlayerId) || __instance == null || __instance.MapButton.HeldButtonSprite == null) return;
            __instance.MapButton.HeldButtonSprite.color = Trapper.playersOnMap.Any() ? Trapper.color : Color.white;
        }
        static void updatePhantom()
        {
            if (PhantomRole.phantomRole == null) return;
            if (CachedPlayer.LocalPlayer.PlayerId == PhantomRole.phantomRole.PlayerId)
            {
                PhantomRole.updateIsMoving();
                //RPCProcedure.updatePhantom();
            }
            PhantomRole.hide();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.updatePhantom, Hazel.SendOption.Reliable, -1);
            //writer.Write(PhantomAbility.phantomAbility.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.updatePhantom();
        }

        static void Postfix(HudManager __instance)
        {
            var player = PlayerControl.LocalPlayer;
            if (player == null) return;
            //壁抜け
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if ((AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started || AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
                    && player.CanMove)
                {
                    player.Collider.offset = new Vector2(0f, 127f);
                }
            }
            //壁抜け解除
            if (player.Collider.offset.y == 127f)
            {
                if (!Input.GetKey(KeyCode.LeftControl) || AmongUsClient.Instance.IsGameStarted)
                {
                    player.Collider.offset = new Vector2(0f, -0.3636f);
                }
            }
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;

            CustomButton.HudUpdate();
            resetNameTagsAndColors();
            setNameColors();
            updateShielded();
            setNameTags();

       //     shakeScreenIfReactorSabotage();

            // Impostors
            updateImpostorKillButton(__instance);
            // Timer updates
            timerUpdate();
            // Mini
            miniUpdate();
            //Phantom
            updatePhantom();

            // Deputy Sabotage, Use and Vent Button Disabling
            updateReportButton(__instance);
            updateVentButton(__instance);
            // Meeting hide buttons if needed (used for the map usage, because closing the map would show buttons)
            updateSabotageButton(__instance);
            updateUseButton(__instance);
            updateBlindReport();
            updateMapButton(__instance);

        }
    }
}
