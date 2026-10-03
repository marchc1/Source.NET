using Game.Server.HL2;
using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server;

using DEFINE = Source.DEFINE<EnvZoom>;

[LinkEntityToClass("env_zoom")]
public class EnvZoom : PointEntity
{
	const int ENV_ZOOM_OVERRIDE = 1 << 0;

	float Rate;
	int FOV;

	public static readonly new DataMap DataDesc = new(typeof(EnvZoom), BaseEntity.DataDesc, [
		DEFINE.KEYFIELD(nameof(Rate), FieldType.Float, "Rate"),
		DEFINE.KEYFIELD(nameof(FOV), FieldType.Integer, "FOV"),

		DEFINE.INPUTFUNC(FieldType.Void, "Zoom", nameof(InputZoom), (INPUTFUNCPTR)((self, data) => ((EnvZoom)self).InputZoom(data))),
		DEFINE.INPUTFUNC(FieldType.Void, "UnZoom", nameof(InputUnZoom), (INPUTFUNCPTR)((self, data) => ((EnvZoom)self).InputUnZoom(data))),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	public int GetFOV() => FOV;
	public float GetSpeed() => Rate;

	public static bool CanOverrideEnvZoomOwner(BaseEntity? zoomOwner) {
		if (zoomOwner is not EnvZoom zoom || zoom.HasSpawnFlags(ENV_ZOOM_OVERRIDE) == false)
			return false;
		return true;
	}

	public static float GetZoomOwnerDesiredFOV(BaseEntity? zoomOwner) {
		if (CanOverrideEnvZoomOwner(zoomOwner))
			return ((EnvZoom)zoomOwner!).GetFOV();

		return 0;
	}

	public void InputZoom(InputData inputdata) {
		BasePlayer? player = ToBasePlayer(inputdata.Activator);
		if (player == null) player = ToBasePlayer(inputdata.Caller);

#if GMOD_DLL
		if (player == null && gpGlobals.MaxClients == 1)
			player = Util.GetLocalPlayer();
#endif

		if (player != null) {
#if HL2_DLL
			if (player == player.GetFOVOwner()) {
				HL2_Player hlPlayer = (HL2_Player)player;

				hlPlayer.StopZooming();
			}
#endif

			if (player.GetFOVOwner() != null && FClassnameIs(player.GetFOVOwner(), "env_zoom"))
				player.ClearZoomOwner();

			player.SetFOV(this, FOV, Rate);
		}
	}

	public void InputUnZoom(InputData inputdata) {
		BasePlayer? player = ToBasePlayer(inputdata.Activator);
		if (player == null) player = ToBasePlayer(inputdata.Caller);

#if GMOD_DLL
		if (player == null && gpGlobals.MaxClients == 1)
			player = Util.GetLocalPlayer();
#endif

		if (player != null)
			player.SetFOV(this, 0);
	}
}
