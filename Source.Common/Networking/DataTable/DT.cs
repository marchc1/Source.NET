using Source.Common.Bitbuffers;

namespace Source.Common.Networking.DataTable;

public class SendNode
{
    public List<SendNode> Children { get; } = new List<SendNode>();

    public short DatatablePropIndex = -1;
    public SendTable Table;

    public ushort FirstRecursiveProp;
    public ushort NumRecursiveProps;

    private ushort DataTableProxyIndex;
    private ushort RecursiveProxyIndex;

    public int GetNumChildren() => Children.Count;

    public SendNode GetChild(int i) => Children[i];

    public bool IsPropInRecursiveProps(int i)
    {
        int index = i - FirstRecursiveProp;
        return index >= 0 && index < NumRecursiveProps;
    }

    public ushort GetDataTableProxyIndex()
    {
        return DataTableProxyIndex;
    }

    public void SetDataTableProxyIndex(ushort value)
    {
        DataTableProxyIndex = value;
    }

    public ushort GetRecursiveProxyIndex()
    {
        return RecursiveProxyIndex;
    }

    public void SetRecursiveProxyIndex(ushort value)
    {
        RecursiveProxyIndex = value;
    }
}

public class SendTablePrecalc
{
    public class ProxyPathEntry
    {
        public ushort DatatablePropIndex;
        public ushort ProxyIndex;
    }

    public class ProxyPath
    {
        public ushort FirstEntry;
        public ushort NumEntries;
        public List<ProxyPathEntry> Entries = new();
    }

    public List<ProxyPathEntry> ProxyPathEntries = new();
    public List<ProxyPath> ProxyPaths = new();
    public List<SendProp> Props = new();
    public List<byte> PropProxyIndices = new();
    public List<SendProp> DatatableProps = new();

    public SendNode Root = new();
    public SendTable Table;
    public int NumDataTableProxies;

    public int GetNumProps() => Props.Count;
    public SendProp GetProp(int i) => Props[i];
    public int GetNumDatatableProps() => DatatableProps.Count;
    public SendProp GetDatatableProp(int i) => DatatableProps[i];

    public SendNode GetRootNode() => Root;
    public void SetNumDataTableProxies(int count) => NumDataTableProxies = count;
	public SendTable GetSendTable() => Table;
}