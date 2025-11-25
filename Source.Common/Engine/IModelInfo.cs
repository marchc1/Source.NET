namespace Source.Common.Engine;

public interface IModelInfo
{
	Model? GetModel(int modelIndex);
	int GetModelIndex(ReadOnlySpan<char> name);
	ReadOnlySpan<char> GetModelName(Model? model);
	MDLHandle_t GetCacheHandle(Model mdl);
	ModelType GetModelType(Model? model);
	VirtualModel? GetVirtualModel(StudioHeader self);
	Memory<byte> GetAnimBlock(StudioHeader studioHeader, int block);
	int GetAutoplayList(StudioHeader studioHdr, out Span<short> autoplayList);
}

public interface IModelInfoClient : IModelInfo
{
	int GetModelFrameCount(Model? pSprite);
	StudioHeader? GetStudiomodel(Model? model);
	bool IsTranslucentTwoPass(Model? model);
}
