#if CLIENT_DLL || GAME_DLL
using Source.Common;
namespace Game.Shared;
using FIELD = Source.FIELD<BaseDoor>;
public partial class BaseDoor : BaseToggle
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_BaseDoor = new(DT_BaseToggle, [
#if CLIENT_DLL
		RecvPropFloat(FIELD.OF(nameof(WaveHeight)))
#else
		SendPropFloat(FIELD.OF(nameof(WaveHeight)), 8, PropFlags.RoundUp)
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("BaseDoor", null, null, DT_BaseDoor).WithManualClassID(StaticClassIndices.CBaseDoor);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("BaseDoor", DT_BaseDoor).WithManualClassID(StaticClassIndices.CBaseDoor);
#endif
	public float WaveHeight;
}
#endif
