using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TEShowLine>;
public class C_TEShowLine : C_TEParticleSystem
{
	public static readonly RecvTable DT_TEShowLine = new(DT_TEParticleSystem, [
		RecvPropVector(FIELD.OF(nameof(End))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("TEShowLine", DT_TEShowLine).WithManualClassID(StaticClassIndices.CTEShowLine);

	public Vector3 End;
}

public static partial class TempEnts
{
	public static void TE_ShowLine(IRecipientFilter filter, float delay, in Vector3 start, in Vector3 end) {
		throw new NotImplementedException();
	}
}
