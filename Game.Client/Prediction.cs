using Game.Shared;

using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Client;
public class Prediction : IPrediction
{

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

	public void Update(int startFrame, bool validFrame, int incomingAcknowledged, int outgoingCommand) {
		throw new NotImplementedException();
	}

	public int GetIncomingPacketNumber() {
		return IncomingPacketNumber;
	}

	public bool InPrediction() {
		return bInPrediction;
	}
}
