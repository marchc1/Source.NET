using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;

public class ModelSoundsCache() : IBaseCacheInfo
{
	public void Rebuild(ReadOnlySpan<char> filename) => throw new NotImplementedException();
	public void Restore(Stream buf) => throw new NotImplementedException();
	public void Save(Stream buf) => throw new NotImplementedException();

	internal void PrecacheSoundList() {
		throw new NotImplementedException();
	}

	public readonly List<ushort> sounds = [];
}
