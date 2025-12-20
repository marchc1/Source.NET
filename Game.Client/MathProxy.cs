using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public class SineProxy : ResultProxy {
	public override bool Init(IMaterial material, KeyValues keyValues) {
		if (!base.Init(material, keyValues))
			return false;

		if (!SinePeriod.Init(material, keyValues, "sinePeriod", 1.0f))
			return false;
		if (!SineMax.Init(material, keyValues, "sineMax", 1.0f))
			return false;
		if (!SineMin.Init(material, keyValues, "sineMin", 0.0f))
			return false;
		if (!SineTimeOffset.Init(material, keyValues, "timeOffset", 0.0f))
			return false;

		return true;
	}
	public override void OnBind(object? o) {
		float flValue;
		float flSineTimeOffset = SineTimeOffset.GetFloat();
		float flSineMax = SineMax.GetFloat();
		float flSineMin = SineMin.GetFloat();
		float flSinePeriod = SinePeriod.GetFloat();
		if (flSinePeriod == 0)
			flSinePeriod = 1;

		// get a value in [0,1]
		flValue = (float)((Math.Sin(2.0f * Math.PI * (gpGlobals.CurTime - flSineTimeOffset) / flSinePeriod) * 0.5) + 0.5);
		// get a value in [min,max]	
		flValue = (flSineMax - flSineMin) * flValue + flSineMin;

		SetFloatResult(flValue);
	}

	readonly FloatInput SinePeriod = new();
	readonly FloatInput SineMax = new();
	readonly FloatInput SineMin = new();
	readonly FloatInput SineTimeOffset = new();
}
