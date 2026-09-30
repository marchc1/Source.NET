using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<RotorWashEmitter>;
[NetworkName("CRotorWashEmitter")]
public partial class RotorWashEmitter : BaseEntity
{
	public static readonly SendTable DT_RotorWashEmitter = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.Altitude, 0, PropFlags.NoScale),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_RotorWashEmitter);

	[NetworkName("m_flAltitude")]
	[NetworkVar] public partial float Altitude { get; set; }
}
