using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<Tesla>;
[LinkEntityToClass("point_tesla")]
[NetworkName("CTesla")]
public class Tesla : BaseEntity
{
	public static readonly SendTable DT_Tesla = new(DT_BaseEntity, [
		SendPropString(FIELD.OF(nameof(SoundName))),
		SendPropString(FIELD.OF(nameof(SpriteName))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_Tesla);

	[NetworkName("m_SoundName")]
	public InlineArray64<char> SoundName;
	[NetworkName("m_iszSpriteName")]
	public InlineArray256<char> SpriteName;
}
