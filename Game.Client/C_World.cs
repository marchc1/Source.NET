using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<Game.Client.C_World>;

namespace Game.Client;

[LinkEntityToClass("worldspawn")]
public class C_World : C_BaseEntity
{
	public static readonly RecvTable DT_World = new(DT_BaseEntity, [
		RecvPropFloat(FIELD.OF(nameof(WaveHeight))),
		RecvPropVector(FIELD.OF(nameof(WorldMins))),
		RecvPropVector(FIELD.OF(nameof(WorldMaxs))),
		RecvPropInt(FIELD.OF(nameof(StartDark))),
		RecvPropFloat(FIELD.OF(nameof(MaxOccludeeArea))),
		RecvPropFloat(FIELD.OF(nameof(MinOccluderArea))),
		RecvPropFloat(FIELD.OF(nameof(MaxPropScreenSpaceWidth))),
		RecvPropFloat(FIELD.OF(nameof(MinPropScreenSpaceWidth))),
		RecvPropString(FIELD.OF(nameof(DetailSpriteMaterial))),
		RecvPropInt(FIELD.OF(nameof(ColdWorld))),
	]);

	public static new readonly ClientClass ClientClass = new ClientClass("World", null, null, DT_World)
																		.WithManualClassID(StaticClassIndices.CWorld);


	float WaveHeight;
	Vector3 WorldMins;
	Vector3 WorldMaxs;
	bool StartDark;
	float MaxOccludeeArea;
	float MinOccluderArea;
	float MaxPropScreenSpaceWidth;
	float MinPropScreenSpaceWidth;
	InlineArray256<char> DetailSpriteMaterial;
	bool ColdWorld;

	void W_Precache() {
		WeaponParse.PrecacheFileWeaponInfoDatabase(filesystem);
	}

	public override void Precache() {
		// ActivityList_Free();
		// EventList_Free();

		// RegisterSharedActivities();

		// Get weapon precaches
		W_Precache();

		// Call all registered precachers.
		PrecacheRegister.Precache();
	}
	public override void Spawn() {
		Precache();
	}
}
