using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Source.Common;

public class BoneAccessor
{
	Memory<Matrix4x4> Bones;
	int ReadableBones;
	int WritableBones;

	public BoneAccessor() {
		Bones = null;
		ReadableBones = WritableBones = 0;
	}

	public BoneAccessor(Matrix4x4[] bones) {
		Bones = bones;
	}

	public int GetReadableBones() => ReadableBones;
	public void SetReadableBones(int flags) => ReadableBones = flags;

	public int GetWritableBones() => WritableBones;
	public void SetWritableBones(int flags) => WritableBones = flags;

	public ref readonly Matrix4x4 GetBone(int bone) => ref Bones.Span[bone];
	public ref Matrix4x4 GetBoneForWrite(int bone) => ref Bones.Span[bone];
	public Span<Matrix4x4> GetBoneArrayForWrite() => Bones.Span;

	public void Init(Memory<Matrix4x4> matrix4x4s) {
		Bones = matrix4x4s;
	}
}
