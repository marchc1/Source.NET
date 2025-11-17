using System.Runtime.CompilerServices;

namespace Source.Common;

[InlineArray(Studio.MAXSTUDIOBONES >> 3)]
public struct BoneBitList
{
	public byte bytes;

	public int Get(int bit) => BitVecBase.IsBitSet(this, bit) ? 1 : 0;
	public bool IsBitSet(int bit) => BitVecBase.IsBitSet(this, bit);
	public void Set(int bit) => BitVecBase.Set(this, bit);
	public void Clear(int bit) => BitVecBase.Clear(this, bit);
	public void Set(int bit, bool newVal) => BitVecBase.Set(this, bit, newVal);
	public int FindNextSetBit(int startBit) => BitVecBase.FindNextSetBit(this, startBit);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void MarkBone(int bone) => Set(bone);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsBoneMarked(int bone) => Get(bone) != 0;
}
