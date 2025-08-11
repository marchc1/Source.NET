using Source.Common.Networking.DataTable;

namespace Source.Common.Entity;

public abstract class IHandleEntity
{
	public abstract void SetRefEHandle(BaseHandle handle);
	public abstract BaseHandle GetRefEHandle();
}
