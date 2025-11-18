using Microsoft.Extensions.DependencyInjection;

namespace Source.Common.Engine;
public interface IEngineAPI : IServiceProvider, IKeyedServiceProvider
{
	public enum Result
	{
		InitFailed = 0,
		InitOK,
		InitRestart,
		RunOK,
		RunRestart
	}

	public Result Run();
	public void SetStartupInfo(in StartupInfo info);
	bool MainLoop();
}
public delegate void PreInject(IServiceCollection services);

public delegate void PreInjectInstance<T>(IServiceProvider services);

/// <summary>
/// Used to sanity check the lifetime of a service locator scope.
/// </summary>
public ref struct ServiceLocatorScope {
	IServiceProvider lifetimeServices;
	public ServiceLocatorScope(IServiceProvider services) {
		Assert(ImportUtils.EngineProvider == null);
		ImportUtils.EngineProvider = lifetimeServices = services;
	}

	public void Dispose() {
		Assert(ImportUtils.EngineProvider != null);
		Assert(ImportUtils.EngineProvider == lifetimeServices);
		ImportUtils.EngineProvider = null;
	}
}

public static class ImportUtils {
	/// <summary>
	/// A static instance of an IServiceProvider that acts as a common service locator for engine components.
	/// While this is not an ideal way to do it, it is the most convenient way to place dependencies
	/// within class instances, for things like entities, panels, etc. which usually will get generated
	/// at a time in which all dependencies are available.
	/// </summary>
	internal static IServiceProvider? EngineProvider { get; set; }

	/// <summary>
	/// Pulls a singleton instance out of the active engine provider.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static T Singleton<T>() where T : notnull {
		Assert(EngineProvider != null);
		//Msg(typeof(T).Name + "\n");
		return EngineProvider!.GetRequiredService<T>();
	}
	/// <summary>
	/// Pulls a keyed singleton instance out of the active engine provider.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static T KeyedSingleton<T>(object? key) where T : notnull {
		Assert(EngineProvider != null);
		return EngineProvider!.GetRequiredKeyedService<T>(key);
	}
}
