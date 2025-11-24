using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common;

public interface IUniformRandomStream
{
	void SetSeed(int seed);
	float RandomFloat(float minVal, float maxVal);
	int RandomInt(int minVal, int maxVal);
	float RandomFloatExp(float minVal, float maxVal, float exponent);
}

// TODO: This isn't right. But it will work for now
public class TempRandomness : IUniformRandomStream
{
	Random rand = new();
	public float RandomFloat(float minVal, float maxVal) {
		return (rand.NextSingle() * (maxVal - minVal)) + minVal;
	}

	public float RandomFloatExp(float minVal, float maxVal, float exponent) {
		float fl = rand.NextSingle();
		if (exponent != 1.0f)
			fl = MathF.Pow(fl, exponent);

		return (fl * (maxVal - minVal)) + minVal;
	}

	public int RandomInt(int minVal, int maxVal) {
		return rand.Next(minVal, maxVal);
	}

	public void SetSeed(int seed) {
		rand = new(seed);
	}
}
