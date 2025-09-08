using Source.Common.Networking.DataTable;

namespace Source.Common.Entity;

public interface IHandleEntity
{
	public void SetRefEHandle(BaseHandle handle);
	public BaseHandle GetRefEHandle();
}
