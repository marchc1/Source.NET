global using EHANDLE = Game.Shared.Handle<Game.Client.C_BaseEntity>;
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
