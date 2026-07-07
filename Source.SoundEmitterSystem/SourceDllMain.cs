global using static Source.SoundEmitterSystem.SourceDllMain;

using Source.Common.Filesystem;

namespace Source.SoundEmitterSystem;

[EngineComponent]
public static class SourceDllMain
{
	[Dependency] public static IFileSystem filesystem { get; set; } = null!;
}
