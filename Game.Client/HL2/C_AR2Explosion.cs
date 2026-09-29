using Game.Shared;

using Source;
using Source.Common;

namespace Game.Client.HL2;
using FIELD = Source.FIELD<C_AR2Explosion>;

[NetworkName("AR2Explosion")]
public partial class C_AR2Explosion : C_BaseParticleEntity
{
	public static readonly RecvTable DT_AR2Explosion = new(DT_BaseParticleEntity, [
		RecvPropString(FIELD.OF(nameof(MaterialName)))
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_AR2Explosion);

	[NetworkName("m_szMaterialName")]
	InlineArray255<char> MaterialName;
}
