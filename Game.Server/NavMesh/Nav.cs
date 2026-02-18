global using NavPlace = System.UInt32;

using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;
namespace Game.Server.NavMesh;

public static class Nav
{
	const float GenerationStepSize = 25.0f;     // (30) was 20, but bots can't fit always fit
	const float JumpHeight = 41.8f;         // if delta Z is less than this, we can jump up on it
	const float JumpCrouchHeight = 64.0f;     // (48) if delta Z is less than or equal to this, we can jumpcrouch up on it
	const float StepHeight = 18.0f;         // if delta Z is greater than this, we have to jump to get up
	const float DeathDrop = 400.0f;         // (300) distance at which we will die if we fall - should be about 600, and pay attention to fall damage during pathfind
	const float ClimbUpHeight = 200.0f;       // height to check for climbing up
	const float CliffHeight = 300.0f;       // height which we consider a significant cliff which we would not want to fall off of

	const int HalfHumanWidth = 16;
	const float HalfHumanHeight = 35.5f;
	const float HumanHeight = 71.0f;
	const float HumanEyeHeight = 62.0f;
	const float HumanCrouchHeight = 55.0f;
	const float HumanCrouchEyeHeight = 37.0f;
	public const uint NavMagicNumber = 0xFEEDFACE;       // to help identify nav files

	public const uint UndefinedPlace = 0;
	public const uint AnyPlace = 0xFFFF;

	public static NavDirType OppositeDirection(NavDirType dir) => dir switch {
		NavDirType.North => NavDirType.South,
		NavDirType.South => NavDirType.North,
		NavDirType.East => NavDirType.West,
		NavDirType.West => NavDirType.East,
		_ => NavDirType.North
	};

	public static NavDirType DirectionLeft(NavDirType dir) => dir switch {
		NavDirType.North => NavDirType.West,
		NavDirType.South => NavDirType.East,
		NavDirType.East => NavDirType.North,
		NavDirType.West => NavDirType.South,
		_ => NavDirType.North
	};

	public static NavDirType DirectionRight(NavDirType dir) => dir switch {
		NavDirType.North => NavDirType.East,
		NavDirType.South => NavDirType.West,
		NavDirType.East => NavDirType.South,
		NavDirType.West => NavDirType.North,
		_ => NavDirType.North
	};

	public static void AddDirectionVector(ref Vector3 v, NavDirType dir, float amount) {
		switch (dir) {
			case NavDirType.North: v.Y -= amount; break;
			case NavDirType.South: v.Y += amount; break;
			case NavDirType.East: v.X += amount; break;
			case NavDirType.West: v.X -= amount; break;
		}
	}

	public static float DirectionToAngle(NavDirType dir) => dir switch {
		NavDirType.North => 270f,
		NavDirType.South => 90f,
		NavDirType.East => 0f,
		NavDirType.West => 180f,
		_ => 0f
	};

	public static NavDirType AngleToDirection(float angle) {
		angle %= 360f;

		if (angle < 0f) angle += 360f;

		if (angle < 45f || angle > 315f) return NavDirType.East;
		if (angle < 135f) return NavDirType.South;
		if (angle < 225f) return NavDirType.West;

		return NavDirType.North;
	}

	public static Vector2 DirectionToVector2D(NavDirType dir) => dir switch {
		NavDirType.North => new Vector2(0f, -1f),
		NavDirType.South => new Vector2(0f, 1f),
		NavDirType.East => new Vector2(1f, 0f),
		NavDirType.West => new Vector2(-1f, 0f),
		_ => Vector2.Zero
	};

	public static Vector2 CornerToVector2D(NavCornerType dir) {
		Vector2 v = dir switch {
			NavCornerType.NorthWeset => new Vector2(-1f, -1f),
			NavCornerType.NorthEast => new Vector2(1f, -1f),
			NavCornerType.SouthEast => new Vector2(1f, 1f),
			NavCornerType.SouthWest => new Vector2(-1f, 1f),
			_ => Vector2.Zero
		};

		return Vector2.Normalize(v);
	}

	public static void GetCornerTypesInDirection(
			NavDirType dir,
			out NavCornerType first,
			out NavCornerType second) {
		switch (dir) {
			default:
			case NavDirType.North:
				first = NavCornerType.NorthWeset;
				second = NavCornerType.NorthEast;
				break;
			case NavDirType.South:
				first = NavCornerType.SouthWest;
				second = NavCornerType.SouthEast;
				break;
			case NavDirType.East:
				first = NavCornerType.NorthEast;
				second = NavCornerType.SouthEast;
				break;
			case NavDirType.West:
				first = NavCornerType.NorthWeset;
				second = NavCornerType.SouthWest;
				break;
		}
	}

	public static float RoundToUnits(float val, float unit) {
		val += val < 0f ? -unit * 0.5f : unit * 0.5f;
		return unit * ((int)val / (int)unit);
	}

	public static bool IsEntityWalkable(BaseEntity entity, WalkThruFlags flags) {
		if (entity.ClassMatches("worldspawn"))
			return false;

		if (entity.ClassMatches("player"))
			return false;

		if (entity.ClassMatches("func_door*"))
			return flags.HasFlag(WalkThruFlags.FuncDoors);

		if (entity.ClassMatches("prop_door*"))
			return flags.HasFlag(WalkThruFlags.PropDoors);

		if (entity.ClassMatches("func_brush") && entity is FuncBrush brush) {
			// TODO: When func_brush is implemented

			// switch (brush.Solidity) {
			// 	case BrushSolidity.Always:
			// 		return false;
			// 	case BrushSolidity.Never:
			// 		return true;
			// 	case BrushSolidity.Toggle:
			// 		return flags.HasFlag(WalkThruFlags.ToggleBrushes);
			// }
		}

		if (entity.ClassMatches("func_breakable") && entity.Health > 0 && entity.TakeDamage == 2) // DAMAGE_YES
			return flags.HasFlag(WalkThruFlags.Breakables);

		if (entity.ClassMatches("func_breakable_surf") && entity.TakeDamage == 2) // DAMAGE_YES
			return flags.HasFlag(WalkThruFlags.Breakables);

		if (entity.ClassMatches("func_playerinfected_clip"))
			return true;

		// todo
		// if (nav_solid_props.GetBool() && entity.ClassMatches("prop_*"))
		// 	return true;

		return false;
	}
}

[Flags]
public enum WalkThruFlags : uint
{
	PropDoors = 0x01,
	FuncDoors = 0x02,
	Doors = PropDoors | FuncDoors,
	Breakables = 0x04,
	ToggleBrushes = 0x08,
	Everything = Doors | Breakables | ToggleBrushes
}

public enum NavErrorType
{
	Ok,
	CantAccessFile,
	InvalidFile,
	BadFileVersion,
	FileOutOfDate,
	CorruptData,
	OutOfMemory
}

[Flags]
public enum NavAttributeType : uint
{
	/// <summary>Invalid attribute.</summary>
	Invalid = 0,

	/// <summary>Must crouch to use this node or area.</summary>
	Crouch = 0x00000001,

	/// <summary>Must jump to traverse this area. Only used during generation.</summary>
	Jump = 0x00000002,

	/// <summary>Do not adjust for obstacles. Move precisely along area.</summary>
	Precice = 0x00000004,

	/// <summary>Inhibit discontinuity jumping.</summary>
	NoJump = 0x00000008,

	/// <summary>Must stop when entering this area.</summary>
	Stop = 0x00000010,

	/// <summary>Must run to traverse this area.</summary>
	Run = 0x00000020,

	/// <summary>Must walk to traverse this area.</summary>
	Walk = 0x00000040,

	/// <summary>Avoid this area unless alternatives are too dangerous.</summary>
	Avoid = 0x00000080,

	/// <summary>Area may become blocked and should be periodically checked.</summary>
	Transient = 0x00000100,

	/// <summary>Area should not be considered for hiding spot generation.</summary>
	DontHide = 0x00000200,

	/// <summary>Bots hiding in this area should stand.</summary>
	Stand = 0x00000400,

	/// <summary>Hostages should not use this area.</summary>
	NoHostages = 0x00000800,

	/// <summary>Represents stairs. Do not climb or jump them, just walk up.</summary>
	Stairs = 0x00001000,

	/// <summary>Do not merge this area with adjacent areas.</summary>
	NoMerge = 0x00002000,

	/// <summary>This area is the climb point on top of an obstacle.</summary>
	ObstacleTop = 0x00004000,

	/// <summary>Adjacent to a drop of at least CliffHeight.</summary>
	Cliff = 0x00008000,

	/// <summary>Custom app specific bits may start here.</summary>
	FirstCustom = 0x00010000,

	/// <summary>Custom app specific bits must not exceed this value.</summary>
	LastCustom = 0x04000000,

	/// <summary>Area has designer specified cost controlled by func_nav_cost entities.</summary>
	FuncCost = 0x20000000,

	/// <summary>Area is in an elevator path.</summary>
	HasElevator = 0x40000000,

	/// <summary>Area is blocked by nav blocker.</summary>
	NavBlocker = 0x80000000
}

[Flags]
public enum NavDirType
{
	North = 0,
	East = 1,
	South = 2,
	West = 3,
	NumDirections
}

[Flags]
public enum NavTraverseType
{
	// NOTE: First 4 directions MUST match NavDirType
	North = 0,
	East,
	South,
	West,
	LadderUp,
	LadderDown,
	Jump,
	ElevatorUp,
	ElevatorDown,
	NumTraverseTypes
};

[Flags]
public enum NavCornerType
{
	NorthWeset = 0,
	NorthEast = 1,
	SouthEast = 2,
	SouthWest = 3,
	NumCorners
};

[Flags]
public enum NavRelativeDirType
{
	Forward = 0,
	Right,
	Backward,
	Left,
	Up,
	Down,
	NumRelativeDirections
};

public struct Extent
{
	public Vector3 Lo;
	public Vector3 Hi;

	public void Init() {
		Lo.Init();
		Hi.Init();
	}

	public void Init(BaseEntity entity) {
		// entity.CollisionProp().WorldSpaceSurroundingBounds(out Lo, out Hi);
	}

	public float SizeX() => Hi.X - Lo.X;
	public float SizeY() => Hi.Y - Lo.Y;
	public float SizeZ() => Hi.Z - Lo.Z;
	public float Area() => SizeX() * SizeY();

	public void Encompass(Vector3 pos) {
		if (pos.X < Lo.X) Lo.X = pos.X;
		else if (pos.X > Hi.X) Hi.X = pos.X;

		if (pos.Y < Lo.Y) Lo.Y = pos.Y;
		else if (pos.Y > Hi.Y) Hi.Y = pos.Y;

		if (pos.Z < Lo.Z) Lo.Z = pos.Z;
		else if (pos.Z > Hi.Z) Hi.Z = pos.Z;
	}

	public void Encompass(Extent extent) {
		Encompass(extent.Lo);
		Encompass(extent.Hi);
	}

	public bool Contains(Vector3 pos) =>
			pos.X >= Lo.X && pos.X <= Hi.X &&
			pos.Y >= Lo.Y && pos.Y <= Hi.Y &&
			pos.Z >= Lo.Z && pos.Z <= Hi.Z;

	public bool IsOverlapping(Extent other) =>
			Lo.X <= other.Hi.X && Hi.X >= other.Lo.X &&
			Lo.Y <= other.Hi.Y && Hi.Y >= other.Lo.Y &&
			Lo.Z <= other.Hi.Z && Hi.Z >= other.Lo.Z;

	public bool IsEncompassing(Extent other, float tolerance = 0.0f) =>
			Lo.X <= other.Lo.X + tolerance && Hi.X >= other.Hi.X - tolerance &&
			Lo.Y <= other.Lo.Y + tolerance && Hi.Y >= other.Hi.Y - tolerance &&
			Lo.Z <= other.Lo.Z + tolerance && Hi.Z >= other.Hi.Z - tolerance;
}

public struct Ray
{
	public Vector3 From, To;
}

public class TraceFilterWalkableEntities //: TraceFilterNoNPCsOrPlayer
{
	readonly WalkThruFlags flags;

	public TraceFilterWalkableEntities(IHandleEntity passEntity, int collisionGroup, WalkThruFlags flags) /*: base(passEntity, collisionGroup)*/ => this.flags = flags;

	public /*override*/ bool ShouldHitEntity(IHandleEntity serverEntity, int contentsMask) {
		// if (base.ShouldHitEntity(serverEntity, contentsMask)) {
		// var entity = EntityFromEntityHandle(serverEntity);
		// return !Nav.IsEntityWalkable(entity, flags);
		// }

		return false;
	}
}
