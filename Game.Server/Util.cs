global using static Game.Util_Globals;

using CommunityToolkit.HighPerformance;

using Game.Server;
using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game;


public static class ClassMap
{
	static bool Initialized;
	public static void Init() {
		if (Initialized) return;
		Initialized = true;
		// Here is where we start initializing things based on LinkEntityToClassAttribute's
		var typePairs = Assembly.GetExecutingAssembly().GetTypesWithAttributeMulti<LinkEntityToClassAttribute>();
		foreach (var type in typePairs) {
			var attr = type.Value;

			var factoryType = typeof(CEntityFactory<>).MakeGenericType(type.Key);
			Activator.CreateInstance(factoryType, [type.Value.LocalName]);
		}
	}
}

public interface IEntityFactory
{
	IServerNetworkable? Create(ReadOnlySpan<char> className);
	void Destroy(IServerNetworkable networkable);
}

public interface IEntityFactoryDictionary
{
	void InstallFactory(IEntityFactory factory, ReadOnlySpan<char> classname);
	IServerNetworkable? Create(ReadOnlySpan<char> classname);
	void Destroy(ReadOnlySpan<char> className, IServerNetworkable networkable);
	IEntityFactory? FindFactory(ReadOnlySpan<char> className);
	ReadOnlySpan<char> GetCannonicalName(ReadOnlySpan<char> className);
}

public class CEntityFactory<T> : IEntityFactory where T : BaseEntity, new()
{
	public CEntityFactory(string classname) {
		EntityFactoryDictionary().InstallFactory(this, classname);
	}
	static T _CreateEntityTemplate(ReadOnlySpan<char> classname) {
		T newEnt = new T();
		newEnt.PostConstructor(classname);
		return newEnt;
	}
	public IServerNetworkable Create(ReadOnlySpan<char> className) {
		T ent = _CreateEntityTemplate(className);
		return ent.NetworkProp();
	}

	public void Destroy(IServerNetworkable networkable) {
		if (networkable != null)
			networkable.Release();
	}
}

public class CEntityFactoryDictionary : IEntityFactoryDictionary
{
	public IServerNetworkable? Create(ReadOnlySpan<char> className) {
		className = className.SliceNullTerminatedString();
		IEntityFactory? factory = FindFactory(className);
		if (factory == null) {
			Warning($"Attempted to create unknown entity type {className}!\n");
			return null;
		}
		return factory.Create(className);
	}

	public void Destroy(ReadOnlySpan<char> className, IServerNetworkable networkable) {
		className = className.SliceNullTerminatedString();

		IEntityFactory? factory = FindFactory(className);
		if (factory == null) {
			Warning($"Attempted to destroy unknown entity type {factory}!\n");
			return;
		}

		factory.Destroy(networkable);
	}

	public IEntityFactory? FindFactory(ReadOnlySpan<char> className) {
		if (!Factories.TryGetValue(className.Hash(), out IEntityFactory? factory))
			return null;

		return factory;
	}

	public ReadOnlySpan<char> GetCannonicalName(ReadOnlySpan<char> className) {
		return className; // ??
	}

	public void InstallFactory(IEntityFactory factory, ReadOnlySpan<char> classname) {
		Factories[classname.Hash()] = factory;
	}

	public readonly Dictionary<ulong, IEntityFactory> Factories = [];
}


public static partial class Util_Globals
{
	static readonly CEntityFactoryDictionary s_EntityFactory = new();
	public static IEntityFactoryDictionary EntityFactoryDictionary() => s_EntityFactory;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ENTINDEX(Edict? edict) {
		int result = edict != null ? edict.EdictIndex : 0;
		Assert(result == engine.IndexOfEdict(edict));
		return result;
	}

	public static bool FStrEq(ReadOnlySpan<char> sz1, ReadOnlySpan<char> sz2)
		=> Unsafe.AreSame(in sz1.DangerousGetReference(), in sz2.DangerousGetReference()) || stricmp(sz1, sz2) == 0;
}
public static partial class Util
{
	public static void TransmitShakeEvent(BasePlayer player, float localAmplitude, float frequency, TimeUnit_t duration, ShakeCommand command) {
		if ((localAmplitude > 0) || (command == ShakeCommand.Stop)) {
			if (command == ShakeCommand.Stop)
				localAmplitude = 0;

			SingleUserRecipientFilter user = new(player);
			user.MakeReliable();
			UserMessageBegin(user, "Shake");
			WRITE_BYTE((byte)command);          // shake command (SHAKE_START, STOP, FREQUENCY, AMPLITUDE)
			WRITE_FLOAT(localAmplitude);        // shake magnitude/amplitude
			WRITE_FLOAT(frequency);             // shake noise frequency
			WRITE_FLOAT((float)duration);       // shake lasts this long
			MessageEnd();
		}
	}

	public static void PrecacheOther(ReadOnlySpan<char> className, ReadOnlySpan<char> modelName = default) => throw new NotImplementedException();

	public static void ClientPrintFilter<Filter>(scoped in Filter filter, HudPrint dest, ReadOnlySpan<char> msgName, ReadOnlySpan<char> param1 = default, ReadOnlySpan<char> param2 = default, ReadOnlySpan<char> param3 = default, ReadOnlySpan<char> param4 = default) where Filter : IRecipientFilter {
		UserMessageBegin(filter, "TextMsg");
		WRITE_BYTE((byte)dest);
		WRITE_STRING(msgName);

		if (!param1.IsEmpty)
			WRITE_STRING(param1);
		else
			WRITE_STRING("");

		if (!param2.IsEmpty)
			WRITE_STRING(param2);
		else
			WRITE_STRING("");

		if (!param3.IsEmpty)
			WRITE_STRING(param3);
		else
			WRITE_STRING("");

		if (!param4.IsEmpty)
			WRITE_STRING(param4);
		else
			WRITE_STRING("");

		MessageEnd();
	}
	public static Edict? INDEXENT(int edictNum) => engine.PEntityOfEntIndex(edictNum);

	public static BasePlayer? PlayerByIndex(int playerIndex) {
		BasePlayer? player = null;

		if (playerIndex > 0 && playerIndex <= gpGlobals.MaxClients) {
			Edict? playerEdict = INDEXENT(playerIndex);
			if (playerEdict != null && !playerEdict.IsFree())
				player = (BasePlayer?)BaseEntity.GetContainingEntity(playerEdict);
		}

		return player;
	}

	public static BasePlayer? GetListenServerHost() {
		if (engine.IsDedicatedServer()) {
			Assert("UTIL_GetListenServerHost");
			Warning("UTIL_GetListenServerHost() called from a dedicated server or single-player game.\n");
			return null;
		}

		return PlayerByIndex(1);
	}

	public static bool IsCommandIssuedByServerAdmin() {
		int issuingPlayerIndex = GetCommandClientIndex();

		if (engine.IsDedicatedServer() && issuingPlayerIndex > 0)
			return false;

		return issuingPlayerIndex < 1;
	}

	public static void ClientPrintAll(HudPrint dest, ReadOnlySpan<char> msgName, ReadOnlySpan<char> param1 = default, ReadOnlySpan<char> param2 = default, ReadOnlySpan<char> param3 = default, ReadOnlySpan<char> param4 = default) {
		ReliableBroadcastRecipientFilter filter = new();
		ClientPrintFilter(filter, dest, msgName, param1, param2, param3, param4);
	}

	public static int DispatchSpawn(BaseEntity? entity) {
		Console.WriteLine($"Dispatching spawn for {entity}");
		if (entity != null) {
			// keep a smart pointer that will know if the object gets deleted
			EHANDLE pEntSafe = new();
			pEntSafe.Set(entity);

			// TODO: GetBaseAnimating / SetBoneCacheFlags(BCF_IS_IN_SPAWN)
			entity.Spawn();
			// TODO: ClearBoneCacheFlags(BCF_IS_IN_SPAWN)

			// Try to get the pointer again, in case the spawn function deleted the entity.
			if (!pEntSafe.IsValid() || entity.IsMarkedForDeletion())
				return -1;

			// TODO
			// if (entity.m_iGlobalname != NULL_STRING) {
			// 	int globalIndex = GlobalEntity_GetIndex(entity.m_iGlobalname);
			// 	if (globalIndex >= 0) {
			// 		if (GlobalEntity_GetState(globalIndex) == GLOBAL_DEAD) {
			// 			entity.Remove();
			// 			return -1;
			// 		} else if (!FStrEq(STRING(gpGlobals.mapname), GlobalEntity_GetMap(globalIndex))) {
			// 			entity.MakeDormant();
			// 		}
			// 	} else {
			// 		GlobalEntity_Add(entity.m_iGlobalname, gpGlobals.mapname, GLOBAL_ON);
			// 	}
			// }

			gEntList.NotifySpawn(entity);
		}

		return 0;
	}

	public static void Remove(BaseEntity entity) {
		throw new NotImplementedException();
	}

	public static void Remove(IServerNetworkable entity) {
		throw new NotImplementedException();
	}

	public static int GetCommandClientIndex() => ServerGameClients.CommandClientIndex;

	public static BasePlayer? GetCommandClient() {
		int id = GetCommandClientIndex();
		if (id > 0)
			return PlayerByIndex(id)!;

		return null;
	}
}
