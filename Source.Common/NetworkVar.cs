namespace Source.Common;

public delegate void NetworkVarChanged<T>(ref T newValue);

public interface INetworkStateChanged
{
	void NetworkStateChanged(IFieldAccessor accessor);
}

public readonly ref struct NetworkArray<TArray, Type> where TArray : struct
{
	readonly Span<Type> Value;
	readonly INetworkStateChanged? Outer;
	readonly DynamicArrayAccessor Field;

	public NetworkArray(Span<Type> value, INetworkStateChanged? outer, DynamicArrayAccessor field) {
		Value = value;
		Outer = outer;
		Field = field;
	}

	public ref readonly Type this[int i] => ref Get(i);

	public ref readonly Type Get(int i) {
		Assert(i >= 0 && i < Value.Length);
		return ref Value[i];
	}

	public ref Type GetForModify(int i) {
		Assert(i >= 0 && i < Value.Length);
		NetworkStateChanged(i);
		return ref Value[i];
	}

	public void Set(int i, in Type val) {
		Assert(i >= 0 && i < Value.Length);
		if (!EqualityComparer<Type>.Default.Equals(Value[i], val)) {
			NetworkStateChanged(i);
			Value[i] = val;
		}
	}

	public ReadOnlySpan<Type> Base => Value;
	public int Count() => Value.Length;

	public static implicit operator ReadOnlySpan<Type>(NetworkArray<TArray, Type> netArray) => netArray.Value;

	void NetworkStateChanged(int changeIndex) => Outer?.NetworkStateChanged(Field.AtIndex(changeIndex)!);
}

public struct NetworkVarBase<Type>
{
	public Type Value;
	public NetworkVarChanged<Type>? VarChanged;
}
