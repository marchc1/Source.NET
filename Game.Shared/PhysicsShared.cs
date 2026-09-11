global using static Game.Shared.PhysicsSharedGlobals;

using Source;
using Source.Common;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Game.Shared;

[EngineComponent]
public static class PhysicsSharedGlobals
{
	[Dependency] public static IPhysics physics { get; set; } = null!;
	[Dependency] public static IPhysicsCollision physcollision { get; set; } = null!;
	[Dependency] public static IPhysicsSurfaceProps physprops { get; set; } = null!;


	public static IPhysicsObject? g_PhysWorldObject = null!;
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
		if (file != null) {
			int len = (int)file.Stream.Length;
			Span<char> buffer = stackalloc char[len];
			using StreamReader reader = new(file.Stream);
			reader.Read(buffer);
			props.ParseSurfaceData(filename, buffer);
		}
	}
	public static void PhysParseSurfaceData(IPhysicsSurfaceProps props, IFileSystem fileSystem) {
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
	public static void PhysDestroyObject(IPhysicsObject? obj, BaseEntity entity) {
		//g_pPhysSaveRestoreManager->ForgetModel(pObject);

		obj?.SetGameData(null);
		g_EntityCollisionHash.RemoveAllPairsForObject(obj);
		if (entity != null && entity.IsMarkedForDeletion())
			g_EntityCollisionHash.RemoveAllPairsForObject(entity);

		physenv?.DestroyObject(obj);
	}

	public static bool PhysModelParseSolidByIndex(ref Solid solid, BaseEntity entity, VCollide? collide, int solidIndex) {
		if (collide == null || collide.KeyValues == null)
			return false;

		bool parsed = false;

		solid = default;
		solid.Params = g_PhysDefaultObjectParams;

		IVPhysicsKeyParser parse = physcollision.VPhysicsKeyParserCreate(collide.KeyValues);
		while (!parse.Finished()) {
			ReadOnlySpan<char> block = parse.GetCurrentBlockName();
			if (strcmpi(block, "solid") == 0) {
				Solid tmpSolid = default;
				tmpSolid.Params = g_PhysDefaultObjectParams;

				parse.ParseSolid(ref tmpSolid, null);

				if (solidIndex < 0 || tmpSolid.Index == solidIndex) {
					parsed = true;
					solid = tmpSolid;
					break;
				}
			}
			else
				parse.SkipBlock();
		}
		physcollision.VPhysicsKeyParserDestroy(parse);

		// collisions are off by default
		solid.Params.EnableCollisions = true;

		solid.Params.GameData = entity;
		solid.Params.Name = new string(((ReadOnlySpan<char>)entity.GetModelName()).SliceNullTerminatedString());
		return parsed;
	}

	public static IPhysicsObject? PhysModelCreate(BaseEntity entity, int modelIndex, in Vector3 origin, in QAngle angles, ref Solid solid) {
		if (physenv == null)
			return null;

		VCollide? collide = modelinfo.GetVCollide(modelIndex);
		if (collide == null || collide.SolidCount == 0 || collide.Solids == null)
			return null;

		if (Unsafe.IsNullRef(ref solid)) {
			Solid tmpSolid = default;
			if (!PhysModelParseSolidByIndex(ref tmpSolid, entity, collide, -1))
				return null;
			return PhysModelCreateInternal(entity, collide, in origin, in angles, ref tmpSolid);
		}

		return PhysModelCreateInternal(entity, collide, in origin, in angles, ref solid);
	}

	static IPhysicsObject? PhysModelCreateInternal(BaseEntity entity, VCollide collide, in Vector3 origin, in QAngle angles, ref Solid solid) {
		int surfaceProp = -1;
		ReadOnlySpan<char> surfacePropName = ((ReadOnlySpan<char>)solid.SurfaceProp).SliceNullTerminatedString();
		if (!surfacePropName.IsEmpty)
			surfaceProp = (int)physprops.GetSurfaceIndex(surfacePropName);

		IPhysicsObject? obj = physenv.CreatePolyObject(collide.Solids![solid.Index]!, surfaceProp, in origin, in angles, ref solid.Params);
		return obj;
	}

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
