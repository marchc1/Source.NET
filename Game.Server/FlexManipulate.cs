using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<FlexManipulate>;
[NetworkName("CFlexManipulate")]
public partial class FlexManipulate : BaseEntity
{
	public static readonly SendTable DT_FlexManipulate = new(DT_BaseEntity, [
		SendPropFloat(NetworkVarFields.ExScale, 0, PropFlags.NoScale),
		SendPropVector(NetworkVarFields.EyesLocalTarget, 0, PropFlags.NoScale),
		SendPropArray3(FIELD.OF_ARRAY(nameof(FlexWeights)), SendPropFloat(null!, 0, PropFlags.NoScale)),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_FlexManipulate);

	[NetworkName("m_flexScale")]
	[NetworkVar] public partial float ExScale { get; set; }
	[NetworkName("m_EyesLocalTarget")]
	[NetworkVar] public partial Vector3 EyesLocalTarget { get; set; }
	[NetworkName("m_iFlexWeights")]
	public InlineArray96<float> FlexWeights;
}
