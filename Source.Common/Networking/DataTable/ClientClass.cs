using Source.Common.Entity;

namespace Source.Common.Networking.DataTable;

public delegate IClientNetworkable CreateClientClassFn(int entnum, int serialNum);
public delegate IClientNetworkable CreateEventFn();

public class ClientClass
{
	public CreateClientClassFn CreateFn;
	public CreateEventFn EventFn;	// Only called for event objects.
	public string NetworkName;
	public RecvTable pRecvTable;
	public ClientClass? Next;
	public int ClassID;	// Managed by the engine.
	public static ClientClass? g_pClientClassHead = null;

	public ClientClass(string NetworkName, CreateClientClassFn CreateFn, CreateEventFn EventFn, RecvTable pRecvTable)
	{
		this.NetworkName = NetworkName;
		this.CreateFn = CreateFn;
		this.EventFn = EventFn;
		this.Next = g_pClientClassHead;
		g_pClientClassHead = this;
		this.pRecvTable = pRecvTable;

		this.ClassID = -1;
	}

	public string GetName()
	{
		return NetworkName;
	}
}

public class ClientClass<T> where T : IClientNetworkable, new()
{
	public ClientClass Class;
	public RecvTable Table;

    public ClientClass(string name)
    {
		Table = new();

		Class = new(name, (entNum, serialNum) =>
        {
            var instance = new T();
            instance.Init(entNum, serialNum);
            return instance;
        }, null, Table);
    }
}