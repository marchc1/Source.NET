using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Engine;

namespace Game.Server;

[EngineComponent]
public class GameServerClientMethods(Host Host)
{
	public const TimeUnit_t TALK_INTERVAL = 0.66; // min time between say commands from a client

	[ConCommand(helpText: "Display player message")]
	void say(in TokenizedCommand args, CommandSource source, int clientslot) {
		BasePlayer? player = ToBasePlayer(Util.GetCommandClient());
		if (player != null) {
			if ((player.LastTimePlayerTalked() + TALK_INTERVAL) < gpGlobals.CurTime) {
				Host.Say(player.Edict(), args, false);
				player.NotePlayerTalked();
			}
		}
		// This will result in a "console" say.  Ignore anything from
		// an index greater than 0 when we don't have a player pointer, 
		// as would be the case when a client that's connecting generates 
		// text via a script.  This can be exploited to flood everyone off.
		else if (Util.GetCommandClientIndex() == 0) {
			Host.Say(null, args, false);
		}
	}


	public static ReadOnlySpan<char> CheckChatText(BasePlayer? player, ReadOnlySpan<char> text) => text[..Math.Min(text.Length, 127)];
}

public static class HostExts
{
	public static void Say(this Host host, Edict? edict, in TokenizedCommand args, bool teamOnly) {
		BasePlayer? client;
		nint j;
		scoped ReadOnlySpan<char> p;
		ReadOnlySpan<char> text = stackalloc char[256];
		Span<char> temp = stackalloc char[256];
		ReadOnlySpan<char> say = "say";
		ReadOnlySpan<char> sayTeam = "say_team";
		ReadOnlySpan<char> cmd = args[0];
		bool senderDead = false;

		// We can get a raw string now, without the "say " prepended
		if (args.ArgC() == 0)
			return;

		if (stricmp(cmd, say) == 0 || stricmp(cmd, sayTeam) == 0) {
			if (args.ArgC() >= 2)
				p = args.ArgS();
			else // say with a blank message, nothing to do
				return;
		}
		else  // Raw text, need to prepend argv[0]
		{
			if (args.ArgC() >= 2)
				sprintf(temp, "%s %s").S(cmd).S(args.ArgS());
			else
				// Just a one word command, use the first word...sigh
				sprintf(temp, "%s").S(cmd);

			p = temp;
		}

		BasePlayer? player = null;
		if (edict != null) {
			player = ((BasePlayer?)BaseEntity.Instance(edict));
			Assert(player != null);

			// make sure the text has valid content
			p = GameServerClientMethods.CheckChatText(player, p);
		}

		if (p.IsEmpty)
			return;

		if (edict != null && player != null) {
			if (!player.CanSpeak())
				return;

			Assert(player.GetPlayerName()[0] != '\0');
			senderDead = (player.LifeState != (int)LifeState.Alive);
		}
		else
			senderDead = false;

		ReadOnlySpan<char> pszFormat = null;
		ReadOnlySpan<char> pszPrefix = null;
		ReadOnlySpan<char> pszLocation = null;
		if (g_pGameRules != null) {
			pszFormat = g_pGameRules.GetChatFormat(teamOnly, player);
			pszPrefix = g_pGameRules.GetChatPrefix(teamOnly, player);
			pszLocation = g_pGameRules.GetChatLocation(teamOnly, player);
		}

		ReadOnlySpan<char> pszPlayerName = player != null ? player.GetPlayerName() : "Console";

		if (!pszPrefix.IsStringEmpty) {
			if (!pszLocation.IsStringEmpty)
				text = $"{pszPrefix} {pszPlayerName} @ {pszLocation}: ";
			else
				text = $"{pszPrefix} {pszPlayerName}: ";
		}
		else
			text = $"{pszPlayerName}: ";

		text = $"{p.SliceNullTerminatedString()}\n";

		// loop through all players
		// Start with the first player.
		// This may return the world in single player if the client types something between levels or during spawn
		// so check it, or it will infinite loop

		client = null;
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			client = ToHL2MPPlayer(Util.PlayerByIndex(i));
			if (client == null)
				continue;

			if (client.Edict() == null)
				continue;

			if (client.Edict() == edict)
				continue;

			if (!(client.IsNetClient()))   // Not a client ? (should never be true)
				continue;

			if (teamOnly && g_pGameRules!.PlayerCanHearChat(client, player) == GameRulesPlayerRelationship.NotTeammate )
				continue;

			// if (player != null && !client.CanHearAndReadChatFrom(player))
				// continue;

			// if (player != null && GetVoiceGameMgr()?.IsPlayerIgnoringPlayer(player->entindex(), i) ?? false)
				// continue;

			SingleUserRecipientFilter user = new(client);
			user.MakeReliable();

			if (!pszFormat.IsStringEmpty)
				Util.SayText2Filter(user, player, true, pszFormat, pszPlayerName, p, pszLocation);
			else
				Util.SayTextFilter(user, text, player, true);
		}

		if (player != null) {
			// print to the sending client
			SingleUserRecipientFilter user = new(player);
			user.MakeReliable();

			if (!pszFormat.IsStringEmpty)
				Util.SayText2Filter(user, player, true, pszFormat, pszPlayerName, p, pszLocation);
			else
				Util.SayTextFilter(user, text, player, true);
		}

		// echo to server console
		// Adrian: Only do this if we're running a dedicated server since we already print to console on the client.
		if (engine.IsDedicatedServer())
			Msg(text);

		Assert(!p.IsEmpty);

		int userid = 0;
		ReadOnlySpan<char> networkID = "Console";
		ReadOnlySpan<char> playerName = "Console";
		ReadOnlySpan<char> playerTeam = "Console";
		if (player != null) {
			userid = player.GetUserID();
			networkID = player.GetNetworkIDString();
			playerName = player.GetPlayerName();
			Team? team = player.GetTeam();
			if (team != null)
				playerTeam = team.GetName();
		}

		if (teamOnly)
			Util.LogPrintf($"\"{playerName}<{userid}><{networkID}><{playerTeam}>\" say_team \"{p}\"\n");
		else
			Util.LogPrintf($"\"{playerName}<{userid}><{networkID}><{playerTeam}>\" say \"{p}\"\n");

		IGameEvent? ev = gameeventmanager.CreateEvent("player_say", true);

		if (ev != null) {
			ev.SetInt("userid", userid);
			ev.SetString("text", p);
			ev.SetInt("priority", 1);   // HLTV event priority, not transmitted
			gameeventmanager.FireEvent(ev, true);
		}
	}
}

public static class ServerClient
{
#if !GMOD_DLL
	[ConCommand(helpText: "Noclip. Player becomes non-solid and flies.")]
	static void noclip() {

	}
#endif
}
