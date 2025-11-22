using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public enum InterpType
{
	Linear,
	Spline
}

public class InterpolatedValue
{
	public InterpolatedValue() {
		StartTime = 0;
		EndTime = 0;
		StartValue = 0;
		EndValue = 0;
		InterpType = InterpType.Linear;
	}

	public InterpolatedValue(TimeUnit_t startTime, TimeUnit_t endTime, float startValue, float endValue, InterpType type) {
		StartTime = startTime;
		EndTime = endTime;
		StartValue = startValue;
		EndValue = endValue;
		InterpType = type;
	}

	public void SetTime(TimeUnit_t start, TimeUnit_t end) {
		StartTime = start;
		EndTime = end;
	}

	public void SetRange(float start, float end) {
		StartValue = start;
		EndValue = end;
	}

	public void SetType(InterpType type) {
		InterpType = type;
	}

	public void SetAbsolute(float value) {
		StartValue = EndValue = value;
		StartTime = EndTime = gpGlobals.CurTime;
		InterpType = InterpType.Linear;
	}

	public void Init(float startValue, float endValue, TimeUnit_t dt, InterpType type = InterpType.Linear) {
		if (dt <= 0) {
			SetAbsolute(endValue);
			return;
		}
		SetTime(gpGlobals.CurTime, gpGlobals.CurTime + dt);
		SetRange(startValue, endValue);
		SetType(type);
	}

	public void InitFromCurrent(float endValue, TimeUnit_t dt, InterpType type = InterpType.Linear) {
		Init(Interp(gpGlobals.CurTime), endValue, dt, type);
	}

	public float Interp(TimeUnit_t curTime) {
		switch (InterpType) {
			case InterpType.Linear:
				if (curTime >= EndTime)
					return EndValue;

				if (curTime <= StartTime)
					return StartValue;

				return (float)MathLib.RemapVal(curTime, StartTime, EndTime, StartValue, EndValue);
			case InterpType.Spline:
				if (curTime >= EndTime)
					return EndValue;

				if (curTime <= StartTime)
					return StartValue;

				return (float)MathLib.SimpleSplineRemapVal(curTime, StartTime, EndTime, StartValue, EndValue);
		}

		// NOTENOTE: You managed to pass in a bogus interpolation type!
		Assert(false);
		return -1.0f;
	}

	TimeUnit_t StartTime;
	TimeUnit_t EndTime;
	float StartValue;
	float EndValue;
	InterpType InterpType;
}
