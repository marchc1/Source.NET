using Microsoft.Extensions.DependencyInjection;

using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Input;
using Source.Common.Launcher;
using Source.Common.MaterialSystem;

using System.Reflection;

using System.Runtime.CompilerServices;

namespace Source.Engine;


public class EngineAPI(IGame game, IServiceProvider services, Common COM, Sys Sys, ILauncherManager launcherMgr, IInputSystem inputSystem) : IEngineAPI, IDisposable
{
	public bool Dedicated;

	public void Dispose() {
		((IDisposable)services).Dispose();
		GC.SuppressFinalize(this);
	}

	StartupInfo startupInfo;

	Lazy<IEngine> engR = new(services.GetRequiredService<IEngine>);


	internal List<MemberInfo>? filledDependencies;

	public IEngineAPI.Result RunListenServer() {
		IEngineAPI.Result result = IEngineAPI.Result.RunOK;
		IMod mod = services.GetRequiredService<IMod>();
		if (mod.Init(startupInfo.InitialMod, startupInfo.InitialGame)) {
			result = (IEngineAPI.Result)mod.Run();
			mod.Shutdown();
		}
		EngineBuilder.InvalidateEngineDeps(filledDependencies);
		return result;
	}


	public void SetStartupInfo(in StartupInfo info) {
		startupInfo = info;
		Sys.TextMode = info.TextMode;
		COM.InitFilesystem(info.InitialMod);
	}

	public IEngineAPI.Result Run() {
		services.GetRequiredService<IMaterialSystem>().ModInit();

		ConVar_Register();
		return RunListenServer();
	}

	public object? GetService(Type serviceType) => services.GetService(serviceType);
	public object? GetKeyedService(Type serviceType, object? key) => ((IKeyedServiceProvider)services).GetKeyedService(serviceType, key);
	public object GetRequiredKeyedService(Type serviceType, object? key) => ((IKeyedServiceProvider)services).GetRequiredKeyedService(serviceType, key);

	public bool InEditMode() => false;
	public void PumpMessages() {
		launcherMgr.PumpWindowsMessageLoop();
		inputSystem.PollInputState();
		game.DispatchAllStoredGameMessages();
	}
	public void PumpMessagesEditMode(bool idle, long idleCount) => throw new NotImplementedException();
	public void ActivateEditModeShaders(bool active) { }

	public bool MainLoop() {
		bool idle = true;
		long idleCount = 0;
		while (true) {
			IEngine eng = engR.Value;
			switch (eng.GetQuitting()) {
				case IEngine.Quit.NotQuitting:
					if (!InEditMode())
						PumpMessages();
					else
						PumpMessagesEditMode(idle, idleCount);

					if (!InEditMode()) {
						ActivateEditModeShaders(false);
						eng.Frame();
						ActivateEditModeShaders(true);
					}

					if (InEditMode()) {
						// hammer.RunFrame()? How would this work? todo; learn how editmode works.
					}
					break;
				case IEngine.Quit.ToDesktop: return false;
				case IEngine.Quit.Restart: return true;
			}
		}
	}

	static IEnumerable<Type> safeTypeGet(Assembly assembly) {
		IEnumerable<Type?> types;
		try {
			types = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e) {
			types = e.Types;
		}
		foreach (var t in types.Where(t => t != null && t.Assembly.GetName().Name != "Steamworks.NET"))
			yield return t!;
	}
	void ConVar_Register() {
		ICvar cvar = this.GetRequiredService<ICvar>();
		var assemblies = AppDomain.CurrentDomain.GetAssemblies()
			.Where(ReflectionUtils.IsOkAssembly);

		Type CVAR = typeof(ConVar);
		Type CCMD = typeof(ConCommand);

		foreach (var assembly in assemblies) {
			cvar.SetAssemblyIdentifier(assembly);
			foreach (var type in assembly.GetTypes()) {
				if (type.IsAssignableTo(typeof(ConVar)) || type == typeof(ConVarRef))
					continue;

				var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

				// If any props/fields exist, run the cctor so we can pull out static cvars/concmds
				if ((props.Length + fields.Length) > 0)
					RuntimeHelpers.RunClassConstructor(type.TypeHandle);

				for (int i = 0; i < props.Length; i++) {
					PropertyInfo prop = props[i];
					if (!prop.PropertyType.IsAssignableTo(CVAR))
						continue;
					if (prop.GetCustomAttribute<CvarIgnoreAttribute>() != null)
						continue;

					var getMethod = prop.GetGetMethod();

					if (getMethod == null)
						continue;

					if (getMethod.IsStatic) {
						// Pull a static reference out to link
						ConVar? cv = (ConVar?)getMethod.Invoke(null, null);
						if (cv == null) continue;
						if (cv.GetName() == null) cv.SetName(prop.Name);
						cvar.RegisterConCommand(cv);
					}
					else {
						object? instance = DetermineInstance(type, false, prop.Name);
						ConVar? cv = (ConVar?)getMethod.Invoke(instance, null);
						if (cv == null) continue;
						if (cv.GetName() == null) cv.SetName(prop.Name);
						cvar.RegisterConCommand(cv);
					}
				}

				for (int i = 0; i < fields.Length; i++) {
					FieldInfo field = fields[i];
					if (!field.FieldType.IsAssignableTo(CVAR))
						continue;
					if (field.GetCustomAttribute<CvarIgnoreAttribute>() != null)
						continue;

					if (field.IsStatic) {
						// Pull a static reference out to link
						ConVar? cv = (ConVar?)field.GetValue(null);
						if (cv == null) continue;
						if (cv.GetName() == null) cv.SetName(field.Name);
						cvar.RegisterConCommand(cv);
					}
					else {
						object? instance = DetermineInstance(type, false, field.Name);
						ConVar? cv = (ConVar?)field.GetValue(instance);
						if (cv == null) continue;
						if (cv.GetName() == null) cv.SetName(field.Name);
						cvar.RegisterConCommand(cv);
					}
				}

				for (int i = 0; i < methods.Length; i++) {
					MethodInfo method = methods[i];
					ConCommandAttribute? cmdAttr = method.GetCustomAttribute<ConCommandAttribute>();
					if (cmdAttr == null)
						continue;

					ConCommandAttribute attribute = method.GetCustomAttribute<ConCommandAttribute>()!; // ^^ never null!
					object? instance = method.IsStatic ? null : DetermineInstance(type, true, method.Name);

					// Lets see if we can find a FnCommandCompletionCallback...
					FnCommandCompletionCallback? completionCallback = null;
					if (attribute.AutoCompleteMethod != null)
						type.TryExtractMethodDelegate(instance, x => x.Name == attribute.AutoCompleteMethod, out completionCallback);

					// Construct a new ConCommand
					string cmdName = attribute.Name ?? method.Name;
					string? helpText = attribute.HelpText;
					FCvar flags = attribute.Flags;

					ConCommand cmd;

					if (method.TryToDelegate<FnCommandCallbackVoid>(instance, out var callbackVoid))
						cmd = new(cmdName, callbackVoid, helpText, flags, completionCallback);
					else if (method.TryToDelegate<FnCommandCallback>(instance, out var callback))
						cmd = new(cmdName, callback, helpText, flags, completionCallback);
					else if (method.TryToDelegate<FnCommandCallbackSourced>(instance, out var callbackSourced))
						cmd = new(cmdName, callbackSourced, helpText, flags, completionCallback);
					else
						throw new ArgumentException("Cannot dynamically produce ConCommand with the arguments we were given");

					cvar.RegisterConCommand(cmd);
				}
			}
		}
	}

	private object? DetermineInstance(Type type, bool concommand, ReadOnlySpan<char> name) {
		// We need to find an appropriate instance of the type in question.
		// If it's not registered with the dependency injection framework, then we can't really link anything
		// Should've made it static...
		object? instance = GetService(type);

		// As a last resort, try pulling at interface types.
		if (instance == null) {
			foreach (var iface in type.GetInterfaces()) {
				instance = GetService(iface);
				if (instance != null)
					return instance;
			}
		}
		else
			return instance;

		throw new Exception($"{(concommand ? "ConCommand" : "ConVar")} at member '{name}' was marked as by-instance, and the EngineAPI cannot find an instance of the type it's contained in ({type.Name}). Review if the instance type is an engine component, or if this should be a static field/method. If you are trying to hold a reference to a ConVar, either use a ConVarRef or mark the field/property with a [CvarIgnore] attribute.");
	}
}
