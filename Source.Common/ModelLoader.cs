using Source.Common.Engine;

namespace Source.Common;
public enum ModelLoaderFlags
{
	NotLoadedOrReferenced = 0,
	Loaded = 1 << 0,
	Server = 1 << 1,
	Client = 1 << 2,
	ClientDLL = 1 << 3,
	StaticProp = 1 << 4,
	DetailProp = 1 << 5,
	DynServer = 1 << 6,
	DynClient = 1 << 7,
	Dynamic = DynServer | DynClient,
	ReferenceMask = Server | Client | ClientDLL | StaticProp | DetailProp | Dynamic,
	TouchedByPreload = 1 << 15,
	LoadedByPreload = 1 << 16,
	TouchedMaterials = 1 << 17,
};
public interface IModelLoader {
	void Init();
	int GetCount();
	Model GetModelForIndex(int i);
	ReadOnlySpan<char> GetName(Model model);
	void GetExtraData(Model model);
	int GetModelFileSize(ReadOnlySpan<char> name);
	Model? GetModelForName(ReadOnlySpan<char> name, ModelLoaderFlags referenceType);
	Model? ReferenceModel(ReadOnlySpan<char> name, ModelLoaderFlags referenceType);
	void UnreferenceModel(Model model, ModelLoaderFlags referenceType);
	void UnreferenceAllModels(ModelLoaderFlags referenceType);
	void ResetModelServerCounts();
	void PurgeUnusedModels();
	void Shutdown();
}