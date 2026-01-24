using Source;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Drawing.Drawing2D;
using System.Numerics;
using System.Reflection;

namespace Source.Common;


// TODO: These two interfaces
public interface ISave
{

}

public interface IRestore
{
	
}

public ref struct SaveRestoreFieldInfo
{
	public SaveRestoreFieldInfo() {
		Field = null!;
		Owner = null!;
	}
	public SaveRestoreFieldInfo(IFieldAccessor accessor, object owner, Span<TypeDescription> typedesc) {
		Field = accessor;
		Owner = owner;
		TypeDesc = typedesc;
	}
	public IFieldAccessor Field;
	public object Owner;
	public Span<TypeDescription> TypeDesc;
}

public interface ISaveRestoreOps
{
	void Save(in SaveRestoreFieldInfo fieldInfo, ISave save);
	void Restore(in SaveRestoreFieldInfo fieldInfo, IRestore restore);

	bool IsEmpty(in SaveRestoreFieldInfo fieldInfo);
	void MakeEmpty(in SaveRestoreFieldInfo fieldInfo);
	bool Parse(in SaveRestoreFieldInfo fieldInfo, ReadOnlySpan<char> value);

	//---------------------------------

	public void Save(IFieldAccessor field, object owner, ISave save) {
		SaveRestoreFieldInfo fieldInfo = new(field, owner, null);
		Save(fieldInfo, save);
	}

	public void Restore(IFieldAccessor field, object owner, IRestore restore) {
		SaveRestoreFieldInfo fieldInfo = new(field, owner, null);
		Restore(fieldInfo, restore);
	}

	public bool IsEmpty(IFieldAccessor field, object owner) {
		SaveRestoreFieldInfo fieldInfo = new(field, owner, null);
		return IsEmpty(fieldInfo);
	}

	public void MakeEmpty(IFieldAccessor field, object owner) {
		SaveRestoreFieldInfo fieldInfo = new(field, owner, null);
		MakeEmpty(fieldInfo);
	}

	public bool Parse(IFieldAccessor field, object owner, ReadOnlySpan<char> value) {
		SaveRestoreFieldInfo fieldInfo = new(field, owner, null);
		return Parse(fieldInfo, value);
	}
}
