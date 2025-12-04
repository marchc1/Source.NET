using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Source.Common;

public delegate IClientNetworkable CreateClientClassFn(int entNum, int serialNum);
public delegate IClientNetworkable CreateEventFn();

public static class ClientClassRetriever {
	static readonly Dictionary<Type, ClientClass> ClassList = [];

	public static ClientClass GetOrError(Type t) {
		if (ClassList.TryGetValue(t, out ClientClass? c))
			return c;

		FieldInfo? field = t.GetField(nameof(ClientClass), BindingFlags.Static | BindingFlags.Public);
		if(field == null)
			throw new NullReferenceException(nameof(field));

		c = ClassList[t] = (ClientClass)field.GetValue(null)!;
		return c;
	}
}

public class ClientClass
{
	public static ClientClass? Head;

	public CreateClientClassFn CreateFn;
	public CreateEventFn? CreateEventFn;
	public string? NetworkName;
	public RecvTable? RecvTable;
	public ClientClass? Next;
	public int ClassID;

	void AssembleDynamicCreateFn(Type t, [NotNull] ref CreateClientClassFn? createFn) {
		if (t.IsAssignableTo(typeof(IClientEntity))) {
			DynamicMethod method = new DynamicMethod($"CreateObjectDynImpl_{t.Name}", typeof(IClientNetworkable), [typeof(int), typeof(int)]);
			ILGenerator il = method.GetILGenerator();

			LocalBuilder ret = il.DeclareLocal(t);
			LocalBuilder success = il.DeclareLocal(typeof(bool));

			Label returnNull = il.DefineLabel();
			Label returnObj = il.DefineLabel();

			il.Emit(OpCodes.Newobj, t.GetConstructor(Type.EmptyTypes)!);
			il.Emit(OpCodes.Stloc, ret);

			il.Emit(OpCodes.Ldloc, ret);
			il.Emit(OpCodes.Ldarg_0); // entNum
			il.Emit(OpCodes.Ldarg_1); // serialNum
			il.Emit(OpCodes.Callvirt, t.GetMethod("Init", new[] { typeof(int), typeof(int) })!);
			il.Emit(OpCodes.Stloc, success);

			il.Emit(OpCodes.Ldloc, success);
			il.Emit(OpCodes.Brfalse_S, returnNull);

			il.Emit(OpCodes.Ldloc, ret);
			il.Emit(OpCodes.Br_S, returnObj);

			il.MarkLabel(returnNull);
			il.Emit(OpCodes.Ldnull);

			il.MarkLabel(returnObj);
			il.Emit(OpCodes.Ret);

			createFn = method.CreateDelegate<CreateClientClassFn>();
		}
		else
			createFn = (_, _) => throw new NotImplementedException($"ClientClass did not define how to create type '{t.Name}'");
	}

	public ClientClass(ReadOnlySpan<char> networkName, RecvTable recvTable, [CallerArgumentExpression(nameof(recvTable))] string? nameOfTable = null) {
		Type t = WhoCalledMe(skipFrames: 2) ?? throw new NullReferenceException("This doesnt work as well as we hoped!");
		AssembleDynamicCreateFn(t, ref CreateFn);

		NetworkName = new(networkName);
		RecvTable = recvTable;

		if (nameOfTable != null)
			recvTable.NetTableName = nameOfTable;

		Next = Head;
		Head = this;
		ClassID = -1;
	}
	public ClientClass(ReadOnlySpan<char> networkName, CreateClientClassFn? createFn, CreateEventFn? createEventFn, RecvTable recvTable, [CallerArgumentExpression(nameof(recvTable))] string? nameOfTable = null) {
		if (createFn == null) {
			Type t = WhoCalledMe(skipFrames: 2) ?? throw new NullReferenceException("This doesnt work as well as we hoped!");
			AssembleDynamicCreateFn(t, ref createFn);
		}

		CreateFn = createFn;
		CreateEventFn = createEventFn;
		NetworkName = new(networkName);
		RecvTable = recvTable;

		if (nameOfTable != null)
			recvTable.NetTableName = nameOfTable;

		Next = Head;
		Head = this;
		ClassID = -1;
	}

	public ReadOnlySpan<char> GetName() => NetworkName;
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ImplementClientClassAttribute : Attribute
{
	public required string ClientClassName;
	public required string RecvTable;
	public required string ServerClassName;
}
