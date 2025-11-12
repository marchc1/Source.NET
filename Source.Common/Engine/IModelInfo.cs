namespace Source.Common.Engine;

public interface IModelInfo
{
	Model? GetModel(int modelIndex);
	int GetModelIndex(ReadOnlySpan<char> name);
}

public interface IModelInfoClient : IModelInfo {

}
