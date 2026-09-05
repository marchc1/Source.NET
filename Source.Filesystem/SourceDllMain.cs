global using static Source.Filesystem.SourceDllMain;
using Source.Common.Filesystem;

namespace Source.Filesystem;

[EngineComponent]
public static class SourceDllMain
{
	[Dependency] public static IFileSystem g_FullFileSystem = null!;	
}
