namespace Source.Common.Engine;
public interface IStaticPropMgrEngine
{
	public struct LightCacheHandle_t { }

	public bool Init();
	public void Shutdown();

	public void LevelInit();

	public void LevelInitClient();

	public void LevelShutdownClient();

	public void LevelShutdown();

	public void RecomputeStaticLighting();

	public bool IsPropInPVS( IHandleEntity pHandleEntity, byte[] pVis );

	public ICollideable GetStaticProp( IHandleEntity pHandleEntity );

	public LightCacheHandle_t GetLightCacheHandleForStaticProp( IHandleEntity pHandleEntity );

	public bool IsStaticProp( IHandleEntity pHandleEntity );
	public bool IsStaticProp( in BaseHandle handle );

	public int GetStaticPropIndex( IHandleEntity pHandleEntity );
	public bool PropHasBakedLightingDisabled( IHandleEntity pHandleEntity);
}
