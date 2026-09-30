using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Mathematics;

namespace Game.Server;

using FIELD = FIELD<PlayerLocalData>;

public partial class PlayerLocalData
{
	public static readonly SendTable DT_Local = new(nameof(DT_Local), [
		SendPropArray3(PlayerLocalData.NetworkVarFields.AreaBits, SendPropInt(PlayerLocalData.NetworkVarFields.AreaBits.AtIndex(0)!, 8, PropFlags.Unsigned)),
		SendPropArray3(PlayerLocalData.NetworkVarFields.AreaPortalBits, SendPropInt(PlayerLocalData.NetworkVarFields.AreaPortalBits.AtIndex(0)!, 8, PropFlags.Unsigned)),
		SendPropInt(PlayerLocalData.NetworkVarFields.HideHUD, (int)HideHudBits.BitCount, PropFlags.Unsigned),
		SendPropFloat(PlayerLocalData.NetworkVarFields.FOVRate, 0, PropFlags.NoScale),
		SendPropInt(PlayerLocalData.NetworkVarFields.Ducked, 1, PropFlags.Unsigned),
		SendPropInt(PlayerLocalData.NetworkVarFields.Ducking, 1, PropFlags.Unsigned),
		SendPropInt(PlayerLocalData.NetworkVarFields.InDuckJump, 1, PropFlags.Unsigned),
		SendPropFloat(PlayerLocalData.NetworkVarFields.DuckTime, 12, PropFlags.RoundDown | PropFlags.ChangesOften, 0.0f, 2048.0f),
		SendPropFloat(PlayerLocalData.NetworkVarFields.DuckJumpTime, 12, PropFlags.RoundDown, 0.0f, 2048.0f),
		SendPropFloat(PlayerLocalData.NetworkVarFields.JumpTime, 12, PropFlags.RoundDown, 0.0f, 2048.0f),
		SendPropFloat(PlayerLocalData.NetworkVarFields.FallVelocity, 32, PropFlags.NoScale | PropFlags.ChangesOften, -4096.0f, 4096.0f),
		SendPropVector(PlayerLocalData.NetworkVarFields.PunchAngle, -1,  PropFlags.NoScale|PropFlags.ChangesOften),
		SendPropVector(PlayerLocalData.NetworkVarFields.PunchAngleVel, -1,  PropFlags.NoScale),
		SendPropInt(PlayerLocalData.NetworkVarFields.DrawViewmodel, 1, PropFlags.Unsigned),
		SendPropInt(PlayerLocalData.NetworkVarFields.WearingSuit, 1, PropFlags.Unsigned),
		SendPropBool(PlayerLocalData.NetworkVarFields.Poisoned),
		SendPropFloat(PlayerLocalData.NetworkVarFields.StepSize, 16, PropFlags.RoundUp, 0.0f, 512.0f),
		SendPropInt(PlayerLocalData.NetworkVarFields.AllowAutoMovement,1, PropFlags.Unsigned),

		SendPropInt(NetworkVarFields.Skybox3D_Scale, 12),
		SendPropVector(NetworkVarFields.Skybox3D_Origin, -1, PropFlags.Coord),
		SendPropInt(NetworkVarFields.Skybox3D_Area, 8, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Skybox3D_Fog_Enable, 1, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Skybox3D_Fog_Blend, 1, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Skybox3D_Fog_Radial, 1, PropFlags.Unsigned),
		SendPropVector(NetworkVarFields.Skybox3D_Fog_DirPrimary, -1, PropFlags.Coord),
		SendPropInt(NetworkVarFields.Skybox3D_Fog_ColorPrimary, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Skybox3D_Fog_ColorSecondary, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Skybox3D_Fog_ColorPrimaryHDR, 32, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.Skybox3D_Fog_ColorSecondaryHDR, 32, PropFlags.Unsigned),
		SendPropFloat(NetworkVarFields.Skybox3D_Fog_Start, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Skybox3D_Fog_End, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Skybox3D_Fog_MaxDensity, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Skybox3D_Fog_HDRColorScale, 0, PropFlags.NoScale),

		SendPropEHandle( NetworkVarFields.PlayerFog_Ctrl ),

		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(0)!, -1, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(1)!, -1, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(2)!, -1, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(3)!, -1, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(4)!, -1, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(5)!, -1, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(6)!, -1, PropFlags.Coord),
		SendPropVector(NetworkVarFields.Audio_LocalSound.AtIndex(7)!, -1, PropFlags.Coord),
		SendPropInt(NetworkVarFields.Audio_SoundscapeIndex, 17, 0),
		SendPropInt(NetworkVarFields.Audio_LocalBits, NUM_AUDIO_LOCAL_SOUNDS, PropFlags.Unsigned),
		SendPropEHandle(NetworkVarFields.Audio_Ent),

		SendPropFloat(FIELD.OF(nameof(SprintSpeed))),
		SendPropFloat(FIELD.OF(nameof(WalkSpeed))),
		SendPropFloat(FIELD.OF(nameof(SlowWalkSpeed))),
		SendPropFloat(FIELD.OF(nameof(LadderSpeed))),
		SendPropFloat(FIELD.OF(nameof(CrouchedWalkSpeed))),
		SendPropFloat(FIELD.OF(nameof(DuckSpeed))),
		SendPropFloat(FIELD.OF(nameof(UnDuckSpeed))),
		SendPropBool(FIELD.OF(nameof(DuckToggled))),
	]);

	[NetworkName("m_fSprintSpeed")]
	public float SprintSpeed;
	[NetworkName("m_fWalkSpeed")]
	public float WalkSpeed;
	[NetworkName("m_fSlowWalkSpeed")]
	public float SlowWalkSpeed;
	[NetworkName("m_fLadderSpeed")]
	public float LadderSpeed;
	[NetworkName("m_fCrouchedWalkSpeed")]
	public float CrouchedWalkSpeed;
	[NetworkName("m_fDuckSpeed")]
	public float DuckSpeed;
	[NetworkName("m_fUnDuckSpeed")]
	public float UnDuckSpeed;
	[NetworkName("m_bDuckToggled")]
	public bool DuckToggled;

	// TODO: NETWORK VARS!!!!!
	[NetworkName("m_chAreaBits")]
	[NetworkVar] public partial NetworkArray<InlineArrayMaxAreaStateBytes<byte>, byte> AreaBits { get; }
	[NetworkName("m_chAreaPortalBits")]
	[NetworkVar] public partial NetworkArray<InlineArrayMaxAreaPortalStateBytes<byte>, byte> AreaPortalBits { get; }
	[NetworkName("m_iHideHUD")]
	[NetworkVar] public partial bool HideHUD { get; set; }
	[NetworkName("m_flFOVRate")]
	[NetworkVar] public partial float FOVRate { get; set; }
	[NetworkName("m_bDucked")]
	[NetworkVar] public partial bool Ducked { get; set; }
	[NetworkName("m_bDucking")]
	[NetworkVar] public partial bool Ducking { get; set; }
	[NetworkName("m_bInDuckJump")]
	[NetworkVar] public partial bool InDuckJump { get; set; }
	[NetworkName("m_flDucktime")]
	[NetworkVar] public partial double DuckTime { get; set; }
	[NetworkName("m_flDuckJumpTime")]
	[NetworkVar] public partial double DuckJumpTime { get; set; }
	[NetworkName("m_flJumpTime")]
	[NetworkVar] public partial double JumpTime { get; set; }
	public int StepSide;
	[NetworkName("m_flFallVelocity")]
	[NetworkVar] public partial float FallVelocity { get; set; }
	public int OldButtons;
	public int OldForwardMove;
	[NetworkName("m_vecPunchAngle")]
	[NetworkVar] public partial QAngle PunchAngle { get; set; }
	[NetworkName("m_vecPunchAngleVel")]
	[NetworkVar] public partial QAngle PunchAngleVel { get; set; }
	[NetworkName("m_bDrawViewmodel")]
	[NetworkVar] public partial bool DrawViewmodel { get; set; }
	[NetworkName("m_bWearingSuit")]
	[NetworkVar] public partial bool WearingSuit { get; set; }
	[NetworkName("m_bPoisoned")]
	[NetworkVar] public partial bool Poisoned { get; set; }
	[NetworkName("m_flStepSize")]
	[NetworkVar] public partial float StepSize { get; set; }
	[NetworkName("m_bAllowAutoMovement")]
	[NetworkVar] public partial bool AllowAutoMovement { get; set; }
	public bool SlowMovement;

	[NetworkName("m_skybox3d")]
	[NetworkVarEmbedded] public partial Sky3DParams.NetworkVar Skybox3D { get; }
	[NetworkName("m_PlayerFog")]
	[NetworkVarEmbedded] public partial FogPlayerParams.NetworkVar PlayerFog { get; }
	[NetworkName("m_audio")]
	[NetworkVarEmbedded] public partial AudioParams.NetworkVar Audio { get; }

	public static void ClientData_Update(BasePlayer pl) {
		// TODO!
		// SkyCamera skyCamera = GetCurrentSkyCamera();
		// if (skyCamera != pl.Local.OldSkyCamera) {
		// 	pl.Local.OldSkyCamera = skyCamera;
		// 	pl.Local.Skybox3D.CopyFrom(skyCamera.SkyboxData);
		// }
		// else if (skyCamera == null)
		Sky3DParams.NetworkVar skybox3d = pl.Local.Skybox3D;
		skybox3d.Area = 255;
	}

	public static void UpdateAllClientData() {
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? pl = Util.PlayerByIndex(i);
			if (pl == null)
				continue;

			ClientData_Update(pl);
		}
	}

	public PlayerLocalData() {
		Ducked = false;
		Ducking = false;
		DuckSpeed = 0.1f;
		UnDuckSpeed = 0.1f;
		SprintSpeed = 400.0f;
		WalkSpeed = 200.0f;
		SlowWalkSpeed = 100.0f;
		LadderSpeed = 150.0f;
		CrouchedWalkSpeed = 0.3f;
	}
}
