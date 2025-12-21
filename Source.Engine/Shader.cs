
using Source.Common.MaterialSystem;

namespace Source.Engine;

public class Shader(IMaterialSystem materials)
{
	public static readonly MaterialProxyFactory s_MaterialProxyFactory = new();
	public void SwapBuffers() {
		materials.SwapBuffers();
	}
}
