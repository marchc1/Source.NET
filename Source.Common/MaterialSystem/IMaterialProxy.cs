using Source.Common.Formats.Keyvalues;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.MaterialSystem;

public interface IMaterialProxy
{
	bool Init(IMaterial material, KeyValues keyValues);
	void OnBind(object o);
	void Release();
	IMaterial GetMaterial();
}

public delegate IMaterialProxy? LookupProxyInterfaceFn(ReadOnlySpan<char> proxyName);

public interface IMaterialProxyFactory
{
	IMaterialProxy? CreateProxy(ReadOnlySpan<char> proxyName);
	void DeleteProxy(IMaterialProxy proxy);
}
