using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Source.Common;

public class BoneAccessor
{
	Memory<Matrix3x4> Bones;
	int ReadableBones;
	int WritableBones;

	public BoneAccessor() {
		Bones = null;
		ReadableBones = WritableBones = 0;
	}

	public BoneAccessor(Matrix3x4[] bones) {
		Bones = bones;
	}

	public int GetReadableBones() => ReadableBones;
	public void SetReadableBones(int flags) => ReadableBones = flags;

	public int GetWritableBones() => WritableBones;
	public void SetWritableBones(int flags) => WritableBones = flags;

	public ref readonly Matrix3x4 GetBone(int bone) => ref Bones.Span[bone];
	public ref Matrix3x4 GetBoneForWrite(int bone) => ref Bones.Span[bone];
	public Span<Matrix3x4> GetBoneArrayForWrite() => Bones.Span;

	public void Init(Memory<Matrix3x4> matrix3x4s) {
		Bones = matrix3x4s;
	}
}
