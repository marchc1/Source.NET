using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Tesla>;
[NetworkName("CTesla")]
public partial class Tesla : BaseEntity
{
	public static readonly SendTable DT_Tesla = new(DT_BaseEntity, [
		SendPropStringT(NetworkVarFields.SoundName),
		SendPropStringT(NetworkVarFields.SpriteName)
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Tesla);

	[NetworkName("m_SoundName")]
	[NetworkVar] public partial string? SoundName { get; set; }
	[NetworkName("m_iszSpriteName")]
	[NetworkVar] public partial string? SpriteName { get; set; }
}
