using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<BoneFollower>;
[NetworkName("CBoneFollower")]
public partial class BoneFollower : BaseEntity
{
	public static readonly SendTable DT_BoneFollower = new(DT_BaseEntity, [
		SendPropInt(NetworkVarFields.ModelIndex, 14, 0),
		SendPropInt(NetworkVarFields.SolidIndex, 6, PropFlags.Unsigned),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BoneFollower);

	[NetworkName("m_modelIndex")]
	[NetworkVar] public new partial int ModelIndex { get; set; }
	[NetworkName("m_solidIndex")]
	[NetworkVar] public partial int SolidIndex { get; set; }
}
