using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
using Source.Engine;
namespace Game.Client;

using FIELD = FIELD<C_TEDecal>;
public class C_TEDecal : C_BaseTempEntity
{
	public static readonly RecvTable DT_TEDecal = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropVector(FIELD.OF(nameof(Start))),
		RecvPropInt(FIELD.OF(nameof(Entity))),
		RecvPropInt(FIELD.OF(nameof(Hitbox))),
		RecvPropInt(FIELD.OF(nameof(Index))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEDecal", DT_TEDecal).AsEvent<C_TEDecal>().WithManualClassID(StaticClassIndices.CTEDecal);

	public Vector3 Origin;
	public Vector3 Start;
	public int Entity;
	public int Hitbox;
	public int Index;

	public override void PostDataUpdate(DataUpdateType updateType) {
		BroadcastRecipientFilter filter = new();
		TE_Decal(filter, 0.0f, in Origin, in Start, Entity, Hitbox, Index);
	}
}

public static partial class TempEnts
{
	public static void TE_Decal(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 start, int entity, int hitbox, int index) {
		Trace tr = default;

		if (entity == 0 && hitbox != 0) {
			Ray ray = new();
			ray.Init(in start, in pos);
			StaticPropMgrGlobals.g_StaticPropMgr.AddDecalToStaticProp(start, pos, hitbox - 1, index, false, tr);
		}
		else {
			C_BaseEntity? ent = cl_entitylist.GetEnt(entity);
			if (ent == null)
				return;

			ent.AddDecal(in start, in pos, in pos, hitbox, index, false, ref tr);
		}
	}
}
