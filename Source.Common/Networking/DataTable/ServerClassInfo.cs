using Source.Common.Engine;
using Source.Common.Entity;

namespace Source.Common.Networking.DataTable;

public class ServerClassInfo
{
	public ClientClass? ClientClass = null;
	public string ClassName = null;
	public string DatatableName = null;

	// This is an index into the network string table (cl.GetInstanceBaselineTable()).
	public int InstanceBaselineIndex = INetworkStringTable.INVALID_STRING_INDEX; // INVALID_STRING_INDEX if not initialized yet.
}
