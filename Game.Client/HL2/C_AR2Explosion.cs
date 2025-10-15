using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_AR2Explosion>;

public partial class C_AR2Explosion : C_BaseParticleEntity
{
	public static readonly RecvTable DT_AR2Explosion = new(DT_BaseParticleEntity, [
		RecvPropString(FIELD.OF(nameof(MaterialName)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass("AR2Explosion", DT_AR2Explosion).WithManualClassID(StaticClassIndices.AR2Explosion);

	InlineArray255<char> MaterialName;
}
