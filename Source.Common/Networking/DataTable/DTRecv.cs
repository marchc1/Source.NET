using Source.Common.Entity;

namespace Source.Common.Networking.DataTable;

public delegate void RecvVarProxyFn(CRecvProxyData pData, object pStruct, object pOut);
public delegate void ArrayLengthRecvProxyFn(object pStruct, int objectID, int currentArrayLength);
public delegate void DataTableRecvVarProxyFn(RecvProp pProp, ref object pOut, object pData, int objectID);

public class CRecvProxyData
{
	public RecvProp? RecvProp { get; set; }
	public DVariant Value;
	public int Element;
	public int ObjectID;
}

public class CStandardRecvProxies
{
	public RecvVarProxyFn? Int32ToInt8;
	public RecvVarProxyFn? Int32ToInt16;
	public RecvVarProxyFn? Int32ToInt32;
	public RecvVarProxyFn? FloatToFloat;
	public RecvVarProxyFn? VectorToVector;
	public RecvVarProxyFn? Int64ToInt64;
}

public static class GlobalProxies
{
	public static readonly CStandardRecvProxies g_StandardRecvProxies = new();
}

public class RecvProp
{
	public string? VarName;
	public SendPropType RecvType;
	public int Flags;
	public int StringBufferSize;

	private bool InsideArray;
	private object? ExtraData;

	private RecvProp? ArrayProp;
	private ArrayLengthRecvProxyFn? ArrayLengthProxy;
	private RecvVarProxyFn? ProxyFn;
	public DataTableRecvVarProxyFn? DataTableProxyFn;
	private RecvTable? DataTable;
	private int Offset;
	private int ElementStride;
	private int NumElements;
	private string? ParentArrayPropName;

	public RecvProp() { }

	public void InitArray(int nElements, int elementStride)
	{
		NumElements = nElements;
		ElementStride = elementStride;
	}

	public int GetNumElements() => NumElements;
	public void SetNumElements(int nElements) => NumElements = nElements;

	public int GetElementStride() => ElementStride;
	public void SetElementStride(int stride) => ElementStride = stride;

	public int GetFlags() => Flags;

	public string? GetName() => VarName;
	public SendPropType GetType() => RecvType;

	public RecvTable? GetDataTable() => DataTable;
	public void SetDataTable(RecvTable table) => DataTable = table;

	public RecvVarProxyFn? GetProxyFn() => ProxyFn;
	public void SetProxyFn(RecvVarProxyFn fn) => ProxyFn = fn;

	public DataTableRecvVarProxyFn? GetDataTableProxyFn() => DataTableProxyFn;
	public void SetDataTableProxyFn(DataTableRecvVarProxyFn fn) => DataTableProxyFn = fn;

	public int GetOffset() => Offset;
	public void SetOffset(int o) => Offset = o;

	public RecvProp? GetArrayProp() => ArrayProp;
	public void SetArrayProp(RecvProp prop) => ArrayProp = prop;

	public void SetArrayLengthProxy(ArrayLengthRecvProxyFn proxy) => ArrayLengthProxy = proxy;
	public ArrayLengthRecvProxyFn? GetArrayLengthProxy() => ArrayLengthProxy;

	public bool IsInsideArray() => InsideArray;
	public void SetInsideArray() => InsideArray = true;

	public object? GetExtraData() => ExtraData;
	public void SetExtraData(object data) => ExtraData = data;

	public string? GetParentArrayPropName() => ParentArrayPropName;
	public void SetParentArrayPropName(string name) => ParentArrayPropName = name;
}

public class RecvTable
{
	public RecvProp[] Props;
	public int TotalProps;
	public RecvDecoder Decoder;
	public string NetTableName;

	private bool Initialized;
	private bool InMainList;

	public void Construct(RecvProp Props, int TotalProps, string NetTableName)
	{

	}

	public int GetNumProps()
	{
		return TotalProps;
	}

	public RecvProp? GetProp(int index)
	{
		return Props[index];
	}

	public string GetName()
	{
		return NetTableName;
	}

	public void SetInitialized(bool Initialized)
	{
		this.Initialized = Initialized;
	}

	public bool IsInitialized()
	{
		return Initialized;
	}

	public void SetInMainList(bool InList)
	{
		this.InMainList = InList;
	}

	public bool IsInMainList()
	{
		return InMainList;
	}
}