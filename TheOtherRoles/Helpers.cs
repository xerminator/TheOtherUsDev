using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Linq;
using static TheOtherRoles.TheOtherRoles;
using TheOtherRoles.Modules;
using TheOtherRoles.Objects;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Players;
using TheOtherRoles.Utilities;
using System.Threading.Tasks;
using System.Net;
using TheOtherRoles.CustomGameModes;
using AmongUs.GameOptions;

namespace TheOtherRoles {

    public enum MurderAttemptResult {
        ReverseKill,
	BothKill,
        PerformKill,
        SuppressKill,
        BlankKill,
        BodyGuardKill
    }

    public enum CustomGamemodes {
        Classic,
        Guesser,
        HideNSeek
    }

	public enum SabatageTypes {
		Comms,
		O2,
		Reactor,
		OxyMask,
		Lights,
		None
	}
	
    public static class Helpers
    {

        public static Dictionary<string, Sprite> CachedSprites = new();
        public static Sprite teamJackalChat = null;
        public static Sprite teamCultistChat = null;
        public static Sprite teamLoverChat = null;

        public static bool gameStarted //new
        {
            get
            {
                return AmongUsClient.Instance != null && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started;
            }
        }

            public static Sprite getTeamJackalChatButtonSprite()
            {
		if (teamJackalChat != null)
		{
			return teamJackalChat;
		}
		teamJackalChat = loadSpriteFromResources("TheOtherRoles.Resources.TeamJackalChat.png", 115f);
		return teamJackalChat;
	}

            public static Sprite getTeamCultistChatButtonSprite()
	{
		if (teamCultistChat != null)
		{
			return teamCultistChat;
		}
		teamCultistChat = loadSpriteFromResources("TheOtherRoles.Resources.TeamJackalChat.png", 115f);
		return teamCultistChat;
	}

            public static Sprite getLoversChatButtonSprite() {
		if (teamLoverChat != null)
		{
			return teamLoverChat;
		}
		teamLoverChat = loadSpriteFromResources("TheOtherRoles.Resources.LoversChat.png", 150f);
		return teamLoverChat;
	}


        public static void enableCursor(bool initalSetCursor) {
            if (initalSetCursor) {
                Sprite sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Cursor.png", 115f);
                Cursor.SetCursor(sprite.texture, Vector2.zero, CursorMode.Auto);
                return;
            }
            if (TheOtherRolesPlugin.ToggleCursor.Value) {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else {
                Sprite sprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Cursor.png", 115f);
                Cursor.SetCursor(sprite.texture, Vector2.zero, CursorMode.Auto);
            }
        }


                public static bool flipBitwise(bool bit)
	{
		if (!bit)
		{
			return true;
		}
		return false;
	}
/*
        public static PlayerControl getPartner(this PlayerControl player)
	{
		if (player == null)
		{
			return null;
		}
		if (Lovers.lover1 == player)
		{
			return Lovers.lover2;
		}
		if (Lovers.lover2 == player)
		{
			return Lovers.lover1;
		}
		return null;
	}
    */

    public static PlayerControl getCultistPartner(this PlayerControl player)
	{
		if (player == null)
		{
			return null;
		}
		if (Cultist.cultist == player)
		{
			return Follower.follower;
		}
		if (Follower.follower == player)
		{
			return Cultist.cultist;
		}
		return null;
	}


        public static bool roleCanSabotage(this PlayerControl player) {
            bool roleCouldUse = false;
            if (Jackal.canSabotage) {
                if (player == Jackal.jackal || player == Sidekick.sidekick || Jackal.formerJackals.Contains(player)) {
                    roleCouldUse = true;
                }
            }
            if (player.Data?.Role != null && player.Data.Role.IsImpostor)
                roleCouldUse = true;
            return roleCouldUse;
        }

        public static bool isRoleAlive(PlayerControl role) {
            if (Mimic.mimic != null) {
				if (role == Mimic.mimic) return false;
			}
            return (role != null && isAlive(role));
        }

        public static bool killingCrewAlive() {
            // This functions blocks the game from ending if specified crewmate roles are alive
            if (!CustomOptionHolder.blockGameEnd.getBool()) return false;
            bool powerCrewAlive = false;

            if (isRoleAlive(Sheriff.sheriff)) powerCrewAlive = true;
            if (isRoleAlive(Veteren.veteren)) powerCrewAlive = true;
            if (isRoleAlive(Mayor.mayor)) powerCrewAlive = true;
            if (isRoleAlive(Swapper.swapper)) powerCrewAlive = true;
            if (isRoleAlive(Guesser.niceGuesser)) powerCrewAlive = true;

            return powerCrewAlive;
        }
        public static bool isNeutral(PlayerControl player) {
            RoleInfo roleInfo = RoleInfo.getRoleInfoForPlayer(player, false).FirstOrDefault();
            if (roleInfo != null)
                return roleInfo.isNeutral;
            return false;
        }
		// The above func is probably better?
		
        public static bool isNeutralForPhantom(PlayerControl p) {
            if (p == Jester.jester) return true;
            if (p == Werewolf.werewolf) return true;
            if (p == Prosecutor.prosecutor) return true;
            if (p == Swooper.swooper) return true;
            if (p == Jackal.jackal) return true;
            if (p == Sidekick.sidekick) return true;
            if (p == Arsonist.arsonist) return true;
            if (p == Amnisiac.amnisiac) return true;
            if (p == Vulture.vulture) return true;
            if (p == Lawyer.lawyer) return true;
            if (p == Pursuer.pursuer) return true;
            if (p == Thief.thief) return true;
            return false;
        }
		

		public static SabatageTypes getActiveSabo() {
			foreach (PlayerTask task in CachedPlayer.LocalPlayer.PlayerControl.myTasks.GetFastEnumerator()) {
				if (task.TaskType == TaskTypes.FixLights) {
					return SabatageTypes.Lights;
				} else if (task.TaskType == TaskTypes.RestoreOxy) {
					return SabatageTypes.O2;
				} else if (task.TaskType == TaskTypes.ResetReactor || task.TaskType == TaskTypes.StopCharles || task.TaskType == TaskTypes.StopCharles) {
					return SabatageTypes.Reactor;
				} else if (task.TaskType == TaskTypes.FixComms) {
					return SabatageTypes.Comms;
				} else if (SubmergedCompatibility.IsSubmerged && task.TaskType == SubmergedCompatibility.RetrieveOxygenMask) {
					return SabatageTypes.OxyMask;
				}
			}
			return SabatageTypes.None;
		}

                public static void resetKill(byte playerId) {
                    PlayerControl player = playerById(playerId);
                    player.killTimer = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
                        if (player == Cleaner.cleaner)
                            Cleaner.cleaner.killTimer = HudManagerStartPatch.cleanerCleanButton.Timer = HudManagerStartPatch.cleanerCleanButton.MaxTimer;
                        else if (player == Warlock.warlock)
                            Warlock.warlock.killTimer = HudManagerStartPatch.warlockCurseButton.Timer = HudManagerStartPatch.warlockCurseButton.MaxTimer;
                        else if (player == Mini.mini && Mini.mini.Data.Role.IsImpostor)
                            Mini.mini.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown * (Mini.isGrownUp() ? 0.66f : 2f));
                        else if (player == Witch.witch)
                            Witch.witch.killTimer = HudManagerStartPatch.witchSpellButton.Timer = HudManagerStartPatch.witchSpellButton.MaxTimer;
                        else if (player == Ninja.ninja)
                            Ninja.ninja.killTimer = HudManagerStartPatch.ninjaButton.Timer = HudManagerStartPatch.ninjaButton.MaxTimer;
                        else if (player == Sheriff.sheriff)
                            Sheriff.sheriff.killTimer = HudManagerStartPatch.sheriffKillButton.Timer = HudManagerStartPatch.sheriffKillButton.MaxTimer;
                        else if (player == Vampire.vampire)
                            Vampire.vampire.killTimer = HudManagerStartPatch.vampireKillButton.Timer = HudManagerStartPatch.vampireKillButton.MaxTimer;
                        else if (player == Jackal.jackal)
                            Jackal.jackal.killTimer = HudManagerStartPatch.jackalKillButton.Timer = HudManagerStartPatch.jackalKillButton.MaxTimer;
                        else if (player == Sidekick.sidekick)
                            Sidekick.sidekick.killTimer = HudManagerStartPatch.sidekickKillButton.Timer = HudManagerStartPatch.sidekickKillButton.MaxTimer;
                        else if (player == Swooper.swooper)
                            Swooper.swooper.killTimer = HudManagerStartPatch.swooperKillButton.Timer = HudManagerStartPatch.swooperKillButton.MaxTimer;

                }

                public static bool isTeamJackal(PlayerControl player) {
                    if (Jackal.jackal == player) return true;
                    if (Sidekick.sidekick == player) return true;
                    return false;
                }
 
                public static bool isTeamJackalWithChat(PlayerControl player) {
                    if (!isTeamJackal(player)) return false;
                    return Jackal.hasChat;
                }

public static bool isPlayerLover(PlayerControl player) {
     return !(player == null) && (player == Lovers.lover1 || player == Lovers.lover2);
}

public static bool isTeamCultist(PlayerControl player)
	{
		return !(player == null) && (player == Cultist.cultist || player == Follower.follower);
	}

        public static PlayerControl getChatPartner(this PlayerControl player)
        {
        //     if (!Jackal.hasChat || Sidekick.sidekick == null) return Lovers.getPartner(player);

        //     if (isPlayerLover(player) && !isTeamJackal(player))
        //         return Lovers.getPartner(player);
        //     if (isTeamJackal(player) && !isPlayerLover(player)) {
        //       if (Jackal.jackal == player) return Sidekick.sidekick;
        //       if (Sidekick.sidekick == player) return Jackal.jackal;
        //     }
        //     if (isPlayerLover(player) && isTeamJackal(player)) {
        //       if (Jackal.jackal == player) {
        //         if (Jackal.chatTarget == 1) return Sidekick.sidekick;
        //         else return Lovers.getPartner(player);
        //       }

        //       if (Sidekick.sidekick == player) {
        //         if (Sidekick.chatTarget == 1) return Jackal.jackal;
        //         else return Lovers.getPartner(player);
        //       }
        //     } 
        //     return null;
        // }

        if (!player.isLover())
		{
			return player.getCultistPartner();
		}
		if (!player.isTeamCultist())
		{
			return player.getPartner();
		}
		if (player == Cultist.cultist)
		{
			if (Cultist.chatTarget)
			{
				return Follower.follower;
			}
			if (!Cultist.chatTarget)
			{
				return player.getPartner();
			}
		}
		if (player == Follower.follower)
		{
			if (Follower.chatTarget)
			{
				return Cultist.cultist;
			}
			if (!Follower.chatTarget)
			{
				return player.getPartner();
			}
		}
		return null;
	}

		public static bool isSaboActive() {
			return !(Helpers.getActiveSabo() == SabatageTypes.None);
		}

		public static bool isReactorActive() {
			return (Helpers.getActiveSabo() == SabatageTypes.Reactor);
		}

		public static bool isLightsActive() {
			return (Helpers.getActiveSabo() == SabatageTypes.Lights);
		}

		public static bool isO2Active() {
			return (Helpers.getActiveSabo() == SabatageTypes.O2);
		}

		public static bool isO2MaskActive() {
			return (Helpers.getActiveSabo() == SabatageTypes.OxyMask);
		}		

		public static bool isCommsActive() {
			return (Helpers.getActiveSabo() == SabatageTypes.Comms);
		}


		public static bool isCamoComms() {
			return (isCommsActive() && MapOptionsTor.camoComms);
		}


        public static void BlackmailShhh() {
            Helpers.showFlash(new Color32(49, 28, 69, byte.MinValue), 3f, false, 0.75f);
        }
        
        public static void Log(string e) {
            TheOtherRolesPlugin.Logger.LogMessage(e);
        }

        public static int getAvailableId() {
            var id = 0;
            while (true) {
                if (ShipStatus.Instance.AllVents.All(v => v.Id != id)) return id;
                id++;
            }
        }

		public static bool isActiveCamoComms() {
			return (isCamoComms() && Camouflager.camoComms);
		}

		public static bool wasActiveCamoComms() {
			return (!isCamoComms() && Camouflager.camoComms);
		}

		public static void camoReset() {
			Camouflager.resetCamouflage();
			if (Morphling.morphTimer > 0f && Morphling.morphling != null && Morphling.morphTarget != null) {
				PlayerControl target = Morphling.morphTarget;
				Morphling.morphling.setLook(target.Data.PlayerName, target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.PetId);
			}
		}

        public static bool canAlwaysBeGuessed(RoleId roleId) {
            bool guessable = false;
            if (roleId == RoleId.Cursed) guessable = true;
            return guessable;
        }

        public static void turnToCrewmate(PlayerControl player) {

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.TurnToCrewmate, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.turnToCrewmate(player.PlayerId);
            foreach (var player2 in PlayerControl.AllPlayerControls) {
                if (player2.Data.Role.IsImpostor && CachedPlayer.LocalPlayer.PlayerControl.Data.Role.IsImpostor) {
                    player.cosmetics.nameText.color = Palette.White;
                }
            }

        }
        
        public static void turnToCrewmate(List<PlayerControl> players, PlayerControl player) {
            foreach (PlayerControl p in players) {
                if (p == player) continue;
                turnToCrewmate(p);
            }
        }
        public static void turnToImpostorRPC(PlayerControl player) {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.TurnToImpostor, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.turnToImpostor(player.PlayerId);
		}

        public static void turnToImpostor(PlayerControl player) {
            player.Data.Role.TeamType = RoleTeamTypes.Impostor;
            RoleManager.Instance.SetRole(player, AmongUs.GameOptions.RoleTypes.Impostor);
            player.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);

            System.Console.WriteLine("PROOF I AM IMP VANILLA ROLE: "+player.Data.Role.IsImpostor);

            foreach (var player2 in PlayerControl.AllPlayerControls) {
                if (player2.Data.Role.IsImpostor && CachedPlayer.LocalPlayer.PlayerControl.Data.Role.IsImpostor) {
                    player.cosmetics.nameText.color = Palette.ImpostorRed;
                }
            }
        }
        
          public static bool ShowButtons {
            get {
                return !(MapBehaviour.Instance && MapBehaviour.Instance.IsOpen) &&
                      !MeetingHud.Instance &&
                      !ExileController.Instance;
            }
        }

        public static void showTargetNameOnButton(PlayerControl target, CustomButton button, string defaultText) {
            if (CustomOptionHolder.showButtonTarget.getBool()) { // Should the button show the target name option
                var text = "";
                if (Camouflager.camouflageTimer >= 0.1f || isActiveCamoComms()) text = defaultText; // set text to default if camo is on
                else if (Helpers.isLightsActive()) text = defaultText; // set to default if lights are out
                else if (Trickster.trickster != null && Trickster.lightsOutTimer > 0f) text = defaultText; // set to default if trickster ability is active
                else if (Morphling.morphling != null && Morphling.morphTarget != null && target == Morphling.morphling && Morphling.morphTimer > 0) text = Morphling.morphTarget.Data.PlayerName;  // set to morphed player
                else if (target == Swooper.swooper && Swooper.isInvisable) text = defaultText;
                else if (target == PhantomRole.phantomRole) text = defaultText;
                else if (target == null) text = defaultText; // Set text to defaultText if no target
                else text = target.Data.PlayerName; // Set text to playername
                showTargetNameOnButtonExplicit(null, button, text);
            }
        }
        


        public static void showTargetNameOnButtonExplicit(PlayerControl target, CustomButton button, string defaultText) {
            var text = defaultText;
            if (target == null) text = defaultText; // Set text to defaultText if no target
            else text = target.Data.PlayerName; // Set text to playername
            button.actionButton.OverrideText(text);
            button.showButtonText = true;
        }

        public static bool isInvisible(PlayerControl player)
	{
		if (Swooper.swooper != null && Swooper.swooper == player && Swooper.isInvisable)
		{
			return true;
		}
		return false;
	}


        public static Sprite loadSpriteFromResources(string path, float pixelsPerUnit) {
            try
            {
                if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
                Texture2D texture = loadTextureFromResources(path);
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
                return CachedSprites[path + pixelsPerUnit] = sprite;
            } catch {
                System.Console.WriteLine("Error loading sprite from path: " + path);
            }
            return null;
        }

        public static unsafe Texture2D loadTextureFromResources(string path) {
            try {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(path);
                var length = stream.Length;
                var byteTexture = new Il2CppStructArray<byte>(length);
                stream.Read(new Span<byte>(IntPtr.Add(byteTexture.Pointer, IntPtr.Size * 4).ToPointer(), (int) length));
                ImageConversion.LoadImage(texture, byteTexture, false);
                return texture;
            } catch {
                System.Console.WriteLine("Error loading texture from resources: " + path);
            }
            return null;
        }

        public static Texture2D loadTextureFromDisk(string path) {
            try {          
                if (File.Exists(path))     {
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                    var byteTexture = Il2CppSystem.IO.File.ReadAllBytes(path);
                    ImageConversion.LoadImage(texture, byteTexture, false);
                    return texture;
                }
            } catch {
                System.Console.WriteLine("Error loading texture from disk: " + path);
            }
            return null;
        }

        public static AudioClip loadAudioClipFromResources(string path, string clipName = "UNNAMED_TOR_AUDIO_CLIP") {
            // must be "raw (headerless) 2-channel signed 32 bit pcm (le)" (can e.g. use Audacity� to export)
            try {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(path);
                var byteAudio = new byte[stream.Length];
                _ = stream.Read(byteAudio, 0, (int)stream.Length);
                float[] samples = new float[byteAudio.Length / 4]; // 4 bytes per sample
                int offset;
                for (int i = 0; i < samples.Length; i++) {
                    offset = i * 4;
                    samples[i] = (float)BitConverter.ToInt32(byteAudio, offset) / Int32.MaxValue;
                }
                int channels = 2;
                int sampleRate = 48000;
                AudioClip audioClip = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
                audioClip.SetData(samples, 0);
                return audioClip;
            } catch {
                System.Console.WriteLine("Error loading AudioClip from resources: " + path);
            }
            return null;

            /* Usage example:
            AudioClip exampleClip = Helpers.loadAudioClipFromResources("TheOtherRoles.Resources.exampleClip.raw");
            if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(exampleClip, false, 0.8f);
            */
        }
        public static PlayerControl playerById(byte id)
        {
            foreach (PlayerControl player in CachedPlayer.AllPlayers)
                if (player.PlayerId == id)
                    return player;
            return null;
        }
        
        public static Dictionary<byte, PlayerControl> allPlayersById()
        {
            Dictionary<byte, PlayerControl> res = new Dictionary<byte, PlayerControl>();
            foreach (PlayerControl player in CachedPlayer.AllPlayers)
                res.Add(player.PlayerId, player);
            return res;
        }

        public static void handleVampireBiteOnBodyReport() {
            // Murder the bitten player and reset bitten (regardless whether the kill was successful or not)
            Helpers.checkMurderAttemptAndKill(Vampire.vampire, Vampire.bitten, true, false);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VampireSetBitten, Hazel.SendOption.Reliable, -1);
            writer.Write(byte.MaxValue);
            writer.Write(byte.MaxValue);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.vampireSetBitten(byte.MaxValue, byte.MaxValue);
        }
        
        public static void handleBomberExplodeOnBodyReport() {
            // Murder the bitten player and reset bitten (regardless whether the kill was successful or not)
            Helpers.checkMuderAttemptAndKill(Bomber.bomber, Bomber.hasBomb, true, false);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.GiveBomb, Hazel.SendOption.Reliable, -1);
            writer.Write(byte.MaxValue);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.giveBomb(byte.MaxValue);
        }

        public static void refreshRoleDescription(PlayerControl player) {
            List<RoleInfo> infos = RoleInfo.getRoleInfoForPlayer(player); 
            List<string> taskTexts = new(infos.Count); 

            foreach (var roleInfo in infos)
            {
                taskTexts.Add(getRoleString(roleInfo));
            }
            
            var toRemove = new List<PlayerTask>();
            foreach (PlayerTask t in player.myTasks.GetFastEnumerator()) 
            {
                var textTask = t.TryCast<ImportantTextTask>();
                if (textTask == null) continue;
                
                var currentText = textTask.Text;
                
                if (taskTexts.Contains(currentText)) taskTexts.Remove(currentText); // TextTask for this RoleInfo does not have to be added, as it already exists
                else toRemove.Add(t); // TextTask does not have a corresponding RoleInfo and will hence be deleted
            }   

            foreach (PlayerTask t in toRemove) {
                t.OnRemove();
                player.myTasks.Remove(t);
                UnityEngine.Object.Destroy(t.gameObject);
            }

            // Add TextTask for remaining RoleInfos
            foreach (string title in taskTexts) {
                var task = new GameObject("RoleTask").AddComponent<ImportantTextTask>();
                task.transform.SetParent(player.transform, false);
                task.Text = title;
                player.myTasks.Insert(0, task);
            }
        }

        internal static string getRoleString(RoleInfo roleInfo)
        {
            if (roleInfo.name == "Jackal") 
            {
                var getSidekickText = Jackal.canCreateSidekick ? " and recruit a Sidekick" : "";
                return cs(roleInfo.color, $"{roleInfo.name}: Kill everyone{getSidekickText}");  
            }

            if (roleInfo.name == "Invert") 
            {
                return cs(roleInfo.color, $"{roleInfo.name}: {roleInfo.shortDescription} ({Invert.meetings})");
            }
            
            return cs(roleInfo.color, $"{roleInfo.name}: {roleInfo.shortDescription}");
        }
        
        public static bool isLighterColor(int colorId) {
            return CustomColors.lighterColors.Contains(colorId);
        }

        public static bool isCustomServer() {
            if (FastDestroyableSingleton<ServerManager>.Instance == null) return false;
            StringNames n = FastDestroyableSingleton<ServerManager>.Instance.CurrentRegion.TranslateName;
            return n != StringNames.ServerNA && n != StringNames.ServerEU && n != StringNames.ServerAS;
        }

        public static bool isDead(this PlayerControl player)
        {
            return player == null || player?.Data?.IsDead == true || player?.Data?.Disconnected == true;
        }

		public static void setInvisable(PlayerControl  player) {
			MessageWriter invisibleWriter = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetInvisibleGen, Hazel.SendOption.Reliable, -1);
			invisibleWriter.Write(player.PlayerId);
			invisibleWriter.Write(byte.MinValue);
			AmongUsClient.Instance.FinishRpcImmediately(invisibleWriter);
			RPCProcedure.setInvisibleGen(player.PlayerId, byte.MinValue);
		}

        public static bool isAlive(this PlayerControl player)
        {
            return !isDead(player);
        }


        public static bool hasFakeTasks(this PlayerControl player) {
            return (player == Prosecutor.prosecutor || player == Werewolf.werewolf || player == Jester.jester || player == Amnisiac.amnisiac || player == Swooper.swooper|| player == Jackal.jackal || player == Sidekick.sidekick || player == Arsonist.arsonist || player == Vulture.vulture || Jackal.formerJackals.Any(x => x == player));
        }

        public static bool canBeErased(this PlayerControl player) {
            return (player != Jackal.jackal && player != Sidekick.sidekick && !Jackal.formerJackals.Any(x => x == player) && player != Swooper.swooper && player != Werewolf.werewolf);
        }

        public static void clearAllTasks(this PlayerControl player) {
            if (player == null) return;
            foreach (var playerTask in player.myTasks.GetFastEnumerator())
            {
                playerTask.OnRemove();
                UnityEngine.Object.Destroy(playerTask.gameObject);
            }
            player.myTasks.Clear();
            
            if (player.Data != null && player.Data.Tasks != null)
                player.Data.Tasks.Clear();
        }

        public static void setSemiTransparent(this PoolablePlayer player, bool value) {
            float alpha = value ? 0.25f : 1f;
            foreach (SpriteRenderer r in player.gameObject.GetComponentsInChildren<SpriteRenderer>())
                r.color = new Color(r.color.r, r.color.g, r.color.b, alpha);
            player.cosmetics.nameText.color = new Color(player.cosmetics.nameText.color.r, player.cosmetics.nameText.color.g, player.cosmetics.nameText.color.b, alpha);
        }

        public static string GetString(this TranslationController t, StringNames key, params Il2CppSystem.Object[] parts) {
            return t.GetString(key, parts);
        }

        public static string cs(Color c, string s) {
            return string.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>{4}</color>", ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a), s);
        }
 
        private static byte ToByte(float f) {
            f = Mathf.Clamp01(f);
            return (byte)(f * 255);
        }

        public static KeyValuePair<byte, int> MaxPair(this Dictionary<byte, int> self, out bool tie) {
            tie = true;
            KeyValuePair<byte, int> result = new KeyValuePair<byte, int>(byte.MaxValue, int.MinValue);
            foreach (KeyValuePair<byte, int> keyValuePair in self)
            {
                if (keyValuePair.Value > result.Value)
                {
                    result = keyValuePair;
                    tie = false;
                }
                else if (keyValuePair.Value == result.Value)
                {
                    tie = true;
                }
            }
            return result;
        }

        public static bool hidePlayerName(PlayerControl source, PlayerControl target) {
            if (Camouflager.camouflageTimer > 0f) return true; // No names are visible
			 if (isActiveCamoComms()) return true;
             if (Ninja.isInvisble && Ninja.ninja == target) return true; 
             if (Swooper.isInvisable && Swooper.swooper == target) return true;
      //       if (PhantomRole.phantomRole == target) return true;  
             if (MapOptionsTor.hideOutOfSightNametags && gameStarted && ShipStatus.Instance != null && source.transform != null && target.transform != null && !source.Data.IsDead)
		{
			float num = (isLightsActive() ? 2f : 1.25f);
			float num2 = Vector3.Distance(source.transform.position, target.transform.position);
			if (PhysicsHelpers.AnythingBetween(source.GetTruePosition(), target.GetTruePosition(), Constants.ShadowMask, useTriggers: false))
			{
				return true;
			}
		}
            if (!MapOptionsTor.hidePlayerNames)
		{
			return false;
		}
		if (source == null || target == null)
		{
			return true;
		}
		if (source.Data.IsDead)
		{
			return false;
		}
		if (source == target)
		{
			return false;
		}
             if (source.Data.Role.IsImpostor && (target.Data.Role.IsImpostor || target == Spy.spy || target == Sidekick.sidekick && Sidekick.wasTeamRed || target == Jackal.jackal && Jackal.wasTeamRed)) return false; // Members of team Impostors see the names of Impostors/Spies
             if ((source == Lovers.lover1 || source == Lovers.lover2) && (target == Lovers.lover1 || target == Lovers.lover2)) return false; // Members of team Lovers see the names of each other
             if ((source == Jackal.jackal || source == Sidekick.sidekick) && (target == Jackal.jackal || target == Sidekick.sidekick || target == Jackal.fakeSidekick)) return false; // Members of team Jackal see the names of each other
			 if ((source == Prosecutor.prosecutor) && (target == Prosecutor.target)) return false; // Prosecutor can always see target name
             if (Deputy.knowsSheriff && (source == Sheriff.sheriff || source == Deputy.deputy) && (target == Sheriff.sheriff || target == Deputy.deputy)) return false; // Sheriff & Deputy see the names of each other
            return true;
        }

        public static void setDefaultLook(this PlayerControl target) {
            target.setLook(target.Data.PlayerName, target.Data.DefaultOutfit.ColorId, target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.SkinId, target.Data.DefaultOutfit.PetId);
        }

        public static void setLook(this PlayerControl target, String playerName, int colorId, string hatId, string visorId, string skinId, string petId) {
            target.RawSetColor(colorId);
            target.RawSetVisor(visorId,colorId);
            target.RawSetHat(hatId, colorId);
            target.RawSetName(hidePlayerName(CachedPlayer.LocalPlayer.PlayerControl, target) ? "" : playerName);

            SkinViewData nextSkin = FastDestroyableSingleton<HatManager>.Instance.GetSkinById(skinId).viewData.viewData;
            PlayerPhysics playerPhysics = target.MyPhysics;
            AnimationClip clip = null;
            var spriteAnim = playerPhysics.myPlayer.cosmetics.skin.animator;
            var currentPhysicsAnim = playerPhysics.Animations.Animator.GetCurrentAnimation();


            if (currentPhysicsAnim == playerPhysics.Animations.group.RunAnim) clip = nextSkin.RunAnim;
            else if (currentPhysicsAnim == playerPhysics.Animations.group.SpawnAnim) clip = nextSkin.SpawnAnim;
            else if (currentPhysicsAnim == playerPhysics.Animations.group.EnterVentAnim) clip = nextSkin.EnterVentAnim;
            else if (currentPhysicsAnim == playerPhysics.Animations.group.ExitVentAnim) clip = nextSkin.ExitVentAnim;
            else if (currentPhysicsAnim == playerPhysics.Animations.group.IdleAnim) clip = nextSkin.IdleAnim;
            else clip = nextSkin.IdleAnim;
            float progress = playerPhysics.Animations.Animator.m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            playerPhysics.myPlayer.cosmetics.skin.skin = nextSkin;
            playerPhysics.myPlayer.cosmetics.skin.UpdateMaterial();
            spriteAnim.Play(clip, 1f);
            spriteAnim.m_animator.Play("a", 0, progress % 1);
            spriteAnim.m_animator.Update(0f);

            if (target.cosmetics.currentPet) UnityEngine.Object.Destroy(target.cosmetics.currentPet.gameObject);
            target.cosmetics.currentPet = UnityEngine.Object.Instantiate<PetBehaviour>(FastDestroyableSingleton<HatManager>.Instance.GetPetById(petId).viewData.viewData);
            target.cosmetics.currentPet.transform.position = target.transform.position;
            target.cosmetics.currentPet.Source = target;
            target.cosmetics.currentPet.Visible = target.Visible;
            target.SetPlayerMaterialColors(target.cosmetics.currentPet.rend);

            Chameleon.update();  // so that morphling and camo wont make the chameleons visible
        }

        public static void showFlash(Color color, float duration=1f, bool fade = true, float opacity = 100f) {
            if (FastDestroyableSingleton<HudManager>.Instance == null || FastDestroyableSingleton<HudManager>.Instance.FullScreen == null) return;
            FastDestroyableSingleton<HudManager>.Instance.FullScreen.gameObject.SetActive(true);
            FastDestroyableSingleton<HudManager>.Instance.FullScreen.enabled = true;
            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(duration, new Action<float>((p) => {
                var renderer = FastDestroyableSingleton<HudManager>.Instance.FullScreen;
                if (!fade) {
                    if (renderer != null)
                        renderer.color = new Color(color.r, color.g, color.b, opacity);
                } else {
                    if (p < 0.5) {
                        if (renderer != null)
                            renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(p * 2 * 0.75f));
                    } else {
                        if (renderer != null)
                            renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01((1 - p) * 2 * 0.75f));
                    }
                }
                if (p == 1f && renderer != null) renderer.enabled = false;
            })));
        }

        public static void checkWatchFlash(PlayerControl target) {
			if (CachedPlayer.LocalPlayer.PlayerControl == PrivateInvestigator.watching) {
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.PrivateInvestigatorWatchFlash, Hazel.SendOption.Reliable, -1);
                writer.Write(target.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.privateInvestigatorWatchFlash(target.PlayerId);
			}
		}

        public static bool roleCanUseVents(this PlayerControl player) {
            bool roleCouldUse = false;
            if (Engineer.engineer != null && Engineer.engineer == player)
                roleCouldUse = true;
            if (Swooper.swooper != null && Swooper.swooper == player)
                roleCouldUse = true;
            else if (Werewolf.canUseVents && Werewolf.werewolf != null && Werewolf.werewolf == player) //testing
                roleCouldUse = true;
            else if (Jackal.canUseVents && Jackal.jackal != null && Jackal.jackal == player)
                roleCouldUse = true;
            else if (Sidekick.canUseVents && Sidekick.sidekick != null && Sidekick.sidekick == player)
                roleCouldUse = true;
            else if (Spy.canEnterVents && Spy.spy != null && Spy.spy == player)
                roleCouldUse = true;
            else if (Vulture.canUseVents && Vulture.vulture != null && Vulture.vulture == player)
                roleCouldUse = true;
            else if (Undertaker.deadBodyDraged != null && !Undertaker.canDragAndVent && Undertaker.undertaker== player)
                roleCouldUse = false;
            else if (Thief.canUseVents &&  Thief.thief != null && Thief.thief == player)
                roleCouldUse = true;
            else if (player.Data?.Role != null && player.Data.Role.CanVent)  {
                if (Janitor.janitor != null && Janitor.janitor == CachedPlayer.LocalPlayer.PlayerControl)
                    roleCouldUse = false;
                else if (Mafioso.mafioso != null && Mafioso.mafioso == CachedPlayer.LocalPlayer.PlayerControl && Godfather.godfather != null && !Godfather.godfather.Data.IsDead)
                    roleCouldUse = false;
                else
                    roleCouldUse = true;
            } else if (Jester.jester != null && Jester.jester == player && Jester.canVent)
                roleCouldUse = true;

            if (Tunneler.tunneler != null && Tunneler.tunneler == player) {
                var (playerCompleted, playerTotal) = TasksHandler.taskInfo(Tunneler.tunneler.Data);
                int numberOfTasks = playerTotal - playerCompleted;
                if (numberOfTasks == 0) roleCouldUse = true;
            }


            return roleCouldUse;
        }

        public static MurderAttemptResult checkMuderAttempt(PlayerControl killer, PlayerControl target, bool blockRewind = false) {
            var targetRole = RoleInfo.getRoleInfoForPlayer(target, false).FirstOrDefault();

            // Modified vanilla checks
            if (AmongUsClient.Instance.IsGameOver) return MurderAttemptResult.SuppressKill;
            if (killer == null || killer.Data == null || killer.Data.IsDead || killer.Data.Disconnected) return MurderAttemptResult.SuppressKill; // Allow non Impostor kills compared to vanilla code
            if (target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected) return MurderAttemptResult.SuppressKill; // Allow killing players in vents compared to vanilla code

            // Handle first kill attempt
            if (MapOptionsTor.shieldFirstKill && MapOptionsTor.firstKillPlayer == target) return MurderAttemptResult.SuppressKill;

            // Handle blank shot
            if (Pursuer.blankedList.Any(x => x.PlayerId == killer.PlayerId)) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetBlanked, Hazel.SendOption.Reliable, -1);
                writer.Write(killer.PlayerId);
                writer.Write((byte)0);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.setBlanked(killer.PlayerId, 0);

                return MurderAttemptResult.BlankKill;
            }


            // Kill the killer if the Veteren is on alert
            else if (Veteren.veteren != null && target == Veteren.veteren && Veteren.alertActive) {
              if (Medic.shielded != null && Medic.shielded == target) {
                   MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
                   writer.Write(target.PlayerId);
                   AmongUsClient.Instance.FinishRpcImmediately(writer);
                   RPCProcedure.shieldedMurderAttempt(killer.PlayerId);
              }
              return MurderAttemptResult.ReverseKill;
            }            // Kill the killer if the Veteren is on alert
            
            // Kill the Body Guard and the killer if the target is guarded
            else if (BodyGuard.bodyguard != null && target == BodyGuard.guarded && isAlive(BodyGuard.bodyguard)) {
              if (Medic.shielded != null && Medic.shielded == target) {
                   MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
                   writer.Write(target.PlayerId);
                   AmongUsClient.Instance.FinishRpcImmediately(writer);
                   RPCProcedure.shieldedMurderAttempt(killer.PlayerId);
              }
              return MurderAttemptResult.BodyGuardKill;
            }


            // Block impostor shielded kill
            if (!Medic.unbreakableShield && Medic.shielded != null && Medic.shielded == target) {
                MessageWriter write = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetBlanked, Hazel.SendOption.Reliable, -1);
                write.Write(killer.PlayerId);
                write.Write((byte)0);
                AmongUsClient.Instance.FinishRpcImmediately(write);
                RPCProcedure.setBlanked(killer.PlayerId, 0);
                Medic.shielded = null;

                return MurderAttemptResult.BlankKill;
            }
            else if (Medic.shielded != null && Medic.shielded == target) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
                writer.Write(killer.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.shieldedMurderAttempt(killer.PlayerId);
                SoundEffectsManager.play("fail");
                return MurderAttemptResult.SuppressKill;
            }

            // Block impostor not fully grown mini kill
            else if (Mini.mini != null && target == Mini.mini && !Mini.isGrownUp()) {
                return MurderAttemptResult.SuppressKill;
            }



            // Block Time Master with time shield kill
            else if (TimeMaster.shieldActive && TimeMaster.timeMaster != null && TimeMaster.timeMaster == target) {
                if (!blockRewind) { // Only rewind the attempt was not called because a meeting startet 
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.TimeMasterRewindTime, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.timeMasterRewindTime();
                }
                MessageWriter write = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetBlanked, Hazel.SendOption.Reliable, -1);
                write.Write(killer.PlayerId);
                write.Write((byte)0);
                AmongUsClient.Instance.FinishRpcImmediately(write);
                RPCProcedure.setBlanked(killer.PlayerId, 0);

                return MurderAttemptResult.BlankKill;
            }
			
			else if (Cursed.cursed != null && Cursed.cursed == target && killer.Data.Role.IsImpostor) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.SetBlanked, Hazel.SendOption.Reliable, -1);
                writer.Write(killer.PlayerId);
                writer.Write((byte)0);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.setBlanked(killer.PlayerId, 0);

				turnToImpostorRPC(target);

				return MurderAttemptResult.BlankKill;
			}

            else if (Cultist.cultist != null && !target.Data.Role.IsImpostor && killer == Cultist.cultist) {
                MessageWriter writer3 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ShowCultistFlash, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer3);
                RPCProcedure.showCultistFlash();
			}

            else if (Follower.follower != null && !target.Data.Role.IsImpostor && killer == Follower.follower) {
                MessageWriter writer3 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ShowFollowerFlash, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer3);
                RPCProcedure.showFollowerFlash();
			}
				
				
			

            // Thief if hit crew only kill if setting says so, but also kill the thief.
            else if (killer == Thief.thief && !target.Data.Role.IsImpostor && !new List<RoleInfo> {RoleInfo.jackal, Thief.canKillSheriff ? RoleInfo.sheriff : null, RoleInfo.sidekick, RoleInfo.swooper, RoleInfo.werewolf, RoleInfo.bodyguard, RoleInfo.veteren }.Contains(targetRole)) {
                Thief.suicideFlag = true;
                return MurderAttemptResult.SuppressKill;
            }

            // Block hunted with time shield kill
            else if (Hunted.timeshieldActive.Contains(target.PlayerId)) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)CustomRPC.HuntedRewindTime, Hazel.SendOption.Reliable, -1);
                writer.Write(target.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.huntedRewindTime(target.PlayerId);

                return MurderAttemptResult.SuppressKill;
            }

            return MurderAttemptResult.PerformKill;
        }

		public static MurderAttemptResult checkMuderAttemptAndKill(PlayerControl killer, PlayerControl target, bool isMeetingStart = false, bool showAnimation = true)  {
			return checkMurderAttemptAndKill(killer, target, isMeetingStart, showAnimation);
		}
	
        public static MurderAttemptResult checkMurderAttemptAndKill(PlayerControl killer, PlayerControl target, bool isMeetingStart = false, bool showAnimation = true)  {
            // The local player checks for the validity of the kill and performs it afterwards (different to vanilla, where the host performs all the checks)
            // The kill attempt will be shared using a custom RPC, hence combining modded and unmodded versions is impossible

            MurderAttemptResult murder = checkMuderAttempt(killer, target, isMeetingStart);

            if (murder == MurderAttemptResult.PerformKill) {
                if (killer == Poucher.poucher) Poucher.killed.Add(target);
				
				if (Mimic.mimic != null && killer == Mimic.mimic && !Mimic.hasMimic) {
					MessageWriter writerMimic = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.MimicMimicRole, Hazel.SendOption.Reliable, -1);
					writerMimic.Write(target.PlayerId);
					AmongUsClient.Instance.FinishRpcImmediately(writerMimic);
					RPCProcedure.mimicMimicRole(target.PlayerId);
				}
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
                writer.Write(killer.PlayerId);
                writer.Write(target.PlayerId);
                writer.Write(showAnimation ? Byte.MaxValue : 0);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.uncheckedMurderPlayer(killer.PlayerId, target.PlayerId, showAnimation ? Byte.MaxValue : (byte)0);
            }
            
            if (murder == MurderAttemptResult.BodyGuardKill) {
                // Kill the Killer
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
                writer.Write(killer.PlayerId);
                writer.Write(killer.PlayerId);
                writer.Write(showAnimation ? Byte.MaxValue : 0);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.uncheckedMurderPlayer(BodyGuard.bodyguard.PlayerId, killer.PlayerId, (byte)0);
                
                // Kill the BodyGuard
                MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.UncheckedMurderPlayer, Hazel.SendOption.Reliable, -1);
                writer2.Write(BodyGuard.bodyguard.PlayerId);
                writer2.Write(BodyGuard.bodyguard.PlayerId);
                writer2.Write(showAnimation ? Byte.MaxValue : 0);
                AmongUsClient.Instance.FinishRpcImmediately(writer2);
                RPCProcedure.uncheckedMurderPlayer(BodyGuard.bodyguard.PlayerId, BodyGuard.bodyguard.PlayerId, (byte)0);


                MessageWriter writer3 = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.ShowBodyGuardFlash, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer3);
                RPCProcedure.showBodyGuardFlash();
            }

            if (murder == MurderAttemptResult.ReverseKill) {
                checkMuderAttemptAndKill(target, killer, isMeetingStart);
            }

            return murder;            
        }
    

	public static bool checkAndDoVetKill(PlayerControl target) {
	  bool shouldVetKill = (Veteren.veteren == target && Veteren.alertActive);
	  if (shouldVetKill) {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VeterenKill, Hazel.SendOption.Reliable, -1);
            writer.Write(CachedPlayer.LocalPlayer.PlayerControl.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.veterenKill(CachedPlayer.LocalPlayer.PlayerControl.PlayerId);
	  }
	  return shouldVetKill;
	}

        public static void shareGameVersion() {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(CachedPlayer.LocalPlayer.PlayerControl.NetId, (byte)CustomRPC.VersionHandshake, Hazel.SendOption.Reliable, -1);
            writer.Write((byte)TheOtherRolesPlugin.Version.Major);
            writer.Write((byte)TheOtherRolesPlugin.Version.Minor);
            writer.Write((byte)TheOtherRolesPlugin.Version.Build);
            writer.Write(AmongUsClient.Instance.AmHost ? Patches.GameStartManagerPatch.timer : -1f);
            writer.WritePacked(AmongUsClient.Instance.ClientId);
            writer.Write((byte)(TheOtherRolesPlugin.Version.Revision < 0 ? 0xFF : TheOtherRolesPlugin.Version.Revision));
            writer.Write(Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToByteArray());
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.versionHandshake(TheOtherRolesPlugin.Version.Major, TheOtherRolesPlugin.Version.Minor, TheOtherRolesPlugin.Version.Build, TheOtherRolesPlugin.Version.Revision, Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId, AmongUsClient.Instance.ClientId);
        }

        public static List<PlayerControl> getKillerTeamMembers(PlayerControl player) {
            List<PlayerControl> team = new List<PlayerControl>();
            foreach(PlayerControl p in CachedPlayer.AllPlayers) {
                if (player.Data.Role.IsImpostor && p.Data.Role.IsImpostor && player.PlayerId != p.PlayerId && team.All(x => x.PlayerId != p.PlayerId)) team.Add(p);
                else if (player == Jackal.jackal && p == Sidekick.sidekick) team.Add(p); 
                else if (player == Sidekick.sidekick && p == Jackal.jackal) team.Add(p);
            }
            
            return team;
        }




        public static bool zoomOutStatus = false;
        public static void toggleZoom(bool reset=false) {
            float orthographicSize = reset || zoomOutStatus ? 3f : 12f;

            zoomOutStatus = !zoomOutStatus && !reset;
            Camera.main.orthographicSize = orthographicSize;
            foreach (var cam in Camera.allCameras) {
                if (cam != null && cam.gameObject.name == "UI Camera") cam.orthographicSize = orthographicSize;  // The UI is scaled too, else we cant click the buttons. Downside: map is super small.
            }

            HudManagerStartPatch.zoomOutButton.Sprite = zoomOutStatus ? Helpers.loadSpriteFromResources("TheOtherRoles.Resources.PlusButton.png", 75f) : Helpers.loadSpriteFromResources("TheOtherRoles.Resources.MinusButton.png", 150f);
            HudManagerStartPatch.zoomOutButton.PositionOffset = zoomOutStatus ? new Vector3(0f, 3f, 0) : new Vector3(0.4f, 2.8f, 0);
            ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height); // This will move button positions to the correct position.
        }

        public static async Task checkBeta() {
            if (TheOtherRolesPlugin.betaDays > 0) {
                TheOtherRolesPlugin.Logger.LogMessage($"Beta check");
                var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);  // This may show as an error, but it is not, compilation will work!
                DateTime? now;
                // Get time from the internet, so no-one can cheat it (so easily).
                try {
                    var client = new System.Net.Http.HttpClient();
                    using var response = await client.GetAsync("http://www.google.com/");
                    if (response.IsSuccessStatusCode)
                        now = response.Headers.Date?.UtcDateTime;
                    else {
                        TheOtherRolesPlugin.Logger.LogMessage($"Could not get time from server: {response.StatusCode}");
                        now = DateTime.UtcNow; //In case something goes wrong. 
                    }
                } catch (System.Net.Http.HttpRequestException) {
                    now = DateTime.UtcNow;
                }
                 if ((now - compileTime)?.TotalDays > TheOtherRolesPlugin.betaDays) {
                    TheOtherRolesPlugin.Logger.LogMessage($"Beta expired!");
                    BepInExUpdater.MessageBoxTimeout(BepInExUpdater.GetForegroundWindow(), "BETA is expired. You cannot play this version anymore.", "The Other Roles Beta", 0,0, 10000);
                    Application.Quit();

                } else TheOtherRolesPlugin.Logger.LogMessage($"Beta will remain runnable for {TheOtherRolesPlugin.betaDays - (now - compileTime)?.TotalDays} days!");
            }
        }

        public static bool hasImpVision(GameData.PlayerInfo player) {
            return player.Role.IsImpostor
                || ((Jackal.jackal != null && Jackal.jackal.PlayerId == player.PlayerId || Jackal.formerJackals.Any(x => x.PlayerId == player.PlayerId)) && Jackal.hasImpostorVision)
                || (Sidekick.sidekick != null && Sidekick.sidekick.PlayerId == player.PlayerId && Sidekick.hasImpostorVision)
                || (Spy.spy != null && Spy.spy.PlayerId == player.PlayerId && Spy.hasImpostorVision)
                || (Jester.jester != null && Jester.jester.PlayerId == player.PlayerId && Jester.hasImpostorVision)
                || (Thief.thief != null && Thief.thief.PlayerId == player.PlayerId && Thief.hasImpostorVision)
                || (Werewolf.werewolf != null && Werewolf.werewolf.PlayerId == player.PlayerId && Werewolf.hasImpostorVision)
                || (Swooper.swooper != null && Swooper.swooper.PlayerId == player.PlayerId && Swooper.hasImpVision);
        }
        
        public static object TryCast(this Il2CppObjectBase self, Type type)
        {
            return AccessTools.Method(self.GetType(), nameof(Il2CppObjectBase.TryCast)).MakeGenericMethod(type).Invoke(self, Array.Empty<object>());
        }
    }
}
