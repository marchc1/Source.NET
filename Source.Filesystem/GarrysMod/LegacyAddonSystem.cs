using Source.Common.GarrysMod;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Filesystem.GarrysMod;

public class LegacyAddonSystem : LegacyAddons.System
{
	public List<ILegacyAddons.Information> GetList() {
		throw new NotImplementedException();
	}

	public void Refresh() {
		throw new NotImplementedException();
	}
}
