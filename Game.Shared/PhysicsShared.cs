global using static Game.Shared.PhysicsSharedGlobals;

using Source;
using Source.Common;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Physics;
namespace Game.Shared;

[EngineComponent]
public static class PhysicsSharedGlobals
{
	[Dependency] public static IPhysics physics { get; set; } = null!;
	[Dependency] public static IPhysicsCollision physcollision { get; set; } = null!;
	[Dependency] public static IPhysicsSurfaceProps physprops { get; set; } = null!;


	public static IPhysicsObject g_PhysWorldObject = null!;
	public static IPhysicsEnvironment physenv = null!;
#if PORTAL
	public static IPhysicsEnvironment physenv_main = null!;
#endif
	public static IPhysicsObjectPairHash g_EntityCollisionHash = null!;

	const string SURFACEPROP_MANIFEST_FILE = "scripts/surfaceproperties_manifest.txt";

	public static readonly ObjectParams g_PhysDefaultObjectParams = new() {
		MassCenterOverrideFn = null,
		Mass = 1.0f,
		Inertia = 1.0f,
		Damping = 0.1f,
		RotDamping = 0.1f,
		RotInertiaLimit = 0.05f,
		Name = "DEFAULT",
		GameData = null,
		Volume = 0f,
		DragCoefficient = 1.0f,
		EnableCollisions = true
	};

	public static void AddSurfacepropFile(ReadOnlySpan<char> filename, IPhysicsSurfaceProps props, IFileSystem fileSystem) {
		using IFileHandle? file = fileSystem.Open(filename, FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");
		if(file != null){
			int len = (int)file.Stream.Length;
			Span<char> buffer = stackalloc char[len];
			using StreamReader reader = new(file.Stream);
			reader.Read(buffer);
			props.ParseSurfaceData(filename, buffer);
		}
	}
	public static void PhysParseSurfaceData(IPhysicsSurfaceProps props, IFileSystem fileSystem){
		KeyValues manifest = new KeyValues(SURFACEPROP_MANIFEST_FILE);
		if (manifest.LoadFromFile(fileSystem, SURFACEPROP_MANIFEST_FILE, "GAME")) {
			for (KeyValues? sub = manifest.GetFirstSubKey(); sub != null; sub = sub.GetNextKey()) {
				if (0 == stricmp(sub.Name, "file")) {
					AddSurfacepropFile(sub.GetString(), props, fileSystem);
					continue;
				}

				Warning($"surfaceprops::Init:  Manifest '{SURFACEPROP_MANIFEST_FILE}' with bogus file type '{sub.Name}', expecting 'file'\n");
			}
		}
		else 
			Error($"Unable to load manifest file '{SURFACEPROP_MANIFEST_FILE}'\n");
	}

	static IGameSystem physicsGameSystem = null!;
	public static void SetPhysicsGameSystem(IGameSystem system) => physicsGameSystem = system;
	public static IGameSystem PhysicsGameSystem() => physicsGameSystem;

#if CLIENT_DLL || GAME_DLL
	public static IPhysicsObject? PhysCreateWorld_Shared(BaseEntity world, VCollide? worldCollide, in ObjectParams defaultParams) {
		Solid solid;
		Fluid fluid;

		if (physenv == null) return null;
		if (worldCollide == null) return null;

		int surfaceData = (int)physprops.GetSurfaceIndex("default");

		ObjectParams oparams = defaultParams;
		oparams.GameData = world;
		oparams.Name = "world";

		IPhysicsObject? worldPhysics = physenv.CreatePolyObjectStatic(worldCollide.Solids![0]!, surfaceData, vec3_origin, vec3_angle, ref oparams);

		return null;
	}
#endif
}
