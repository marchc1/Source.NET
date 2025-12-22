using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Client;

public class Prediction : IPrediction
{
	static readonly ConVar cl_predictweapons = new("cl_predictweapons", "1", FCvar.UserInfo | FCvar.NotConnected, "Perform client side prediction of weapon effects.");
	static readonly ConVar cl_lagcompensation = new("cl_lagcompensation", "1", FCvar.UserInfo | FCvar.NotConnected, "Perform server side lag compensation of weapon firing events.");
	static readonly ConVar cl_showerror = new("cl_showerror", "0", 0, "Show prediction errors, 2 for above plus detailed field deltas.");


	bool bInPrediction;
	bool FirstTimePredicted;
	bool OldCLPredictValue;
	bool EnginePaused;

	int PreviousStartFrame;

	int CommandsPredicted;
	int ServerCommandsAcknowledged;
	int PreviousAckHadErrors;
	int IncomingPacketNumber;

	float IdealPitch;

	public void GetLocalViewAngles(out QAngle ang) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			ang = default;
		else
			ang = player.pl.ViewingAngle;
	}

	public void GetViewAngles(out QAngle ang) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			ang = default;
		else
			ang = player.GetLocalAngles();
	}

	public void GetViewOrigin(out Vector3 org) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			org = default;
		else
			org = player.GetLocalOrigin();
	}

	public void Init() {
		OldCLPredictValue = cl_predict.GetInt() != 0;
	}

	public void CheckError(int commandsAcknowledged) {
		if (!engine.IsInGame())
			return;

		if (cl_predict.GetInt() == 0)
			return;

		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return;

		if (!player.IsIntermediateDataAllocated())
			return;

		Vector3 origin = player.GetNetworkOrigin();

	}

	public void OnReceivedUncompressedPacket() {
		CommandsPredicted = 0;
		ServerCommandsAcknowledged = 0;
		PreviousStartFrame = -1;
	}

	public void PostEntityPacketReceived() {
		throw new NotImplementedException();
	}

	public void PostNetworkDataReceived(int commandsAcknowledged) {
		throw new NotImplementedException();
	}

	public void PreEntityPacketReceived(int commandsAcknowledged, int currentWorldUpdatePacket) {
		Span<char> sz = stackalloc char[32];
		sprintf(sz, "preentitypacket%d").D(commandsAcknowledged);
		IncomingPacketNumber = currentWorldUpdatePacket;

		if (cl_predict.GetInt() == 0) {
			ShutdownPredictables();
			return;
		}

		C_BasePlayer? current = C_BasePlayer.GetLocalPlayer();
		if (current == null)
			return;

		int c = predictables.GetPredictableCount();
		int i;
		for (i = 0; i < c; i++) {
			C_BaseEntity? ent = predictables.GetPredictable(i);
			if (ent == null)
				continue;

			if (!ent.GetPredictable())
				continue;

			ent.PreEntityPacketReceived(commandsAcknowledged);
		}
	}

	private void ShutdownPredictables() {
		// todo
	}

	public void SetLocalViewAngles(in QAngle ang) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		player?.SetLocalViewAngles(ang);
	}

	public void SetViewAngles(in QAngle ang) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null) return;

		player.SetViewAngles(ang);
		player.IV_Rotation.Reset();
	}

	public void SetViewOrigin(in Vector3 org) {
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null) return;

		player.SetLocalOrigin(org);
		player.NetworkOrigin = org;
		player.IV_Origin.Reset();
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}


	readonly GlobalVarsBase saveVars = new(true);
	public void Update(int startFrame, bool validFrame, int incomingAcknowledged, int outgoingCommand) {
		EnginePaused = engine.IsPaused();
		bool receivedNewWorldUpdate = true;
		if (PreviousStartFrame == startFrame && cl_predict.GetInt() != 0)
			receivedNewWorldUpdate = false;
		PreviousStartFrame = startFrame;

		gpGlobals.CopyInstantiatedReferenceTo(saveVars);
		_Update(receivedNewWorldUpdate, validFrame, incomingAcknowledged, outgoingCommand);
		saveVars.CopyInstantiatedReferenceTo(gpGlobals);
	}

	void _Update(bool receivedNewWorldUpdate, bool validFrame, int incomingAcknowledged, int outgoingCommand) {
		C_BasePlayer? localPlayer = C_BasePlayer.GetLocalPlayer();
		if (localPlayer == null)
			return;

		engine.GetViewAngles(out QAngle viewangles);
		localPlayer.SetLocalAngles(viewangles);

		if (!validFrame)
			return;

		if (cl_predict.GetInt() == 0) {
			localPlayer.SetLocalViewAngles(viewangles);
			return;
		}


		C_BaseAnimating.InvalidateBoneCaches();
		using (C_BaseAnimating.AutoAllowBoneAccess boneaccess = new(true, true)) {

			// Remove any purely client predicted entities that were left "dangling" because the 
			//  server didn't acknowledge them or which can now safely be removed
			RemoveStalePredictedEntities(incomingAcknowledged);

			// Restore objects back to "pristine" state from last network/world state update
			if (receivedNewWorldUpdate)
				RestoreOriginalEntityState();

			if (!PerformPrediction(receivedNewWorldUpdate, localPlayer, incomingAcknowledged, outgoingCommand))
				return;
		}


		// Overwrite predicted angles with the actual view angles
		localPlayer.SetLocalAngles(viewangles);

		// This allows us to sample the world when it may not be ready to be sampled
		Assert(C_BaseEntity.IsAbsQueriesValid());

		// FIXME: What about hierarchy here?!?
		SetIdealPitch(localPlayer, localPlayer.GetLocalOrigin(), localPlayer.GetLocalAngles(), localPlayer.ViewOffset);
	}

	private void RemoveStalePredictedEntities(int sequenceNumber) {
		int oldest_allowable_command = sequenceNumber;

		int c = predictables.GetPredictableCount();
		int i;
		for (i = c - 1; i >= 0; i--) {
			C_BaseEntity? ent = predictables.GetPredictable(i);
			if (ent == null)
				continue;

			// Don't do anything to truly predicted things (like player and weapons )
			if (ent.GetPredictable())
				continue;

			// What's left should be things like projectiles that are just waiting to be "linked"
			//  to their server counterpart and deleted
			Assert(ent.IsClientCreated());
			if (!ent.IsClientCreated())
				continue;

			// Snag the PredictionContext
			PredictionContext? ctx = ent.PredictionContext;
			if (ctx == null)
				continue;

			// If it was ack'd then the server sent us the entity.
			// Leave it unless it wasn't made dormant this frame, in
			//  which case it can be removed now
			if (ent.PredictableId.GetAcknowledged()) {
				// Hasn't become dormant yet!!!
				if (!ent.IsDormantPredictable()) {
					Assert(0);
					continue;
				}

				// Still gets to live till next frame
				if (ent.BecameDormantThisPacket())
					continue;

				C_BaseEntity? serverEntity = ctx.ServerEntity.Get();
				if (serverEntity != null) {
					// Notify that it's going to go away
					serverEntity.OnPredictedEntityRemove(true, ent);
				}
			}
			else {
				// Check context to see if it's too old?
				int command_entity_creation_happened = ctx.CreationCommandNumber;
				// Give it more time to live...not time to kill it yet
				if (command_entity_creation_happened > oldest_allowable_command)
					continue;

				// If the client predicted the KILLME flag it's possible
				//  that entity had such a short life that it actually
				//  never was sent to us.  In that case, just let it die a silent death
				if (!ent.IsEFlagSet(EFL.KillMe)) {
					if (cl_showerror.GetInt() != 0) {
						// It's bogus, server doesn't have a match, destroy it:
						Msg($"Removing unack'ed predicted entity: {ent.GetClassname()} created {ctx.CreationModule}({ctx.CreationLineNumber}) id == {ent.PredictableId.Describe()} : {ent}\n");
					}
				}

				// FIXME:  Do we need an OnPredictedEntityRemove call with an "it's not valid"
				// flag of some kind
			}

			// This will remove it from predictables list and will also free the entity, etc.
			ent.Release();
		}
	}

	private bool PerformPrediction(bool receivedNewWorldUpdate, BasePlayer localPlayer, int incomingAcknowledged, int outgoingCommand) {
		Assert(C_BaseEntity.IsAbsQueriesValid());
		Assert(C_BaseEntity.IsAbsRecomputationsEnabled());
		/*
		bInPrediction = true;

		// undo interpolation changes for entities we stand on
		C_BaseEntity? entity = localPlayer.GetGroundEntity();

		while (entity != null && entity.EntIndex() > 0) {
			entity.MoveToLastReceivedPosition();
			// undo changes for moveparents too
			entity = entity.GetMoveParent();
		}

		// Start at command after last one server has processed and 
		//  go until we get to targettime or we run out of new commands
		int i = ComputeFirstCommandToExecute(receivedNewWorldUpdate, incomingAcknowledged, outgoingCommand);

		Assert(i >= 1);
		while (true) {
			// Incoming_acknowledged is the last usercmd the server acknowledged having acted upon
			int current_command = incomingAcknowledged + i;

			// We've caught up to the current command.
			if (current_command > outgoingCommand)
				break;

			if (i >= MULTIPLAYER_BACKUP)
				break;

			ref UserCmd cmd = ref input.GetUserCmd(current_command);

			if (Unsafe.IsNullRef(ref cmd))
				break;

			// Is this the first time predicting this
			FirstTimePredicted = !cmd.HasBeenPredicted;

			// Set globals appropriately
			TimeUnit_t curtime = (localPlayer.TickBase) * TICK_INTERVAL;

			RunSimulation(current_command, curtime, cmd, localPlayer);

			gpGlobals.CurTime = curtime;
			gpGlobals.FrameTime = EnginePaused ? 0 : TICK_INTERVAL;

			// Call untouch on any entities no longer predicted to be touching
			Untouch();

			// Store intermediate data into appropriate slot
			StorePredictionResults(i - 1); // Note that I starts at 1

			CommandsPredicted = i;

			if (current_command == outgoingCommand) {
				localPlayer.FinalPredictedTick = localPlayer.TickBase;
			}
			// Mark that we issued any needed sounds, of not done already
			cmd.HasBeenPredicted = true;

			// Copy the state over.
			i++;
		}

		//	Msg( "%i : predicted %i commands forward, %i ack'd last frame, had errors %s\n", 
		//		gpGlobals->tickcount, 
		//		m_nCommandsPredicted, 
		//		m_nServerCommandsAcknowledged,
		//		m_bPreviousAckHadErrors ? "true" : "false" );

		bInPrediction = false;

		// Somehow we looped past the end of the list (severe lag), don't predict at all
		if (i > MULTIPLAYER_BACKUP) 
			return false;
		*/
		return true;
	}

	private void RestoreOriginalEntityState() {
		Assert(C_BaseEntity.IsAbsRecomputationsEnabled());

		// Transfer intermediate data from other predictables
		int pc = predictables.GetPredictableCount();
		int p;
		for (p = 0; p < pc; p++) {
			C_BaseEntity? ent = predictables.GetPredictable(p);
			if (ent == null)
				continue;

			if (ent.GetPredictable()) {
				ent.RestoreData("RestoreOriginalEntityState", C_BaseEntity.SLOT_ORIGINALDATA, PredictionCopyType.Everything);
			}
		}
	}

	private void SetIdealPitch(C_BasePlayer localPlayer, Vector3 vector3, QAngle qAngle, Vector3 viewOffset) {
		// todo
	}

	public int GetIncomingPacketNumber() {
		return IncomingPacketNumber;
	}

	public bool IsFirstTimePredicted() {
		return FirstTimePredicted;
	}
	public bool InPrediction() {
		return bInPrediction;
	}
}
