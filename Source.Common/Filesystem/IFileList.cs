namespace Source.Common.Filesystem;

public interface IFileList
{
	bool IsFileInList(ReadOnlySpan<char> fileName);
	void Release();
}
