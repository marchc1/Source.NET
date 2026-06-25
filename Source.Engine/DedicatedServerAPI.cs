using Microsoft.Extensions.DependencyInjection;

using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Input;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;

using System.Reflection;

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Source.Engine;

public class DedicatedServerAPI(IGame game, IServiceProvider services, Common COM, Sys Sys, EngineParms host_parms, SV SV) : IDedicatedServerAPI
{
	public List<MemberInfo> __INTERNAL_FilledDependencies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public object? GetService(Type serviceType) => services.GetService(serviceType);
	public object? GetKeyedService(Type serviceType, object? key) => ((IKeyedServiceProvider)services).GetKeyedService(serviceType, key);
	public object GetRequiredKeyedService(Type serviceType, object? key) => ((IKeyedServiceProvider)services).GetRequiredKeyedService(serviceType, key);

	public bool ModInit(in StartupInfo modInfo) {
		throw new NotImplementedException();
	}

	public void ModShutdown() {
		throw new NotImplementedException();
	}

	public bool RunFrame() {
		throw new NotImplementedException();
	}

	public void AddConsoleText(ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void UpdateStatus(out float fps, out int active, out int maxPlayers, Span<char> map) {
		throw new NotImplementedException();
	}

	public void UpdateHostname(Span<char> hostname) {
		throw new NotImplementedException();
	}
}
