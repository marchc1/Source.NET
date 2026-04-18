namespace Source.Common;

public interface IClientEntityList {
	IClientNetworkable? GetClientNetworkable(int entnNum);
	IClientNetworkable? GetClientNetworkableFromHandle(in BaseHandle ent);
	IClientUnknown? GetClientUnknownFromHandle(in BaseHandle ent);
	IClientEntity? GetClientEntity(int entNum);
	IClientEntity? GetClientEntityFromHandle(in BaseHandle ent);
	IClientThinkable? GetClientThinkableFromHandle(in BaseHandle ent);
	int NumberOfEntities(bool includeNonNetworkable);
	int GetHighestEntityIndex();
	void SetMaxEntities(int maxEnts);
	int GetMaxEntities();
	IHandleEntity? LookupEntity(in BaseHandle index);
	BaseHandle InvalidHandle();
}
