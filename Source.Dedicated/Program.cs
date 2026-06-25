using Game.Server;

using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.MaterialSystem;
using Source.Common.Physics;
using Source.Common.SoundEmitterSystem;
using Source.Common.Utilities;
using Source.DataCache;
using Source.Engine;
using Source.FileSystem;
using Source.GUI.Controls;
using Source.MaterialSystem;
using Source.Physics;
using Source.SoundEmitterSystem;

using Steamworks;

namespace Source.Dedicated;

public class DedicatedExports(IDedicatedServerAPI engine) : IDedicatedExports
{
	readonly UserConsoleInput consoleUserInput = new();

	public void RunServer() {
		// Run 2 engine frames first to get the engine to load its resources.
		for (int i = 0; i < 2; i++) {
			DoRunVGUIFrame();
			if (!engine.RunFrame())
				return;
		}

		DoRunVGUIFrame(true);
		consoleUserInput.OnEnter += ConsoleUserInput_OnEnter;
		bool done = false;
		while(!done){
			if (!DoRunVGUIFrame())
				ProcessConsoleInput();

			if (!engine.RunFrame())
				done = true;

			UpdateStatus(false);
		}
	}

	private void ConsoleUserInput_OnEnter(ReadOnlySpan<char> text) {
		engine.AddConsoleText(text);
	}

	private int ProcessConsoleInput() {
		consoleUserInput.RunFrame();
		return 0;
	}

	private void UpdateStatus(bool force) {

	}

	private bool DoRunVGUIFrame(bool finished = false) {
		return false;
	}

	public void Sys_Printf(ReadOnlySpan<char> text) {
		Console.Write(text);
	}
}

public class Bootloader : IDisposable
{
	ICommandLine commandLine;
	IDedicatedServerAPI? engineAPI;

	string baseDir;
	bool isEditMode;
	bool isTextMode;

	public Bootloader() {
		commandLine = new CommandLine();
		commandLine.CreateCmdLine(Environment.CommandLine);
		GetBaseDirectory(commandLine, out baseDir);
		SteamAPI.Init();
		isTextMode = commandLine.CheckParm("-textmode");
	}
	public void Boot() {
		engineAPI = new EngineBuilder(commandLine)
			// These assemblies have no reference to them, so they must be manually loaded.
			.WithAssembly("Source.VTF")
			.WithStubMaterialSystem()
			// Base file system implementation
			.WithComponent<IFileSystem, BaseFileSystem>()
			// Physics
			.WithComponent<IPhysics, PhysicsInterface>()
			// Sound emitter
			.WithComponent<ISoundEmitterSystemBase, SoundEmitterSystemBase>()
			// Datacache impl
			.WithComponent<IDataCache, DataCache.DataCache>()
			.WithComponent<IDedicatedExports, DedicatedExports>()
			.WithComponent<MDLCache>()
			.WithResolvedComponent<IMDLCache, MDLCache>(x => x.GetRequiredService<MDLCache>())
			.WithResolvedComponent<IStudioDataCache, MDLCache>(x => x.GetRequiredService<MDLCache>())
			// Our game DLL'
			.WithGameDLL<ServerGameDLL>()
			// Let the engine builder take over and inject engine-specific dependencies
			.BuildServer();

		// Generate our startup information
		PreInit();

		// Start using this provider for the engine
		using ServiceLocatorScope locatorScope = new(engineAPI);
		if (engineAPI.ModInit(in info))
			engineAPI.ModShutdown();
	}

	static void GetBaseDirectory(ICommandLine cmdLine, out string baseDirectory) {
		baseDirectory = cmdLine.CheckParm("-basedir", out var values) ? values.FirstOrDefault() ?? AppContext.BaseDirectory : AppContext.BaseDirectory;
	}

	StartupInfo info = new();
	private void PreInit() {
		info.BaseDirectory = baseDir;
		info.InitialMod = DetermineInitialMod();
		info.InitialGame = DetermineInitialGame();
		info.TextMode = isTextMode;
	}

	const string defaultHalfLife2GameDirectory = "hl2";

	private string DetermineInitialMod() {
		return !isEditMode ? commandLine.ParmValue("-game", defaultHalfLife2GameDirectory) : throw new NotImplementedException("No editmode support");
	}

	private string DetermineInitialGame() {
		return !isEditMode ? commandLine.ParmValue("-game", defaultHalfLife2GameDirectory) : throw new NotImplementedException("No editmode support");
	}
	public void Dispose() {
		SteamAPI.Shutdown();
	}
}

internal class Program
{
	static void Main(string[] _) {
		Platform.Initialize();
		using (Bootloader bootloader = new())
			bootloader.Boot();
	}
}
