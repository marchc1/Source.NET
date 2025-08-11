using Source.Common.Networking.DataTable;
using System;

public abstract class DatatableStack
{
    protected const int MAX_PROXY_RESULTS = 256;

    protected SendTablePrecalc Precalc;
    protected byte[][] Proxies = new byte[MAX_PROXY_RESULTS][];
    protected byte[] StructBase;
    protected int CurrentProp;
    protected int ObjectID;
    protected bool Initialized;

    protected SendProp CurrentSendProp;

    public DatatableStack(SendTablePrecalc precalc, byte[] structBase, int objectID)
    {
        Precalc = precalc;
        StructBase = structBase;
        ObjectID = objectID;
        Initialized = false;
    }

    public void Init(bool explicitRoutes = false)
    {
        for (int i = 0; i < Proxies.Length; i++)
            Proxies[i] = null;

        Proxies[Precalc.Root.DatatablePropIndex] = StructBase;
        RecurseAndCallProxies(Precalc.Root, StructBase);
        Initialized = true;
    }

    public void SeekToProp(int iProp)
    {
        if (!Initialized)
            throw new InvalidOperationException("SeekToProp called before Init.");

        CurrentProp = iProp;
        CurrentSendProp = Precalc.GetProp(iProp);
    }

    public bool IsCurrentProxyValid() => Proxies[Precalc.PropProxyIndices[CurrentProp]] != null;

    public bool IsPropProxyValid(int iProp) => Proxies[Precalc.PropProxyIndices[iProp]] != null;

    public int GetCurrentPropIndex() => CurrentProp;

	public unsafe IntPtr GetCurrentStructBase()
	{
		byte[] buffer = Proxies[Precalc.PropProxyIndices[CurrentProp]];

		fixed (byte* ptr = buffer)
		{
			return (IntPtr)ptr;
		}
	}

	public int GetObjectID() => ObjectID;

    protected abstract void RecurseAndCallProxies(SendNode node, byte[] structBase);
}

public class ClientDatatableStack : DatatableStack
{
    private RecvDecoder Decoder;

    public ClientDatatableStack(RecvDecoder decoder, byte[] structBase, int objectID)
        : base(decoder.Precalc, structBase, objectID)
    {
        Decoder = decoder;
    }

    protected override void RecurseAndCallProxies(SendNode node, byte[] structBase)
    {
        Proxies[node.GetRecursiveProxyIndex()] = structBase;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            byte[] newStructBase = null;

            if (structBase != null)
            {
                newStructBase = CallPropProxy(child, child.DatatablePropIndex, structBase);
            }

            RecurseAndCallProxies(child, newStructBase);
        }
    }

    private byte[] CallPropProxy(SendNode node, int propIndex, byte[] structBase)
    {
        var prop = Decoder.GetDatatableProp(propIndex);
        if (prop == null)
            return null;

        object val = null;
        prop.DataTableProxyFn?.Invoke(prop, ref val, structBase, ObjectID);
        return val as byte[];
    }

    public byte[] UpdateRoutesExplicit()
    {
        return UpdateRoutesExplicitTemplate(this);
    }

    private static byte[] UpdateRoutesExplicitTemplate(ClientDatatableStack stack)
    {
        int iPropProxyIndex = stack.Precalc.PropProxyIndices[stack.CurrentProp];
        if (stack.Proxies[iPropProxyIndex] != null)
            return stack.Proxies[iPropProxyIndex];

        byte[] structBase = stack.StructBase;
        var proxyPath = stack.Precalc.ProxyPaths[iPropProxyIndex];

        for (int i = 0; i < proxyPath.Entries.Count; i++)
        {
            var entry = proxyPath.Entries[i];
            int iProxy = entry.ProxyIndex;

            if (stack.Proxies[iProxy] == null)
            {
                var val = CallProxy(stack, structBase, entry.DatatablePropIndex);
                stack.Proxies[iProxy] = val;

                if (val == null)
                {
                    stack.Proxies[iPropProxyIndex] = null;
                    break;
                }
            }

            structBase = stack.Proxies[iProxy];
        }

        return structBase;
    }

    private static byte[] CallProxy(ClientDatatableStack stack, byte[] structBase, int propIndex)
    {
        var prop = stack.Decoder.GetDatatableProp(propIndex);
        object val = null;
        prop.DataTableProxyFn?.Invoke(prop, ref val, structBase, stack.ObjectID);
        return val as byte[];
    }
}
