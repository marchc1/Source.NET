using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FireSmoke>;
[NetworkName("CFireSmoke")]
public partial class FireSmoke : BaseEntity
{
	public static readonly SendTable DT_FireSmoke = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.StartScale, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.Scale, 0, PropFlags.NoScale),
		SendPropFloat(NetworkVarFields.ScaleTime, 0, PropFlags.NoScale),
		SendPropInt(NetworkVarFields.Flags, 8, PropFlags.Unsigned),
		SendPropInt(NetworkVarFields.FlameModelIndex, 14, 0),
		SendPropInt(NetworkVarFields.FlameFromAboveModelIndex, 14, 0),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FireSmoke);

	[NetworkName("m_flStartScale")]
	[NetworkVar] public partial float StartScale { get; set; }
	[NetworkName("m_flScale")]
	[NetworkVar] public partial float Scale { get; set; }
	[NetworkName("m_flScaleTime")]
	[NetworkVar] public partial float ScaleTime { get; set; }
	[NetworkName("m_nFlags")]
	[NetworkVar] public partial int Flags { get; set; }
	[NetworkName("m_nFlameModelIndex")]
	[NetworkVar] public partial int FlameModelIndex { get; set; }
	[NetworkName("m_nFlameFromAboveModelIndex")]
	[NetworkVar] public partial int FlameFromAboveModelIndex { get; set; }
}
