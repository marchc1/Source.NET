using System.Buffers;
using System.Collections;

namespace Source.Common.Networking.DataTable;

// RaphaelIT7: Clientside we won't need the CFrameSnapshot. For now atleast.
// public class CFrameSnapshot;
public class ClientFrame
{
	public ClientFrame()
	{
		LastEntity = 0;
		TransmitAlways = null;	// bit array used only by HLTV and replay client
		FromBaseline = null;
		TickCount = 0;
		// m_pSnapshot = null;
		Next = null;
	}

	public ClientFrame(int tickcount)
	{
		LastEntity = 0;
		TransmitAlways = null;	// bit array used only by HLTV and replay client
		FromBaseline = null;
		TickCount = tickcount;
		// m_pSnapshot = null;
		Next = null;
	}

	/*public CClientFrame(CFrameSnapshot pSnapshot)
	{
		last_entity = 0;
		transmit_always = null;	// bit array used only by HLTV and replay client
		from_baseline = null;
		tick_count = pSnapshot.m_nTickCount;
		m_pSnapshot = null;
		SetSnapshot( pSnapshot );
		m_pNext = null;
	}*/

	/*public void Init(CFrameSnapshot Snapshot);
	{
		TickCount = Snapshot.m_nTickCount;
		SetSnapshot( Snapshot );
	}*/

	public void Init(int tickcount)
	{
		TickCount = tickcount;
	}

	/*~CClientFrame()
	{
		SetSnapshot(null);
	}

	// Accessors to snapshots. The data is protected because the snapshots are reference-counted.
	public CFrameSnapshot GetSnapshot()
	{
		return m_pSnapshot;
	}

	public void SetSnapshot(CFrameSnapshot? Snapshot)
		if ( this.Snapshot == Snapshot )
			return;

		if( Snapshot )
			Snapshot.AddReference();

		if ( this.Snapshot )
			this.Snapshot.ReleaseReference();

		this.Snapshot = Snapshot;
	}*/

	public void CopyFrame(ClientFrame frame)
	{
		TickCount = frame.TickCount;	
		LastEntity = frame.LastEntity;
	
		// SetSnapshot(frame.GetSnapshot()); // adds reference to snapshot

		TransmitEntity = frame.TransmitEntity;

		if (frame.TransmitAlways != null)
		{
			TransmitAlways = new(frame.TransmitAlways);
		}
	}

	public static int FindNextSetBit(BitArray bitArray, int startIndex)
	{
		for (int i = startIndex; i < bitArray.Length; i++)
		{
			if (bitArray[i])
				return i;
		}

		return -1;
	}

	// State of entities this frame from the POV of the client.
	public int LastEntity;	// highest entity index
	public int TickCount;	// server tick of this snapshot

	// Used by server to indicate if the entity was in the player's pvs
	public BitArray TransmitEntity = new(Constants.MAX_EDICTS); // if bit n is set, entity n will be send to client
	public BitArray? FromBaseline;	// if bit n is set, this entity was send as update from baseline
	public BitArray? TransmitAlways; // if bit is set, don't do PVS checks before sending (HLTV only)

	private ClientFrame? Next;

	// Index of snapshot entry that stores the entities that were active and the serial numbers
	// for the frame number this packed entity corresponds to
	// m_pSnapshot MUST be private to force using SetSnapshot(), see reference counters
	// private CFrameSnapshot? Snapshot;
};

public class ClientFrameManager
{
	public int AddClientFrame(ClientFrame pFrame)
	{
		Frames.AddFirst(pFrame);

		return Frames.Count;
	}

	public ClientFrame? GetClientFrame(int nTick, bool bExact = true)
	{
		ClientFrame? lastBefore = null;

		foreach (var frame in Frames)
		{
			if (frame.TickCount == nTick)
				return frame;

			if (frame.TickCount < nTick && !bExact)
				lastBefore = frame;
		}

		return bExact ? null : lastBefore;
	}

	public void DeleteClientFrames(int nTick)
	{
		if (nTick < 0) // -1 for all.
		{
			Frames.Clear();
			return;
		}

		var node = Frames.Last;

		while (node != null)
		{
			var prev = node.Previous;
			if (node.Value.TickCount < nTick)
			{
				Frames.Remove(node);
			}
			node = prev;
		}
	}

	public int CountClientFrames()
	{
		return Frames.Count;
	}

	public void RemoveOldestFrame()
	{
		if (Frames.Count > 0)
		{
			Frames.RemoveLast();
		}
	}

	public ClientFrame AllocateFrame()
	{
		return new ClientFrame();
	}

	private LinkedList<ClientFrame> Frames = new LinkedList<ClientFrame>();
};