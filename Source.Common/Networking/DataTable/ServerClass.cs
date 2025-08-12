using Source.Common.Engine;
using Source.Common.Entity;
using Source.Common.Networking.DataTable;

namespace Source.Common.Networking.DataTable;

public class ServerClass
{
	public static ServerClass? ServerClassHead = null;
	public ServerClass(string NetworkName, SendTable Table)
	{
		this.NetworkName = NetworkName;
		this.Table = Table;
		this.InstanceBaselineIndex = INetworkStringTable.INVALID_STRING_INDEX;
		// g_pServerClassHead is sorted alphabetically, so find the correct place to insert
		if ( ServerClassHead == null )
		{
			ServerClassHead = this;
			Next = null;
		} else {
			ServerClass? p1 = ServerClassHead;
			ServerClass? p2 = p1.Next;

			// use _stricmp because Q_stricmp isn't hooked up properly yet
			if ( p1.GetName().CompareTo(NetworkName) > 0)
			{
				Next = ServerClassHead;
				ServerClassHead = this;
				p1 = null;
			}

			while( p1 != null )
			{
				if ( p2 == null || p2.GetName().CompareTo(NetworkName) > 0)
				{
					Next = p2;
					p1.Next = this;
					break;
				}
				
				p1 = p2;
				p2 = p2.Next;
			}
		}
		
		ClassID = -1;
	}

	public string GetName() { return NetworkName; }

	public string NetworkName;
	public SendTable Table;
	public ServerClass? Next;
	public int ClassID;	// Managed by the engine.

	// This is an index into the network string table (sv.GetInstanceBaselineTable()).
	public int InstanceBaselineIndex; // INVALID_STRING_INDEX if not initialized yet.
}