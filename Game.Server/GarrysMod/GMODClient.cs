#if GMOD_DLL
global using static Game.Server.GarrysMod.GMODClient;

using Game.Server;
using Game.Server.GarrysMod;
using Game.Shared;

using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server.GarrysMod;

public static class GMODClient
{
	public static void FinishClientPutInServer(GMOD_Player player) {
		player.InitialSpawn();
		player.Spawn();

		Span<char> sName = stackalloc char[128];
		strcpy(sName, player.GetPlayerName());

		// First parse the name and remove any %'s
		for (Span<char> pAmpersand = sName; !pAmpersand.IsEmpty && pAmpersand[0] != 0; pAmpersand = pAmpersand[1..]) {
			// Replace it with a space
			if (pAmpersand[0] == '%')
				pAmpersand[0] = ' ';
		}

		// notify other clients of player joining the game
		Util.ClientPrintAll(HudPrint.Notify, "#Game_connected", sName[0] != 0 ? sName : "<unconnected>");

		// if (HL2MPRules().IsTeamplay() == true)
		// 	ClientPrint(player, HudPrint.Talk, $"You are on team {player.GetTeam().GetName()}\n");
	}

	public static void ClientPutInServer(Edict edict, ReadOnlySpan<char> playername) {
		// Allocate a CBaseTFPlayer for pev, and call spawn
		GMOD_Player player = GMOD_Player.CreatePlayer("player", edict);
		player.SetPlayerName(playername);
	}

	public static void ClientActive(Edict edict, bool loadGame) {
		// Can't load games in CS!
		Assert(!loadGame);

		GMOD_Player? player = ToGMODPlayer(BaseEntity.Instance(edict));
		FinishClientPutInServer(player!);
	}

	public static ReadOnlySpan<char> GetGameDescription() {
		if (g_pGameRules != null) // this function may be called before the world has spawned, and the game rules initialized
			return g_pGameRules.GetGameDescription();
		else
			return "Garry's Mod";
	}

	//-----------------------------------------------------------------------------
	// Purpose: Given a player and optional name returns the entity of that 
	//			classname that the player is nearest facing
	//			
	// Input  :
	// Output :
	//-----------------------------------------------------------------------------
	public static BaseEntity? FindEntity(Edict edict, Span<char> classname) {
		// If no name was given set bits based on the picked
		// if (FStrEq(classname, ""))
			// todo return (FindPickerEntityClass((CBasePlayer)GetContainingEntity(edict)), classname));

		return null;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Precache game-specific models & sounds
	//-----------------------------------------------------------------------------
	public static void ClientGamePrecache() {
		// todo BaseEntity.PrecacheModel("models/player.mdl");
		// todo BaseEntity.PrecacheModel("models/gibs/agibs.mdl");
		// todo BaseEntity.PrecacheModel("models/weapons/v_hands.mdl");
		// todo BaseEntity.PrecacheScriptSound("HUDQuickInfo.LowAmmo");
		// todo BaseEntity.PrecacheScriptSound("HUDQuickInfo.LowHealth");
		// todo BaseEntity.PrecacheScriptSound("FX_AntlionImpact.ShellImpact");
		// todo BaseEntity.PrecacheScriptSound("Missile.ShotDown");
		// todo BaseEntity.PrecacheScriptSound("Bullets.DefaultNearmiss");
		// todo BaseEntity.PrecacheScriptSound("Bullets.GunshipNearmiss");
		// todo BaseEntity.PrecacheScriptSound("Bullets.StriderNearmiss");
		// todo BaseEntity.PrecacheScriptSound("Geiger.BeepHigh");
		// todo BaseEntity.PrecacheScriptSound("Geiger.BeepLow");
	}


	// called by ClientKill and DeadThink
	public static void respawn(BaseEntity edict, bool fCopyCorpse) {
		GMOD_Player? player = ToGMODPlayer(edict);

		if (player != null) {
			if (gpGlobals.CurTime > player.GetDeathTime() + DEATH_ANIMATION_TIME) {
				// respawn player
				player.Spawn();
			}
			else {
				player.SetNextThink(gpGlobals.CurTime + 0.1);
			}
		}
	}

	public static void GameStartFrame() {
		if (g_fGameOver)
			return;

	}

	//=========================================================
	// instantiate the proper game rules object
	//=========================================================
	public static void InstallGameRules() {
		// vanilla deathmatch
		GameRulesRegister.CreateGameRulesObject("CGMODRules");
	}
}
#endif
