using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_TESprite>;
[NetworkName("CTESprite")]
public class C_TESprite : C_BaseTempEntity
{
	public static readonly RecvTable DT_TESprite = new(DT_BaseTempEntity, [
		RecvPropVector(FIELD.OF(nameof(Origin))),
		RecvPropInt(FIELD.OF(nameof(ModelIndex))),
		RecvPropFloat(FIELD.OF(nameof(Scale))),
		RecvPropInt(FIELD.OF(nameof(Brightness))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_TESprite);

	[NetworkName("m_vecOrigin")]
	public Vector3 Origin;
	[NetworkName("m_nModelIndex")]
	public int ModelIndex;
	[NetworkName("m_fScale")]
	public float Scale;
	[NetworkName("m_nBrightness")]
	public int Brightness;
}

public static partial class TempEnts
{
	public static void TE_Sprite(IRecipientFilter filter, float delay, in Vector3 pos, int modelIndex, float size, int brightness) {
		throw new NotImplementedException();
	}
}
