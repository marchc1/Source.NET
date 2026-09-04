using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEBeamFollow>;
public class C_TEBeamFollow : C_BaseBeam
{
	public static readonly RecvTable DT_TEBeamFollow = new(DT_BaseBeam, [
		RecvPropInt(FIELD.OF(nameof(EntIndex))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEBeamFollow", DT_TEBeamFollow).WithManualClassID(StaticClassIndices.CTEBeamFollow);

	public int EntIndex;
}

public static partial class TempEnts
{
	public static void TE_BeamFollow(IRecipientFilter filter, float delay, int entIndex, int modelIndex, int haloIndex, float life, float width, float endWidth, float fadeLength, float r, float g, float b, float a) {
		throw new NotImplementedException();
	}
}
