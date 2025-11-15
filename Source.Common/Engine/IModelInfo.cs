namespace Source.Common.Engine;

public interface IModelInfo
{
	Model? GetModel(int modelIndex);
	int GetModelIndex(ReadOnlySpan<char> name);
	ReadOnlySpan<char> GetModelName(Model? model);
}

public interface IModelInfoClient : IModelInfo
{
	ModelType GetModelType(Model? model);
	bool IsTranslucentTwoPass(Model? model);
}