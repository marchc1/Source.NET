namespace Source.Common.MaterialSystem;

public interface IMaterialSystemStub : IMaterialSystem
{
	void SetRealMaterialSystem(IMaterialSystem? sys);
}
