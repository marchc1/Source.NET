#if CLIENT_DLL || GAME_DLL
using Source.Common;
using Source.Common.GarrysMod.Lua;

using System.Runtime.InteropServices;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public class LuaEntityClass(string name, LuaType type, Action? initFn, string? derivedFrom) : LuaClass(name, type, initFn, derivedFrom)
{
	public override void MetaTableDerive() {
		if (DerivedFrom == null)
			return;

		ILuaObject? baseMeta = g_Lua!.GetMetaTableObject(DerivedFrom, -1);
		MetaTable.SetMember("MetaBaseClass", baseMeta);
		MetaTable.SetMember("__index", LuaEntity.__index_Entity);

		LuaObject gc = new();
		MetaTable.GetMember("__gc", gc);
		if (gc.isNil()) {
			baseMeta!.GetMember("__gc", gc);
			if (!gc.isNil())
				MetaTable.SetMember("__gc", gc);
		}
		gc.UnReference();

		LuaObject newindex = new();
		MetaTable.GetMember("__newindex", newindex);
		if (newindex.isNil()) {
			baseMeta!.GetMember("__newindex", newindex);
			if (!newindex.isNil())
				MetaTable.SetMember("__newindex", newindex);
		}
		newindex.UnReference();
	}
}

public static class LuaEntity
{
	public static readonly LuaClass LC_Entity = new("Entity", LuaType.Entity, null, null);

	static readonly LuaClassFunction Entity___index__Factory = LC_Entity.Add("__index", Entity____index);
	static readonly LuaClassFunction Entity___newindex__Factory = LC_Entity.Add("__newindex", Entity____newindex);

	public static readonly LuaEntityClass LC_NPC = new("NPC", LuaType.Entity, null, "Entity");
	public static readonly LuaEntityClass LC_Player = new("Player", LuaType.Entity, null, "Entity");
	public static readonly LuaEntityClass LC_Vehicle = new("Vehicle", LuaType.Entity, null, "Entity");
	public static readonly LuaEntityClass LC_Weapon = new("Weapon", LuaType.Entity, null, "Entity");

	static bool bWarning;

	public static BaseEntity? GetEntityFromHandle(uint handle) {
		if (handle == 0xFFFFFFFF)
			return null;
#if CLIENT_DLL
		return cl_entitylist.GetBaseEntityFromHandle(new BaseHandle(handle));
#else
		return gEntList.GetBaseEntity(new BaseHandle(handle));
#endif
	}

	public static BaseEntity? GetEntityFromUserData(nint data) {
		if (data == 0)
			return null;
		return GetEntityFromHandle((uint)Marshal.ReadInt32(data));
	}

	public static BaseEntity? UserGet(int stackPos) {
		if (!g_Lua!.IsType(stackPos, LuaType.Entity))
			return null;

		nint ud = g_Lua.GetUserdata(stackPos);
		if (ud == 0)
			return null;

		return GetEntityFromUserData(Marshal.ReadIntPtr(ud));
	}

	public static BaseEntity? Get_Entity(int stackPos, bool allowNull) {
		LuaType type = g_Lua!.GetType(stackPos);
		if (type != LuaType.Entity && (!allowNull || type != LuaType.Nil))
			g_Lua.TypeError("Entity", stackPos);

		BaseEntity? ent = UserGet(stackPos);
		if (ent == null && !allowNull)
			g_Lua.Error("Tried to use a NULL entity!");

		return ent;
	}

	public static void Push_Entity(BaseEntity? ent) {
		if (g_Lua == null || g_Lua.Global() == null)
			return;

		if (ent != null) {
			ent.PushEntity();
			return;
		}

		LuaObject NULL = new();
		g_Lua.Global().GetMember("NULL", NULL);
		if (NULL.GetType() != LuaType.Entity)
			Warning("Global 'NULL' is not an entity! It has been replaced somehow\n");
		NULL.Push();
		NULL.UnReference();
	}

	public static ILuaObject FindEntityMetaTable(BaseEntity? ent) {
		if (ent == null)
			return LC_Entity.MetaTable;
		if (ent.IsPlayer())
			return LC_Player.MetaTable;
		if (ent.IsNPC())
			return LC_NPC.MetaTable;
		if (ent.IsVehicle())
			return LC_Vehicle.MetaTable;
		return ent.Lua_GetLuaClass().MetaTable;
	}

	public static void MakeLuaNULLEntity() {
		LuaObject NULL = new();
		g_Lua!.PushUserType(0, LuaType.Entity);
		LC_Entity.MetaTable.Push();
		g_Lua.SetMetaTable(-2);
		NULL.SetFromStack(-1);
		g_Lua.Pop(1);
		g_Lua.Global().SetMember("NULL", NULL);
		NULL.UnReference();
	}

	static int EntityBaseIndex() {
		if (LC_Entity.MetaTable.PushMemberFast(2))
			return 1;

		BaseEntity? ent = Get_Entity(1, true);
		if (ent != null) {
			ILuaObject? table = ent.GetLuaTable();
			if (table != null && table.PushMemberFast(2))
				return 1;

			if (ent.IsWeapon()) {
				string? key = g_Lua!.CheckString(2);
				if (key != null && key.Equals("owner", StringComparison.OrdinalIgnoreCase)) {
					Push_Entity(((BaseCombatWeapon)ent).GetOwner());
					return 1;
				}
			}
		}

		string? str = g_Lua!.CheckString(2);
		if (str != null && str == "Entity") {
			if (!bWarning) {
				Warning($"[Deprecated] Entity.Entity [{g_Lua.GetCurrentLocation()}]\n");
				bWarning = true;
			}
			Push_Entity(ent);
			return 1;
		}

		return 0;
	}

	public static int __index_Entity(ILuaInterface lua) {
		if (lua.FindOnObjectsMetaTable(1, 2))
			return 1;
		return EntityBaseIndex();
	}

	static int Entity____index(ILuaInterface lua) => EntityBaseIndex();

	static int Entity____newindex(ILuaInterface lua) {
		BaseEntity? ent = Get_Entity(1, true);
		if (ent == null)
			return 0;

		ILuaObject? table = ent.GetLuaTable();
		if (table == null || !table.isTable())
			return 0;

		table.SetMemberFast(2, 3);
		if (g_Lua!.GetType(2) != LuaType.String || lua.GetString(2) != "CalcAbsolutePosition")
			return 0;

		if (lua.GetType(3) == LuaType.Function)
			ent.LuaCalcAbsolutePosition.SetFromStack(3);
		else
			ent.LuaCalcAbsolutePosition.UnReference();
		return 0;
	}
}
#endif
