using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_EntityFlame>;
[NetworkName("CEntityFlame")]
public class C_EntityFlame : C_BaseEntity
{
	public static readonly RecvTable DT_EntityFlame = new(DT_BaseEntity, [
		RecvPropEHandle(FIELD.OF(nameof(EntAttached))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_EntityFlame);

	[NetworkName("m_hEntAttached")]
	public EHANDLE EntAttached = new();
}
