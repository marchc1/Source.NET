namespace Source.Common.Engine;

public interface IModelInfo
{
	Model? GetModel(int modelIndex);
	int GetModelIndex(ReadOnlySpan<char> name);
	ReadOnlySpan<char> GetModelName(Model? model);
	MDLHandle_t GetCacheHandle(Model mdl);
	ModelType GetModelType(Model? model);
	VirtualModel? GetVirtualModel(StudioHeader self);
}

public interface IModelInfoClient : IModelInfo
{
	bool IsTranslucentTwoPass(Model? model);
}
