#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source.Common;
using Game.Shared;
using Source;
using System.Numerics;

#if CLIENT_DLL
namespace Game.Client;
using FIELD = Source.FIELD<C_PhysBeam>;
#else
namespace Game.Server;
using FIELD = Source.FIELD<PhysBeam>;
#endif

using Table =
#if CLIENT_DLL
	RecvTable;
#else
	SendTable;
#endif

using Class =
#if CLIENT_DLL
	ClientClass;
#else
	ServerClass;
#endif

public partial class
#if CLIENT_DLL
    C_PhysBeam 
#else
	PhysBeam
#endif
	: SharedBaseEntity
{
	public static readonly Table DT_PhysBeam = new(DT_BaseEntity, [
#if CLIENT_DLL
		RecvPropEHandle(FIELD.OF(nameof(TargetEnt))),
		RecvPropVector(FIELD.OF(nameof(HoldPos))),
		RecvPropBool(FIELD.OF(nameof(IsOn))),
		RecvPropInt(FIELD.OF(nameof(PhysBone))),
#elif GAME_DLL
		SendPropEHandle(FIELD.OF(nameof(TargetEnt))),
		SendPropVector(FIELD.OF(nameof(HoldPos)), 0, PropFlags.NoScale),
		SendPropBool(FIELD.OF(nameof(IsOn))),
		SendPropInt(FIELD.OF(nameof(PhysBone)), 32, 0),
#endif
	]);

	public static readonly new Class
#if CLIENT_DLL
		ClientClass
#else
		ServerClass
#endif
		= new Class("PhysBeam", DT_PhysBeam).WithManualClassID(StaticClassIndices.CPhysBeam);

	public readonly EHANDLE TargetEnt = new();
	public Vector3 HoldPos;
	public bool IsOn;
	public int PhysBone;
}
#endif
