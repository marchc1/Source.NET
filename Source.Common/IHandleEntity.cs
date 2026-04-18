namespace Source.Common;

public interface IHandleEntity {
	void SetRefEHandle(in BaseHandle handle);
	ref readonly BaseHandle GetRefEHandle();
}
