global using EHANDLE = Source.Common.Handle<Game.Client.C_BaseEntity>;
global using ClientLeafShadowHandle_t = uint;
global using TextureHandle_t = ushort;
global using FragmentHandle_t = ushort;
global using static Game.Client.SourceDllMain;
global using static Game.Client.BeamDraw;

namespace Game.Client;

public ref struct C_BaseEntityIterator {
	public C_BaseEntityIterator() {
		Restart();
	}
	public void Restart() {
		CurBaseEntity = cl_entitylist.BaseEntities.First;
	}

	public C_BaseEntity? Next() {
		while (CurBaseEntity != null) {
			C_BaseEntity pRet = CurBaseEntity.Value;
			CurBaseEntity = CurBaseEntity.Next;

			if (!pRet.IsDormant())
				return pRet;
		}

		return null;
	}

	private LinkedListNode<C_BaseEntity>? CurBaseEntity;
}
