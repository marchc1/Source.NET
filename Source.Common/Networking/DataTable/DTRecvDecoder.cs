namespace Source.Common.Networking.DataTable;

public class CClientSendProp
{
    public string? TableName { get; private set; }

    public CClientSendProp() { }

    public void SetTableName(string? name)
    {
        TableName = name;
    }
}

public class CClientSendTable
{
    public SendTable SendTable { get; } = new SendTable();
    public List<CClientSendProp> Props { get; } = new List<CClientSendProp>();

    public int GetNumProps() => SendTable.NumProps;
    public CClientSendProp GetClientProp(int index) => Props[index];
    public string? GetName() => SendTable.Name;
}

public delegate int ArrayLengthSendProxyFn(object structObj, int objectId);
public delegate object SendVarProxyFn(SendProp prop, object structObj, object inputPtr, ref DVariant outVal, int iElement, int objectId);
public delegate void SendTableProxyFn(SendProp prop, object structObj, object dataTable, int objectId);

public class SendProp
{
    public RecvProp MatchingRecvProp;                  

    public SendPropType Type;
    public int NumBits;
    public float LowValue;
    public float HighValue;

    public SendProp ArrayPropRef; 
    public ArrayLengthSendProxyFn ArrayLengthProxy;
    public int NumElements;
    public int ElementStride;

    public string ExcludeDtName;
    public string ParentArrayPropName;
    public string VarName;

    public float HighLowMul;

    int Flags;
    SendVarProxyFn ProxyFn;
    SendTableProxyFn DataTableProxyFn;
    SendTable DataTable;
    int Offset;
    object ExtraData;

    public SendProp()
    {
        Clear();
    }

    public void Clear()
    {
        MatchingRecvProp = null;

        Type = default;
        NumBits = 0;
        LowValue = 0f;
        HighValue = 0f;

        ArrayPropRef = null;
        ArrayLengthProxy = null;
        NumElements = 1;
        ElementStride = 0;

        ExcludeDtName = null;
        ParentArrayPropName = null;
        VarName = null;

        HighLowMul = 0f;

        Flags = 0;
        ProxyFn = null;
        DataTableProxyFn = null;

        DataTable = null;
        Offset = 0;

        ExtraData = null;
    }

    public int GetOffset() => Offset;
    public void SetOffset(int i) => Offset = i;

    public SendVarProxyFn GetProxyFn()
    {
        return ProxyFn;
    }
    public void SetProxyFn(SendVarProxyFn f) => ProxyFn = f;

    public SendTableProxyFn GetDataTableProxyFn()
    {
        return DataTableProxyFn;
    }
    public void SetDataTableProxyFn(SendTableProxyFn f) => DataTableProxyFn = f;

    public SendTable GetDataTable() => DataTable;
    public void SetDataTable(SendTable table) => DataTable = table;

    public string GetExcludeDTName() => ExcludeDtName;
    public string GetParentArrayPropName() => ParentArrayPropName;
    public void SetParentArrayPropName(string arrayPropName)
    {
        ParentArrayPropName = arrayPropName;
    }
    public string GetName() => VarName;
    public bool IsSigned() => (Flags & (int)NetConstants.SPROP_UNSIGNED) == 0;
    public bool IsExcludeProp() => (Flags & (int)NetConstants.SPROP_EXCLUDE) != 0;
    public bool IsInsideArray() => (Flags & (int)NetConstants.SPROP_INSIDEARRAY) != 0;
    public void SetInsideArray() => Flags |= (int)NetConstants.SPROP_INSIDEARRAY;
    public void SetArrayProp(SendProp pProp) => ArrayPropRef = pProp;
    public SendProp GetArrayProp() => ArrayPropRef;
    public void SetArrayLengthProxy(ArrayLengthSendProxyFn fn) => ArrayLengthProxy = fn;
    public ArrayLengthSendProxyFn GetArrayLengthProxy() => ArrayLengthProxy;
    public int GetNumElements() => NumElements;
    public void SetNumElements(int nElements) => NumElements = nElements;
    public int GetNumArrayLengthBits() => NumArrayLengthBitsFromCapacity(NumElements);
    static int NumArrayLengthBitsFromCapacity(int capacity)
    {
        int v = Math.Max(0, capacity);
        int bits = 0;
        while ((1 << bits) <= v) bits++;
        return bits > 0 ? bits : 1;
    }

    public int GetElementStride() => ElementStride;
    public SendPropType GetType() => Type;

    public int GetFlags() => Flags;
    public void SetFlags(int flags)
    {
        Flags = flags;
    }
    public object GetExtraData() => ExtraData;
    public void SetExtraData(object data) => ExtraData = data;
}

public class RecvDecoder
{
    public RecvTable? RecvTable { get; set; }
    public CClientSendTable? ClientSendTable { get; set; }
    public SendTablePrecalc Precalc { get; } = new SendTablePrecalc();

    public List<RecvProp> Props { get; } = new List<RecvProp>();
    public List<RecvProp> DatatableProps { get; } = new List<RecvProp>();

    public string? GetName() => RecvTable?.NetTableName;
    public SendTable? GetSendTable() => Precalc.GetSendTable();
    public RecvTable? GetRecvTable() => RecvTable;

    public int GetNumProps() => Props.Count;

    public RecvProp? GetProp(int i) =>
        (uint)i < (uint)Props.Count ? Props[i] : null;

    public SendProp? GetSendProp(int i) => Precalc.GetProp(i);

    public int GetNumDatatableProps() => DatatableProps.Count;

    public RecvProp? GetDatatableProp(int i) =>
        (uint)i < (uint)DatatableProps.Count ? DatatableProps[i] : null;
}

public class SendTable
{
    public int NumProps { get; set; }
    public string? Name { get; set; }
}