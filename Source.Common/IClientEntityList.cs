namespace Source.Common;

public interface IClientEntityList {
	IClientNetworkable? GetClientNetworkable(int entnNum);
	IClientNetworkable? GetClientNetworkableFromHandle(BaseHandle ent);
	IClientUnknown? GetClientUnknownFromHandle(BaseHandle ent);
	IClientEntity? GetClientEntity(int entNum);
	IClientEntity? GetClientEntityFromHandle(BaseHandle ent);
	IClientThinkable? GetClientThinkableFromHandle(BaseHandle ent);
	int NumberOfEntities(bool includeNonNetworkable);
	int GetHighestEntityIndex();
	void SetMaxEntities(int maxEnts);
	int GetMaxEntities();
	IHandleEntity? LookupEntity(BaseHandle index);
	BaseHandle? InvalidHandle();
}
