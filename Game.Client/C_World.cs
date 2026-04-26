using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<Game.Client.C_World>;

namespace Game.Client;

[LinkEntityToClass("worldspawn")]
public class C_World : C_BaseEntity
{
	public C_World() : base() { 
		g_ClientWorld = this;
	}

	public static readonly RecvTable DT_World = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF(nameof(WorldMins))),
		RecvPropVector(FIELD.OF(nameof(WorldMaxs))),
		RecvPropInt(FIELD.OF(nameof(StartDark))),
		RecvPropFloat(FIELD.OF(nameof(MaxOccludeeArea))),
		RecvPropFloat(FIELD.OF(nameof(MinOccluderArea))),
		RecvPropFloat(FIELD.OF(nameof(MaxPropScreenSpaceWidth))),
		RecvPropFloat(FIELD.OF(nameof(MinPropScreenSpaceWidth))),
		RecvPropString(FIELD.OF(nameof(DetailSpriteMaterial))),
	]);

	public static new readonly ClientClass ClientClass = new ClientClass("World", null, null, DT_World)
																		.WithManualClassID(StaticClassIndices.CWorld);

	public override bool Init(int entNum, int serialNum) {
		WaveHeight = 0.0f;
		ActivityList.Init();

		return base.Init(entNum, serialNum);
	}

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

	public void RegisterSharedActivities() {
		ActivityList.RegisterSharedActivities();
		// EventList.RegisterSharedEvents();
	}

	public override void Precache() {
		ActivityList.Free();
		// EventList.Free();

		RegisterSharedActivities();

		// Get weapon precaches
		W_Precache();

		// Call all registered precachers.
		PrecacheRegister.Precache();
	}
	public override void Spawn() {
		Precache();
	}

	static C_World? g_ClientWorld;
	public static C_World GetClientWorldEntity() {
		Assert(g_ClientWorld != null);
		return g_ClientWorld;
	}
}
