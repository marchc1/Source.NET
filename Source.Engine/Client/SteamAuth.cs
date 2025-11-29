global using static Source.Engine.Client.Steam3ClientAccessor;
namespace Source.Engine.Client;


[EngineComponent]
public static class Steam3ClientAccessor
{
#if !SWDS
	[Dependency] static Steam3Client _client = null!;
#endif
	public static Steam3Client? Steam3Client()
#if !SWDS
		=> _client;
#else
		=> null;
#endif
}

#if SWDS
public class Steam3Client;
#else
[EngineComponent]
public class Steam3Client : IDisposable
{
	public void Dispose() {}
}
#endif
