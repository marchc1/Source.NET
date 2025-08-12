using Source.Common.Networking.DataTable;
using System;

public abstract class DatatableStack
{
	protected const int MAX_PROXY_RESULTS = 256;

	protected SendTablePrecalc Precalc;
	protected IntPtr[] Proxies = new IntPtr[MAX_PROXY_RESULTS];
	protected IntPtr StructBase;
	protected int CurrentProp;
	protected int ObjectID;
	protected bool Initialized;

	protected SendProp CurrentSendProp;

	public DatatableStack(SendTablePrecalc precalc, IntPtr structBase, int objectID)
	{
		Precalc = precalc;
		StructBase = structBase;
		ObjectID = objectID;
		Initialized = false;
	}

	public void Init(bool explicitRoutes = false)
	{
		for (int i = 0; i < Proxies.Length; i++)
			Proxies[i] = 0;

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
		return Proxies[Precalc.PropProxyIndices[CurrentProp]];
	}

	public int GetObjectID() => ObjectID;

	protected abstract void RecurseAndCallProxies(SendNode node, IntPtr structBase);
}

public class ClientDatatableStack : DatatableStack
{
	private RecvDecoder Decoder;

	public ClientDatatableStack(RecvDecoder decoder, IntPtr structBase, int objectID)
		: base(decoder.Precalc, structBase, objectID)
	{
		Decoder = decoder;
	}

	protected override void RecurseAndCallProxies(SendNode node, IntPtr structBase)
	{
		Proxies[node.GetRecursiveProxyIndex()] = structBase;

		for (int i = 0; i < node.Children.Count; i++)
		{
			var child = node.Children[i];
			IntPtr newStructBase = 0;
			if (structBase != 0)
			{
				newStructBase = CallPropProxy(child, child.DatatablePropIndex, structBase);
			}

			RecurseAndCallProxies(child, newStructBase);
		}
	}

	private IntPtr CallPropProxy(SendNode node, int propIndex, IntPtr structBase)
	{
		var prop = Decoder.GetDatatableProp(propIndex);
		if (prop == null)
			return 0;

		IntPtr val = 0;
		prop.DataTableProxyFn?.Invoke(prop, ref val, structBase, ObjectID);
		return val;
	}

	public IntPtr UpdateRoutesExplicit()
	{
		return UpdateRoutesExplicitTemplate(this);
	}

	private static IntPtr UpdateRoutesExplicitTemplate(ClientDatatableStack stack)
	{
		int iPropProxyIndex = stack.Precalc.PropProxyIndices[stack.CurrentProp];
		if (stack.Proxies[iPropProxyIndex] != 0)
			return stack.Proxies[iPropProxyIndex];

		IntPtr structBase = stack.StructBase;
		var proxyPath = stack.Precalc.ProxyPaths[iPropProxyIndex];

		for (int i = 0; i < proxyPath.Entries.Count; i++)
		{
			var entry = proxyPath.Entries[i];
			int iProxy = entry.ProxyIndex;

			if (stack.Proxies[iProxy] == 0)
			{
				var val = CallProxy(stack, structBase, entry.DatatablePropIndex);
				stack.Proxies[iProxy] = val;

				if (val == 0)
				{
					stack.Proxies[iPropProxyIndex] = 0;
					break;
				}
			}

			structBase = stack.Proxies[iProxy];
		}

		return structBase;
	}

	private static IntPtr CallProxy(ClientDatatableStack stack, IntPtr structBase, int propIndex)
	{
		var prop = stack.Decoder.GetDatatableProp(propIndex);
		IntPtr val = 0;
		prop.DataTableProxyFn?.Invoke(prop, ref val, structBase, stack.ObjectID);
		return val;
	}
}
