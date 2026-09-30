using Source;
using Source.Common;

using System.Runtime.CompilerServices;

namespace Game.Shared;

public static class HandleExts {
	static BaseEntityList? entityList;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IHandleEntity? Get(this BaseHandle handle) {
		return (entityList ??= Singleton<BaseEntityList>()).LookupEntity(handle);
	}
	public static T? Get<T>(this Handle<T> handle) where T : IHandleEntity {
		return (T?)(entityList ??= Singleton<BaseEntityList>()).LookupEntity(handle);
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

public readonly ref struct NetworkHandle<T> where T : IHandleEntity
{
	readonly ref Handle<T> Value;
	readonly INetworkStateChanged? Outer;
	readonly IFieldAccessor Field;

	public NetworkHandle(ref Handle<T> value, INetworkStateChanged? outer, IFieldAccessor field) {
		Value = ref value;
		Outer = outer;
		Field = field;
	}

	public T? Get() => Value.Get();
	public uint Index => Value.Index;
	public bool IsValid() => Value.IsValid();
	public int GetEntryIndex() => Value.GetEntryIndex();
	public int GetSerialNumber() => Value.GetSerialNumber();

	public Handle<T> Set(IHandleEntity? entity) => Set(new Handle<T>().Set(entity));

	public Handle<T> Set(Handle<T> other) {
		if (Value.Index != other.Index) {
			Outer?.NetworkStateChanged(Field);
			Value.Index = other.Index;
		}
		return Value;
	}

	public ref Handle<T> GetForModify() {
		Outer?.NetworkStateChanged(Field);
		return ref Value;
	}

	public static implicit operator Handle<T>(NetworkHandle<T> handle) => handle.Value;
	public static implicit operator BaseHandle(NetworkHandle<T> handle) => handle.Value;
}
