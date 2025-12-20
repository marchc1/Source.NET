using Source;
using Source.Common.MaterialSystem;
using Source.Common.Utilities;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Game.Client;

public class ExposeMaterialProxyAttribute : Attribute
{
	public required string Name { get; set; }
}

internal delegate IMaterialProxy CreateProxyFn();
public static class MaterialProxies
{
	static readonly FrozenDictionary<UtlSymId_t, CreateProxyFn> ProxyFns;
	static MaterialProxies() {
		Dictionary<UtlSymId_t, CreateProxyFn> buildProxies = [];
		// Find all material proxies registered with ExposeMaterialProxyAttribute in this assembly
		var proxyTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetCustomAttribute<ExposeMaterialProxyAttribute>() != null);
		foreach (var type in proxyTypes) {
			ExposeMaterialProxyAttribute proxyAttr = type.GetCustomAttribute<ExposeMaterialProxyAttribute>()!;
			UtlSymId_t nameHash = proxyAttr.Name.Hash(false);
			if (buildProxies.TryGetValue(nameHash, out _))
				throw new Exception($"Tried creating a material proxy ({proxyAttr.Name.}) twice");

			if (!type.IsAssignableTo(typeof(IMaterialProxy)))
				throw new Exception($"ExposeMaterialProxyAttribute is not valid on a type that is not IMaterialProxy (got {type})");

			buildProxies[nameHash] = () => (IMaterialProxy)Activator.CreateInstance(type)!;
		}

		ProxyFns = buildProxies.ToFrozenDictionary();
	}
	public static IMaterialProxy? CreateProxyInterfaceFn(ReadOnlySpan<char> name) {

	}
}
