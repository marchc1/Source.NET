using Game.Shared;

using Source;
using Source.Common;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Server;

public class RecipientFilter : IRecipientFilter
{
	public static readonly IPredictionSystem g_RecipientFilterPredictionSystem = new();

	bool Reliable;
	bool InitMessage;
	readonly List<int> Recipients = [];
	bool UsingPredictionRules;
	bool IgnoringPredictionCull;


	public bool IsReliable() => Reliable;
	public bool IsInitMessage() => InitMessage;
	public int GetRecipientCount() => Recipients.Count;
	public int GetRecipientIndex(int slot) {
		if (slot < 0 || slot >= GetRecipientCount())
			return -1;

		return Recipients[slot];
	}

	public void CopyFrom(scoped in RecipientFilter src) { }
	public void Reset() {
		Reliable = false;
		InitMessage = false;
		Recipients.Clear();
		UsingPredictionRules = false;
		IgnoringPredictionCull = false;
	}
	public void MakeInitMessage() { }
	public void MakeReliable() => Reliable = true;
	public void AddAllPlayers() {
		Recipients.Clear();

		int i;
		for (i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);
			if (player == null)
				continue;

			AddRecipient(player);
		}
	}
	public void AddRecipientsByPVS(in Vector3 origin) { }
	public void RemoveRecipientsByPVS(in Vector3 origin) { }
	public void AddRecipientsByPAS(in Vector3 origin) { }
	public void AddRecipient(BasePlayer player) {
		Assert(player != null);

		if (player == null)
			return;

		int index = player.EntIndex();

		// If we're predicting and this is not the first time we've predicted this sound
		//  then don't send it to the local player again.
		if (UsingPredictionRules)
			// Only add local player if this is the first time doing prediction
			if (g_RecipientFilterPredictionSystem.GetSuppressHost() == player)
				return;

		// Already in list
		if (Recipients.IndexOf(index) != -1)
			return;

		Recipients.Add(index);
	}
	public void RemoveAllRecipients() => Recipients.Clear();
	public void RemoveRecipient(BasePlayer player) { }
	public void RemoveRecipientByPlayerIndex(int playerindex) { }
	public void AddRecipientsByTeam(Team team) { }
	public void RemoveRecipientsByTeam(Team team) { }
	public void RemoveRecipientsNotOnTeam(Team team) { }
	public void UsePredictionRules() => UsingPredictionRules = true;
	public bool IsUsingPredictionRules() => UsingPredictionRules;
	public void SetIgnorePredictionCull(bool ignore) => IgnoringPredictionCull = ignore;
	public bool IgnorePredictionCull() => IgnoringPredictionCull;

#if GMOD_DLL
	public bool GMOD_HasRecipient(int slot) => throw new NotImplementedException();
	public bool GMOD_RemoveRecipientsByPAS(in Vector3 origin) => throw new NotImplementedException();
#endif
}


public class SingleUserRecipientFilter : RecipientFilter
{
	public SingleUserRecipientFilter(BasePlayer player) => AddRecipient(player);
}

public class TeamRecipientFilter : RecipientFilter
{
	public TeamRecipientFilter(int team, bool isReliable = false) {
		if (isReliable)
			MakeReliable();

		RemoveAllRecipients();

		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);

			if (player == null)
				continue;

			// todo

			AddRecipient(player);
		}
	}
}


public class BroadcastRecipientFilter : RecipientFilter
{
	public BroadcastRecipientFilter() => AddAllPlayers();
}


public class ReliableBroadcastRecipientFilter : BroadcastRecipientFilter
{
	public ReliableBroadcastRecipientFilter() : base() => MakeReliable();
}

public class BroadcastNonOwnerRecipientFilter : RecipientFilter
{
	public BroadcastNonOwnerRecipientFilter(BasePlayer player) {
		AddAllPlayers();
		RemoveRecipient(player);
	}
}


public class PVSFilter : RecipientFilter
{
	public PVSFilter(in Vector3 origin) => AddRecipientsByPVS(origin);
}
