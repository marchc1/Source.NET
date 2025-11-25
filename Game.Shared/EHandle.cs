using Source.Common;

using System.Runtime.CompilerServices;

namespace Game.Shared;

public static class HandleExts {
	static IClientEntityList? entityList;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IHandleEntity? Get(this BaseHandle handle) {
		return (entityList ??= Singleton<IClientEntityList>()).LookupEntity(handle);
	}
	public static T? Get<T>(this Handle<T> handle) where T : IHandleEntity {
		return (T?)(entityList ??= Singleton<IClientEntityList>()).LookupEntity(handle);
	}

	/// <summary>
	/// Because C# doesn't have set operators, you use this to set an EHANDLE's value to another value.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="handle"></param>
	/// <param name="entity"></param>
	public static void Set<T>(this Handle<T> handle, Handle<T> entity) where T : IHandleEntity {
		handle.Index = entity.Index;
	}
}

public class Handle<T> : BaseHandle;
