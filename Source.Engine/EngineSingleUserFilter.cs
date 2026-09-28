using Source.Common;
using Source.Common.Server;

namespace Source.Engine;

//-----------------------------------------------------------------------------
// Purpose:
//-----------------------------------------------------------------------------
public struct EngineRecipientFilter : IRecipientFilter
{
	bool m_bInit;
	bool m_bReliable;
	List<int>? m_Recipients;

	public EngineRecipientFilter() {
		Reset();
	}

	public readonly int GetRecipientCount() => m_Recipients?.Count ?? 0;

	public readonly int GetRecipientIndex(int slot) {
		if (slot < 0 || slot >= GetRecipientCount())
			return -1;

		return m_Recipients![slot];
	}

	public readonly bool IsReliable() => m_bReliable;
	public readonly bool IsInitMessage() => m_bInit;

	public void Reset() {
		m_bReliable = false;
		m_bInit = false;
		m_Recipients = [];
	}

	public void MakeReliable() {
		m_bReliable = true;
	}

	public void MakeInitMessage() {
		m_bInit = true;
	}

	public void AddAllPlayers() {
		m_Recipients ??= [];
		m_Recipients.Clear();

		for (int i = 0; i < sv.GetClientCount(); i++) {
			IClient? cl = sv.GetClient(i);

			if (cl == null || !cl.IsActive())
				continue;

			m_Recipients.Add(i + 1);
		}
	}

	public void AddRecipient(int index) {
		m_Recipients ??= [];

		// Already in list
		if (m_Recipients.Contains(index))
			return;

		m_Recipients.Add(index);
	}

	public void RemoveRecipient(int index) {
		// Remove it if it's in the list
		m_Recipients?.Remove(index);
	}

	public readonly bool IncludesPlayer(int playerindex) {
		for (int i = 0; i < GetRecipientCount(); i++) {
			if (playerindex == GetRecipientIndex(i))
				return true;
		}

		return false;
	}

	public void AddPlayersFromFilter<T>(in T filter) where T : IRecipientFilter {
		for (int i = 0; i < filter.GetRecipientCount(); i++)
			AddRecipient(filter.GetRecipientIndex(i));
	}
}

//-----------------------------------------------------------------------------
// Purpose: Simple filter for doing MSG_ONE type stuff directly in engine
//-----------------------------------------------------------------------------
public readonly struct EngineSingleUserFilter(int clientindex, bool bReliable = false) : IRecipientFilter
{
	readonly int m_nClientIndex = clientindex;
	readonly bool m_bReliable = bReliable;

	public bool IsReliable() => m_bReliable;
	public int GetRecipientCount() => 1;
	public int GetRecipientIndex(int slot) => m_nClientIndex;
	public bool IsInitMessage() => false;
}
