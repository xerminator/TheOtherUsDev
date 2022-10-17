using System.Linq;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using TheOtherRoles.Objects;
using TheOtherRoles.Players;
using System.Collections;
using TMPro;
using TheOtherRoles.Utilities;
using static TheOtherRoles.TheOtherRoles;
using Object = UnityEngine.Object;
using InnerNet;
using Hazel;
using UnhollowerBaseLib;

namespace TheOtherRoles
{
    public static class Utils
    {
        public static PlayerControl GetPlayerById(int PlayerId)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }
        public static PlayerControl PlayerById(int PlayerId)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }

        public static IEnumerator FlashCoroutine(Color color, float waitfor = 1f, float alpha = 0.3f)
        {
            color.a = alpha;
            if (HudManager.InstanceExists && HudManager.Instance.FullScreen)
            {
                var fullscreen = DestroyableSingleton<HudManager>.Instance.FullScreen;
                fullscreen.enabled = true;
                fullscreen.gameObject.active = true;
                fullscreen.color = color;
            }

            yield return new WaitForSeconds(waitfor);

            if (HudManager.InstanceExists && HudManager.Instance.FullScreen)
            {
                var fullscreen = DestroyableSingleton<HudManager>.Instance.FullScreen;
                if (fullscreen.color.Equals(color))
                {
                    fullscreen.color = new Color(1f, 0f, 0f, 0.37254903f);
                    fullscreen.enabled = false;
                }
            }
        }
    }
    public static class TransporterUtils
    {
        public static IEnumerator TransportPlayers(byte player1, byte player2, bool die)
        {
            var TP1 = Utils.PlayerById(player1);
            var TP2 = Utils.PlayerById(player2);
            var deadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            DeadBody Player1Body = null;
            DeadBody Player2Body = null;

            if (TP1.Data.IsDead)
                foreach (var body in deadBodies)
                    if (body.ParentId == TP1.PlayerId)
                        Player1Body = body;
            if (TP2.Data.IsDead)
                foreach (var body in deadBodies)
                    if (body.ParentId == TP2.PlayerId)
                        Player2Body = body;

            if (TP1.inVent && CachedPlayer.LocalPlayer.PlayerId == TP1.PlayerId)
            {
                while (SubmergedCompatibility.getInTransition())
                {
                    yield return null;
                }
                TP1.MyPhysics.ExitAllVents();
            }
            if (TP2.inVent && CachedPlayer.LocalPlayer.PlayerId == TP2.PlayerId)
            {
                while (SubmergedCompatibility.getInTransition())
                {
                    yield return null;
                }
                TP2.MyPhysics.ExitAllVents();
            }

            if (Player1Body == null && Player2Body == null)
            {
                TP1.MyPhysics.ResetMoveState();
                TP2.MyPhysics.ResetMoveState();
                var TempPosition = TP1.GetTruePosition();
                TP1.NetTransform.SnapTo(new Vector2(TP2.GetTruePosition().x, TP2.GetTruePosition().y + 0.3636f));
                if (die) TP1.RpcMurderPlayer(TP2);
                else
                {
                    TP2.NetTransform.SnapTo(new Vector2(TempPosition.x, TempPosition.y + 0.3636f));
                    //TP2.myRend().flipX = TempFacing;
                }    
            }
            else if (Player1Body != null && Player2Body == null)
            {
                StopDragging(Player1Body.ParentId);
                TP2.MyPhysics.ResetMoveState();
                var TempPosition = Player1Body.TruePosition;
                Player1Body.transform.position = TP2.GetTruePosition();
                TP2.NetTransform.SnapTo(new Vector2(TempPosition.x, TempPosition.y + 0.3636f));
            }
            else if (Player1Body == null && Player2Body != null)
            {
                StopDragging(Player2Body.ParentId);
                TP1.MyPhysics.ResetMoveState();
                var TempPosition = TP1.GetTruePosition();
                TP1.NetTransform.SnapTo(new Vector2(Player2Body.TruePosition.x, Player2Body.TruePosition.y + 0.3636f));
                Player2Body.transform.position = TempPosition;
            }
            else if (Player1Body != null && Player2Body != null)
            {
                StopDragging(Player1Body.ParentId);
                StopDragging(Player2Body.ParentId);
                var TempPosition = Player1Body.TruePosition;
                Player1Body.transform.position = Player2Body.TruePosition;
                Player2Body.transform.position = TempPosition;
            }

            if (CachedPlayer.LocalPlayer.PlayerId == TP1.PlayerId ||
                CachedPlayer.LocalPlayer.PlayerId == TP2.PlayerId)
            {
                //Coroutines.Start(Utils.FlashCoroutine(Transporter.color));
                //DestroyableSingleton<HudManager>.Instance.StartCoroutine_Auto(Utils.FlashCoroutine(Transporter.color));
                if (Minigame.Instance) Minigame.Instance.Close();
            }

            TP1.moveable = true;
            TP2.moveable = true;
            TP1.Collider.enabled = true;
            TP2.Collider.enabled = true;
            TP1.NetTransform.enabled = true;
            TP2.NetTransform.enabled = true;
        }

        public static void StopDragging(byte PlayerId)
        {
            if (Undertaker.undertaker != null && PlayerId == Undertaker.undertaker.PlayerId)
            {
                Undertaker.isDraging =false;
                Undertaker.deadBodyDraged = null;
            }
        }

        public static void Update(HudManager __instance)
        {
            FixedUpdate(__instance);
        }

        public static void FixedUpdate(HudManager __instance)
        {
            if (Transporter.PressedButton && Transporter.TransportList == null)
            {
                Transporter.TransportPlayer1 = null;
                Transporter.TransportPlayer2 = null;

                __instance.Chat.SetVisible(false);
                Transporter.TransportList = Object.Instantiate(__instance.Chat);

                Transporter.TransportList.transform.SetParent(Camera.main.transform);
                Transporter.TransportList.SetVisible(true);
                Transporter.TransportList.Toggle();

                Transporter.TransportList.TextBubble.enabled = false;
                Transporter.TransportList.TextBubble.gameObject.SetActive(false);

                Transporter.TransportList.TextArea.enabled = false;
                Transporter.TransportList.TextArea.gameObject.SetActive(false);

                Transporter.TransportList.BanButton.enabled = false;
                Transporter.TransportList.BanButton.gameObject.SetActive(false);

                Transporter.TransportList.CharCount.enabled = false;
                Transporter.TransportList.CharCount.gameObject.SetActive(false);

                Transporter.TransportList.OpenKeyboardButton.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().enabled = false;
                //Transporter.TransportList.OpenKeyboardButton.Destroy();
                Object.Destroy(Transporter.TransportList.OpenKeyboardButton);

                Transporter.TransportList.gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>()
                    .enabled = false;
                Transporter.TransportList.gameObject.transform.GetChild(0).gameObject.SetActive(false);

                Transporter.TransportList.BackgroundImage.enabled = false;

                foreach (var rend in Transporter.TransportList.Content
                    .GetComponentsInChildren<SpriteRenderer>())
                    if (rend.name == "SendButton" || rend.name == "QuickChatButton")
                    {
                        rend.enabled = false;
                        rend.gameObject.SetActive(false);
                    }

                foreach (var bubble in Transporter.TransportList.chatBubPool.activeChildren)
                {
                    bubble.enabled = false;
                    bubble.gameObject.SetActive(false);
                }

                Transporter.TransportList.chatBubPool.activeChildren.Clear();

                foreach (var TempPlayer in PlayerControl.AllPlayerControls)
                {
                    if (TempPlayer != null &&
                        TempPlayer.Data != null &&
                        !TempPlayer.Data.IsDead &&
                        !TempPlayer.Data.Disconnected &&
                        TempPlayer.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    {
                        foreach (var player in PlayerControl.AllPlayerControls)
                        {
                            if (player != null &&
                                player.Data != null &&
                                ((!player.Data.Disconnected && !player.Data.IsDead) ||
                                Object.FindObjectsOfType<DeadBody>().Any(x => x.ParentId == player.PlayerId)))
                            {
                                Transporter.TransportList.AddChat(TempPlayer, "Click here");
                                Transporter.TransportList.chatBubPool.activeChildren[Transporter.TransportList.chatBubPool.activeChildren._size - 1].Cast<ChatBubble>().SetName(player.Data.PlayerName, false, false,
                                    PlayerControl.LocalPlayer.PlayerId == player.PlayerId ? Transporter.color : Color.white);
                                var IsDeadTemp = player.Data.IsDead;
                                player.Data.IsDead = false;
                                Transporter.TransportList.chatBubPool.activeChildren[Transporter.TransportList.chatBubPool.activeChildren._size - 1].Cast<ChatBubble>().SetCosmetics(player.Data);
                                player.Data.IsDead = IsDeadTemp;
                            }
                        }
                        break;
                    }
                }
            }
            if (Transporter.TransportList != null)
            {
                if (Minigame.Instance)
                    Minigame.Instance.Close();

                if (!Transporter.TransportList.IsOpen || MeetingHud.Instance || Input.GetKeyInt(KeyCode.Escape) || PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Transporter.TransportList.Toggle();
                    Transporter.TransportList.SetVisible(false);
                    Transporter.TransportList = null;
                    Transporter.PressedButton = false;
                    Transporter.TransportPlayer1 = null;
                }
                else
                {
                    foreach (var bubble in Transporter.TransportList.chatBubPool.activeChildren)
                    {
                        if (Transporter.button.Timer == 0f && Transporter.TransportList != null)
                        {
                            Vector2 ScreenMin =
                                Camera.main.WorldToScreenPoint(bubble.Cast<ChatBubble>().Background.bounds.min);
                            Vector2 ScreenMax =
                                Camera.main.WorldToScreenPoint(bubble.Cast<ChatBubble>().Background.bounds.max);
                            if (Input.mousePosition.x > ScreenMin.x && Input.mousePosition.x < ScreenMax.x)
                            {
                                if (Input.mousePosition.y > ScreenMin.y && Input.mousePosition.y < ScreenMax.y)
                                {
                                    if (!Input.GetMouseButtonDown(0) && Transporter.LastMouse)
                                    {
                                        Transporter.LastMouse = false;
                                        foreach (var player in PlayerControl.AllPlayerControls)
                                        {
                                            if (player.Data.PlayerName == bubble.Cast<ChatBubble>().NameText.text)
                                            {
                                                if (Transporter.TransportPlayer1 == null)
                                                {
                                                    Transporter.TransportPlayer1 = player;
                                                    bubble.Cast<ChatBubble>().Background.color = Color.green;
                                                }
                                                else if (player.PlayerId == Transporter.TransportPlayer1.PlayerId)
                                                {
                                                    Transporter.TransportPlayer1 = null;
                                                    bubble.Cast<ChatBubble>().Background.color = Color.white;
                                                }
                                                else
                                                {
                                                    Transporter.PressedButton = false;
                                                    Transporter.TransportList.Toggle();
                                                    Transporter.TransportList.SetVisible(false);
                                                    Transporter.TransportList = null;

                                                    Transporter.TransportPlayer2 = player;

                                                    if (!Transporter.UntransportablePlayers.ContainsKey(Transporter.TransportPlayer1.PlayerId) && !Transporter.UntransportablePlayers.ContainsKey(Transporter.TransportPlayer2.PlayerId))
                                                    {   
                                                        if (Veteren.veteren != null && Transporter.TransportPlayer1 == Veteren.veteren && Veteren.alertActive)
                                                        {
                                                            if (Medic.shielded == player && Medic.shielded != null)
                                                            {
                                                               MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(Transporter.transporter.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
                                                                writer.Write(Transporter.TransportPlayer1.PlayerId);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                                                RPCProcedure.shieldedMurderAttempt(Transporter.transporter.PlayerId);
                                                                return;
                                                            }
                                                            else if (BodyGuard.guarded != null && BodyGuard.guarded == player)
                                                            {
                                                                // Kill the Transporter
                                                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
                                                                writer.Write(Transporter.transporter.PlayerId);
                                                                writer.Write(Transporter.transporter.PlayerId);
                                                                writer.Write(true ? Byte.MaxValue : 0);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                                                RPCProcedure.uncheckedMurderPlayer(Transporter.transporter.PlayerId, Transporter.transporter.PlayerId, true ? Byte.MaxValue : (byte)0);
                
                                                                // Kill the BodyGuard
                                                                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
                                                                writer2.Write(BodyGuard.bodyguard.PlayerId);
                                                                writer2.Write(BodyGuard.bodyguard.PlayerId);
                                                                writer2.Write(true ? Byte.MaxValue : 0);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer2);
                                                                RPCProcedure.uncheckedMurderPlayer(BodyGuard.bodyguard.PlayerId, BodyGuard.bodyguard.PlayerId, true ? Byte.MaxValue : (byte)0);

                                                                MessageWriter writer3 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ShowBodyGuardFlash, Hazel.SendOption.Reliable, -1);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer3);
                                                                RPCProcedure.showBodyGuardFlash();
                                                                return;
                                                            }
                                                            else {
                                                                TransportPlayers(Transporter.TransportPlayer1.PlayerId, Transporter.TransportPlayer2.PlayerId, false);
                                                                var write2 = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                                                                    (byte)CustomRPC.Transport, SendOption.Reliable, -1);
                                                                write2.Write(Transporter.TransportPlayer1.PlayerId);
                                                                write2.Write(player.PlayerId);
                                                                write2.Write(true);
                                                                AmongUsClient.Instance.FinishRpcImmediately(write2);
                                                            }
                                                            return;
                                                        }
                                                        else if (Veteren.veteren != null && Transporter.TransportPlayer2 == Veteren.veteren && Veteren.alertActive)
                                                        {
                                                            if (Medic.shielded == player && Medic.shielded != null)
                                                            {
                                                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(Transporter.transporter.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
                                                                writer.Write(Transporter.TransportPlayer2.PlayerId);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                                                RPCProcedure.shieldedMurderAttempt(Transporter.transporter.PlayerId);
                                                               return;
                                                            }
                                                            else if (BodyGuard.guarded != null && BodyGuard.guarded == player)
                                                            {
                                                                // Kill the Transporter
                                                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
                                                                writer.Write(Transporter.transporter.PlayerId);
                                                                writer.Write(Transporter.transporter.PlayerId);
                                                                writer.Write(true ? Byte.MaxValue : 0);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                                                RPCProcedure.uncheckedMurderPlayer(Transporter.transporter.PlayerId, Transporter.transporter.PlayerId, true ? Byte.MaxValue : (byte)0);
                
                                                                // Kill the BodyGuard
                                                                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
                                                                writer2.Write(BodyGuard.bodyguard.PlayerId);
                                                                writer2.Write(BodyGuard.bodyguard.PlayerId);
                                                                writer2.Write(true ? Byte.MaxValue : 0);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer2);
                                                                RPCProcedure.uncheckedMurderPlayer(BodyGuard.bodyguard.PlayerId, BodyGuard.bodyguard.PlayerId, true ? Byte.MaxValue : (byte)0);

                                                                MessageWriter writer3 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ShowBodyGuardFlash, Hazel.SendOption.Reliable, -1);
                                                                AmongUsClient.Instance.FinishRpcImmediately(writer3);
                                                                RPCProcedure.showBodyGuardFlash();
                                                                return;
                                                            }
                                                            else {
                                                                TransportPlayers(Transporter.TransportPlayer1.PlayerId, player.PlayerId, false);
                                                                var write2 = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                                                                    (byte)CustomRPC.Transport, SendOption.Reliable, -1);
                                                                write2.Write(Transporter.TransportPlayer2.PlayerId);
                                                                write2.Write(player.PlayerId);
                                                                write2.Write(true);
                                                                AmongUsClient.Instance.FinishRpcImmediately(write2);
                                                            }
                                                            return;
                                                        }
                                                        Transporter.button.Timer = Transporter.button.MaxTimer;
                                                        Transporter.transportsLeft--;

                                                        TransportPlayers(Transporter.TransportPlayer1.PlayerId, Transporter.TransportPlayer2.PlayerId, false);

                                                        var write = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                                                            (byte)CustomRPC.Transport, SendOption.Reliable, -1);
                                                        write.Write(Transporter.TransportPlayer1.PlayerId);
                                                        write.Write(Transporter.TransportPlayer2.PlayerId);
                                                        write.Write(false);
                                                        AmongUsClient.Instance.FinishRpcImmediately(write);
                                                    }
                                                    else
                                                    {
                                                        (__instance as MonoBehaviour).StartCoroutine(Effects.SwayX(Transporter.button.hudManager.transform));
                                                    }

                                                    Transporter.TransportPlayer1 = null;
                                                    Transporter.TransportPlayer2 = null;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!Input.GetMouseButtonDown(0) && Transporter.LastMouse)
                    {
                        if (Transporter.MenuClick)
                            Transporter.MenuClick = false;
                        else {
                            Transporter.TransportList.Toggle();
                            Transporter.TransportList.SetVisible(false);
                            Transporter.TransportList = null;
                            Transporter.PressedButton = false;
                            Transporter.TransportPlayer1 = null;
                        }
                    }
                    Transporter.LastMouse = Input.GetMouseButtonDown(0);
                }
            }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class Update
    {
        private static void Postfix(HudManager __instance)
        {
            if (PlayerControl.AllPlayerControls.Count <= 1) return;
            if (CachedPlayer.LocalPlayer == null) return;
            if (CachedPlayer.LocalPlayer.Data == null) return;
            if (Transporter.transporter.PlayerId != CachedPlayer.LocalPlayer.PlayerId) return;
            if (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
                    if (Transporter.transporter.PlayerId == CachedPlayer.LocalPlayer.PlayerId)
                        TransporterUtils.Update(__instance);
        }
    }

     [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public class UntransportableUpdate
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (PlayerControl.AllPlayerControls.Count <= 1) return;
                if (PlayerControl.LocalPlayer == null) return;
                if (PlayerControl.LocalPlayer.Data == null) return;
                if (PlayerControl.LocalPlayer.Data.IsDead) return;
                if (!GameData.Instance) return;
                if (Transporter.transporter.PlayerId != CachedPlayer.LocalPlayer.PlayerId) return;

                foreach (var entry in Transporter.UntransportablePlayers)
                {
                    var player = Utils.PlayerById(entry.Key);
                    // System.Console.WriteLine(entry.Key+" is out of bounds");
                    if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;

                    if (Transporter.UntransportablePlayers.ContainsKey(player.PlayerId) && player.moveable == true &&
                        Transporter.UntransportablePlayers.GetValueSafe(player.PlayerId).AddSeconds(0.5) < DateTime.UtcNow)
                    {
                        Transporter.UntransportablePlayers.Remove(player.PlayerId);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ClimbLadder))]
        public class SaveLadderPlayer
        {
            public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] Ladder source, [HarmonyArgument(1)] byte climbLadderSid)
            {
                if (Transporter.transporter.PlayerId == CachedPlayer.LocalPlayer.PlayerId)
                    Transporter.UntransportablePlayers.Add(__instance.myPlayer.PlayerId, DateTime.UtcNow);
                    else
                {
                    var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte) CustomRPC.SetUntransportable, SendOption.Reliable, -1);
                    writer.Write(PlayerControl.LocalPlayer.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
        }

        [HarmonyPatch(typeof(MovingPlatformBehaviour), nameof(MovingPlatformBehaviour.Use), new Type[] {})]
        public class SavePlatformPlayer
        {
            public static void Prefix(MovingPlatformBehaviour __instance)
            {
                // System.Console.WriteLine(PlayerControl.LocalPlayer.PlayerId+" used the platform.");
                if (Transporter.transporter.PlayerId == CachedPlayer.LocalPlayer.PlayerId)
                {
                    Transporter.UntransportablePlayers.Add(PlayerControl.LocalPlayer.PlayerId, DateTime.UtcNow);
                }
                else
                {
                    var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte) CustomRPC.SetUntransportable, SendOption.Reliable, -1);
                    writer.Write(PlayerControl.LocalPlayer.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
        }
}