namespace Source.Common;

public delegate void NetworkVarChanged<T>(ref T newValue);

public struct NetworkVarBase<Type>
{
	public Type Value;
	public NetworkVarChanged<Type>? VarChanged;
}

public struct NetworkArray<Type>(int count) where Type : unmanaged
{
	public readonly Type[] Value = new Type[count];
	public NetworkVarChanged<Type>? VarChanged;

	public readonly ref readonly Type this[int i] => ref Get(i);

	public readonly ref readonly Type Get(int i) {
		Assert(i >= 0 && i < count);
		return ref Value[i];
	}

	public readonly ref Type GetForModify(int i) {
		Assert(i >= 0 && i < count);
		NetworkStateChanged(i);
		return ref Value[i];
	}

	public readonly unsafe void Set(int i, in Type val) {
		Assert(i >= 0 && i < count);
		if (memcmp(in Value[i], in val) != 0) {
			NetworkStateChanged(i);
			Value[i] = val;
		}
	}

	public readonly Type[] Base => Value;
	public readonly int Count() => count;

	public void Hook(NetworkVarChanged<Type> fn) => VarChanged = fn;

	private readonly void NetworkStateChanged(int changeIndex) {
		if (VarChanged == null)
			return;
		VarChanged(ref Value[changeIndex]);
	}
}
