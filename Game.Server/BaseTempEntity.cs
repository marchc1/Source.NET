
using Source.Common;

namespace Game.Server;

public class BaseTempEntity
{
	string Name = "";
	BaseTempEntity? Next;

	public static readonly SendTable DT_BaseTempEntity = new([]);
	public static readonly ServerClass ServerClass = new ServerClass("BaseTempEntity", DT_BaseTempEntity).WithManualClassID(Shared.StaticClassIndices.CBaseTempEntity);

	public static BaseTempEntity? s_pTempEntities = null;

	public BaseTempEntity(ReadOnlySpan<char> name){
		this.Name = new(name);
		Next = s_pTempEntities;
		s_pTempEntities = this;
	}

	public static BaseTempEntity? GetList() => s_pTempEntities;
	public BaseTempEntity? GetNext() => Next;

	public void Precache(){

	}

	public static void PrecacheTempEnts() {
		BaseTempEntity? te = GetList();
		while (te != null) {
			te.Precache();
			te = te.GetNext();
		}
	}
}
