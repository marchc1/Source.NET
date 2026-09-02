using Source.Common;
using Source.Common.Engine;

namespace Source.Engine;

public class LocalNetworkBackdoor
{
	public LocalNetworkBackdoor() {

	}

	public void StartEntityStateUpdate() {
		EntsAlive.ClearAll();
		EntsCreated = 0;
		EntsChanged = 0;

		// signal client that we start updating entities
		ClientDLL.FrameStageNotify(Source.Common.Client.ClientFrameStage.NetUpdateStart);
	}

	public void EndEntityStateUpdate() {
		ClientDLL.FrameStageNotify(Source.Common.Client.ClientFrameStage.NetUpdatePostDataUpdateStart);

		// Handle entities created.
		int i;
		for (i = 0; i < EntsCreated; i++) {
			int iEdict = (int)EntsCreatedIndices[i];
			_CachedEntState cached = CachedEntState[iEdict];
			IClientNetworkable net = cached.Networkable!;

			net.PostDataUpdate(DataUpdateType.Created);
			net.NotifyShouldTransmit(ShouldTransmiteState.Start);
			cached.Dormant = false;
		}

		// Handle entities changed.
		for (i = 0; i < EntsChanged; i++) {
			int iEdict = (int)EntsChangedIndices[i];
			CachedEntState[iEdict].Networkable!.PostDataUpdate(DataUpdateType.DataTableChanged);
		}

		ClientDLL.FrameStageNotify(Source.Common.Client.ClientFrameStage.NetUpdatePostDataUpdateEnd);

		// Handle entities removed (= SV_WriteDeletions() in normal mode)
		int nDWords = PrevEntsAlive.GetNumDWords();

		// Handle entities removed.
		for (i = 0; i < nDWords; i++) {
			uint prevEntsAlive = PrevEntsAlive.GetDWord(i);
			uint entsAlive = EntsAlive.GetDWord(i);
			uint toDelete = (prevEntsAlive ^ entsAlive) & prevEntsAlive;

			if (toDelete != 0) {
				for (int iBit = 0; iBit < 32; iBit++) {
					if ((toDelete & (1 << iBit)) != 0) {
						int iEdict = (i << 5) + iBit;
						if (iEdict >= 0 && iEdict < Constants.MAX_EDICTS) {
							if (CachedEntState[iEdict].Networkable != null) {
								CachedEntState[iEdict].Networkable!.Release();
								CachedEntState[iEdict].Networkable = null;
							}
							else {
								// todo: AssertOnce(!"EndEntityStateUpdate:  Would have crashed with NULL m_pNetworkable\n");
							}
						}
						else {
							// todo: AssertOnce(!"EndEntityStateUpdate:  Would have crashed with entity out of range\n");
						}
					}
				}
			}
		}

		// Remember the previous state of which entities were around.
		PrevEntsAlive = EntsAlive;

		// end of all entity update activity
		ClientDLL.FrameStageNotify(Source.Common.Client.ClientFrameStage.NetUpdateEnd);
	}

	public void EntityDormant(int ent, int serialNum) {
		_CachedEntState cached = CachedEntState[ent];

		IClientNetworkable? net = cached.Networkable;
		Assert(net == entitylist.GetClientNetworkable(ent));
		if (net != null) {
			Assert(cached.SerialNumber == net.GetIClientUnknown().GetRefEHandle().GetSerialNumber());
			if (cached.SerialNumber == serialNum) {
				EntsAlive.Set(ent);

				// Tell the game code that this guy is now dormant.
				Assert(cached.Dormant == net.IsDormant());
				if (!cached.Dormant) {
					net.NotifyShouldTransmit(ShouldTransmiteState.End);
					cached.Dormant = true;
				}
			}
			else {
				net.Release();
				cached.Networkable = null;
				PrevEntsAlive.Clear(ent);
			}
		}
	}

	public void AddToPendingDormantEntityList(uint edict) {
		Edict e = sv.Edicts![edict];
		if (0 == (e.StateFlags & EdictFlags.PendingDormantCheck)) {
			e.StateFlags |= EdictFlags.PendingDormantCheck;
			PendingDormantEntities.AddLast(edict);
		}
	}

	public void ProcessDormantEntities() {
		foreach (var edict in PendingDormantEntities) {
			Edict e = sv.Edicts![edict];

			// Make sure the entity still exists and stil has the dontsend flag set.
			if (e.IsFree() || 0 == (e.StateFlags & EdictFlags.DontSend)) {
				e.StateFlags &= ~EdictFlags.PendingDormantCheck;
				continue;
			}

			EntityDormant((int)edict, e.NetworkSerialNumber);
			e.StateFlags &= ~EdictFlags.PendingDormantCheck;
		}
		PendingDormantEntities.Clear();
	}

	public void NotifyEdictFlagsChange(uint edict) {
		if ((sv.Edicts![edict].StateFlags & Source.Common.Engine.EdictFlags.DontSend) != 0)
			AddToPendingDormantEntityList(edict);
	}

	public void EntState(int ent, int serialNum, int @class, SendTable sendTable, object sourceEnt, bool changed, bool shouldTransmit) {
		_CachedEntState cached = CachedEntState[ent];

		// Remember that this ent is alive.
		EntsAlive.Set(ent);

		ClientClass? clientClass = cl.GetClientClass(@class);
		if (clientClass == null)
			Error($"LocalNetworkBackdoor.EntState - missing client class {@class}");

		IClientNetworkable? net = cached.Networkable;
		Assert(net == entitylist.GetClientNetworkable(ent));

		if (!shouldTransmit) {
			if (net != null) {
				Assert(cached.SerialNumber == net.GetIClientUnknown().GetRefEHandle().GetSerialNumber());
				if (cached.SerialNumber == serialNum) {
					// Tell the game code that this guy is now dormant.
					Assert(cached.Dormant == net.IsDormant());
					if (!cached.Dormant) {
						net.NotifyShouldTransmit(ShouldTransmiteState.End);
						cached.Dormant = true;
					}
				}
				else {
					net.Release();
					net = null;
					cached.Networkable = null;
					// Since we set this above, need to clear it now to avoid assertion in EndEntityStateUpdate()
					EntsAlive.Clear(ent);
					PrevEntsAlive.Clear(ent);
				}
			}
			else {
				EntsAlive.Clear(ent);
			}
			return;
		}
		// Do we have an entity here already?
		bool bExistedAndWasDormant = false;
		if (net != null) {
			// If the serial numbers are different, make it recreate the ent.
			Assert(cached.SerialNumber == net.GetIClientUnknown().GetRefEHandle().GetSerialNumber());
			if (serialNum == cached.SerialNumber) {
				bExistedAndWasDormant = cached.Dormant;
			}
			else {
				net.Release();
				net = null;
				PrevEntsAlive.Clear(ent);
			}
		}

		// Create the entity?
		bool bCreated = false;
		DataUpdateType updateType;
		if (net != null) {
			updateType = DataUpdateType.DataTableChanged;
		}
		else {
			updateType = DataUpdateType.Created;
			net = clientClass.CreateFn(ent, serialNum);
			bCreated = true;
			EntsCreatedIndices[EntsCreated++] = (uint)ent;

			cached.SerialNumber = serialNum;
			cached.DataPointer = net.GetDataTableBasePtr();
			cached.Networkable = net;
			cached.Dormant = net.IsDormant();
		}

		if (changed || bCreated || bExistedAndWasDormant) {
			net!.PreDataUpdate(updateType);

			Assert(cached.DataPointer == net.GetDataTableBasePtr());

			LocalTransfer.TransferEntity(
				sv.Edicts![ent],
				sendTable,
				sourceEnt,
				clientClass.RecvTable,
				cached.DataPointer,
				bCreated,
				bExistedAndWasDormant,
				ent);

			if (bExistedAndWasDormant)
				// Set this so we use DATA_UPDATE_CREATED logic
				EntsCreatedIndices[EntsCreated++] = (uint)ent;
			else {
				if (!bCreated)
					EntsChangedIndices[EntsChanged++] = (uint)ent;
			}
		}
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
		if (!cl.NetChannel!.IsLoopback())
			return;

		StandardSendProxies sendProxies = serverGameDLL.GetStandardSendProxies();
		StandardRecvProxies recvProxies = g_ClientDLL!.GetStandardRecvProxies();

		int nFastCopyProps = 0;
		int nSlowCopyProps = 0;

		for (int iClass = 0; iClass < cl.NumServerClasses; iClass++) {
			ClientClass? clientClass = cl.GetClientClass(iClass);
			if (clientClass == null)
				Error($"InitFastCopy - missing client class {iClass} (Should be equivelent of server class: {cl.ServerClasses![iClass]!.ClassName})");

			ServerClass? serverClass = SV.FindServerClass(clientClass.GetName());
			if (serverClass == null)
				Error($"InitFastCopy - missing server class {clientClass.GetName()}");

			LocalTransfer.InitFastCopy(
				serverClass.Table,
				sendProxies,
				clientClass.RecvTable,
				recvProxies,
				ref nSlowCopyProps,
				ref nFastCopyProps
				);
		}

		int percentFast = (nFastCopyProps * 100) / (nSlowCopyProps + nFastCopyProps + 1);
		if (percentFast <= 55) {
			// This may not be a real problem, but at the time this code was added, 67% of the
			// properties were able to be copied without proxies. If percentFast goes to 0 or some
			// really low number suddenly, then something probably got screwed up.
			Assert(false);
			Warning($"InitFastCopy: only {percentFast}% fast props. Bug?\n");
		}
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
