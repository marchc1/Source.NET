using Source.Common.Bitbuffers;
using Source.Common.Entity;
using System.Collections;

namespace Source.Common.Networking.DataTable;

public class CEntityInfo
{
	public CEntityInfo()
	{
		OldEntity = -1;
		NewEntity = -1;
		HeaderBase = -1;
	}
	
	public bool AsDelta;
	public ClientFrame? From;
	public ClientFrame To;

	public UpdateType UpdateType;

	public int OldEntity; // current entity index in m_pFrom
	public int NewEntity; // current entity index in m_pTo

	public int HeaderBase;
	public int HeaderCount;

	public void NextOldEntity() 
	{
		if (From != null)
		{
			OldEntity = ClientFrame.FindNextSetBit(From.TransmitEntity, OldEntity+1);

			if (OldEntity < 0)
			{
				// Sentinel/end of list....
				OldEntity = Constants.ENTITY_SENTINEL;
			}
		} else {
			OldEntity = Constants.ENTITY_SENTINEL;
		}
	}

	public void NextNewEntity() 
	{
		NewEntity = ClientFrame.FindNextSetBit(To.TransmitEntity, NewEntity+1);

		if (NewEntity < 0)
		{
			// Sentinel/end of list....
			NewEntity = Constants.ENTITY_SENTINEL;
		}
	}
};

// PostDataUpdate calls are stored in a list until all ents have been updated.
public class CPostDataUpdateCall
{
	int Entity;
	DataUpdateType UpdateType;
};

public class CEntityReadInfo : CEntityInfo
{
	public CEntityReadInfo() 
	{
		TotalPostDataUpdateCalls = 0;
		LocalPlayerBits = 0;
		OtherPlayerBits = 0;
		UpdateType = UpdateType.PreserveEnt;
	}

	public bf_read Buf;
	public FHDR UpdateFlags;	// from the subheader
	public bool IsEntity;

	public int Baseline;	// what baseline index do we use (0/1)
	public bool UpdateBaselines; // update baseline while parsing snaphsot
		
	public int LocalPlayerBits; // profiling data
	public int OtherPlayerBits; // profiling data

	public CPostDataUpdateCall[] PostDataUpdateCalls = new CPostDataUpdateCall[Constants.MAX_EDICTS];
	public int TotalPostDataUpdateCalls;
};