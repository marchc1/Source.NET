using Source.Common;

namespace Source.Engine;

public class LocalNetworkBackdoor
{
	public LocalNetworkBackdoor() {

	}

	public void StartEntityStateUpdate() {

	}

	public void EndEntityStateUpdate() {

	}

	public void EntityDormant(int ent, int serialNum) {

	}

	public void AddToPendingDormantEntityList(uint edict) {

	}

	public void ProcessDormantEntities() {

	}

	public void NotifyEdictFlagsChange(uint edict) {
		if ((sv.Edicts![edict].StateFlags & Source.Common.Engine.EdictFlags.DontSend) != 0)
			AddToPendingDormantEntityList(edict);
	}

	public void EntState(
		int ent,
		int serialNum,
		int @class,
		SendTable sendTable,
		object sourceEnt,
		bool changed,
		bool shouldTransmit) {

	}

	public void ClearState() {
		for (int i = 0; i < Constants.MAX_EDICTS; i++) {
			_CachedEntState ces = CachedEntState[i];

			ces.Networkable = null;
			ces.SerialNumber = -1;
			ces.Dormant = false;
			ces.DataPointer = null;
		}

		PrevEntsAlive.ClearAll();
	}

	public void StartBackdoorMode() {
		ClearState();

		for (int i = 0; i < Constants.MAX_EDICTS; i++) {
			IClientNetworkable? net = entitylist.GetClientNetworkable(i);

			_CachedEntState ces = CachedEntState[i];

			if (net != null) {
				ces.Networkable = net;
				ces.SerialNumber = net.GetIClientUnknown().GetRefEHandle()!.GetSerialNumber();
				ces.Dormant = net.IsDormant();
				ces.DataPointer = net.GetDataTableBasePtr();
				PrevEntsAlive.Set(i);
			}
		}
	}

	public void StopBackdoorMode() {
		ClearState();
	}
	
	public static void InitFastCopy() {

	}

	MaxEdictsBitVec EntsAlive;
	MaxEdictsBitVec PrevEntsAlive;
	InlineArrayMaxEdicts<uint> EntsCreatedIndices;
	int EntsCreated;
	InlineArrayMaxEdicts<uint> EntsChangedIndices;
	int EntsChanged;
	readonly LinkedList<uint> PendingDormantEntities = new();
	class _CachedEntState
	{
		public _CachedEntState() {
			SerialNumber = -1;
			DataPointer = null;
			Networkable = null;
		}

		public bool Dormant;
		public int SerialNumber;
		public object? DataPointer;
		public IClientNetworkable? Networkable;
	}

	InlineArrayNewMaxEdicts<_CachedEntState> CachedEntState = new();
}
