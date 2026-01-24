using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;


public delegate C_BaseEntity DispatchFunction();
public interface IClassMap
{
	void Add(Type type, ReadOnlySpan<char> mapname, ReadOnlySpan<char> classname, DispatchFunction? factory = null);
	ReadOnlySpan<char> Lookup(ReadOnlySpan<char> classname);
	C_BaseEntity? CreateEntity(ReadOnlySpan<char> mapname);
}
