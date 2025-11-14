namespace Source.Common.Engine;

public interface IModelRender
{
	ModelInstanceHandle_t CreateInstance(IClientRenderable renderable);
	void DestroyInstance(ModelInstanceHandle_t modelInstance);
}
