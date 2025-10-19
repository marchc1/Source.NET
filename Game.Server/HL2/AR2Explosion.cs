using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server.HL2;
using FIELD = Source.FIELD<AR2Explosion>;
public partial class AR2Explosion : BaseParticleEntity
{
	public static readonly SendTable DT_AR2Explosion = new(DT_BaseParticleEntity, [
		SendPropString(FIELD.OF(nameof(MaterialName)))
	]);
	public static readonly new ServerClass ServerClass = new ServerClass("AR2Explosion", DT_AR2Explosion).WithManualClassID(StaticClassIndices.AR2Explosion);

	InlineArray255<char> MaterialName;
}
