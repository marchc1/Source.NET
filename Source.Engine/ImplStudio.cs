using Source.Common;
using Source.Common.Engine;

namespace Source.Engine;

public class ModelRender : IModelRender
{
	public ModelInstanceHandle_t CreateInstance(IClientRenderable renderable) {
		throw new NotImplementedException();
	}
	public void DestroyInstance(ModelInstanceHandle_t modelInstance) {
		// TODO
	}
}
