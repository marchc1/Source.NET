using System.Runtime.CompilerServices;

namespace Source.Common;

public interface ILocalize
{
	bool AddFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID = default, bool includeFallbackSearchPaths = false);
	ReadOnlySpan<char> Find(ReadOnlySpan<char> text);
	ReadOnlySpan<char> TryFind(ReadOnlySpan<char> text);
	ulong FindIndex(ReadOnlySpan<char> value);
	ReadOnlySpan<char> GetValueByIndex(ulong hash);

	// Wow! This sucks!
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1)
		=> ConstructString(localized, format, s1, null, null, null, null, null, null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
		=> ConstructString(localized, format, s1, s2, null, null, null, null, null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3)
		=> ConstructString(localized, format, s1, s2, s3, null, null, null, null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4)
		=> ConstructString(localized, format, s1, s2, s3, s4, null, null, null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4, ReadOnlySpan<char> s5)
		=> ConstructString(localized, format, s1, s2, s3, s4, s5, null, null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4, ReadOnlySpan<char> s5, ReadOnlySpan<char> s6)
		=> ConstructString(localized, format, s1, s2, s3, s4, s5, s6, null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4, ReadOnlySpan<char> s5, ReadOnlySpan<char> s6, ReadOnlySpan<char> s7)
		=> ConstructString(localized, format, s1, s2, s3, s4, s5, s6, s7, null);

	// We'll see if we need more variations of this...
	void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4, ReadOnlySpan<char> s5, ReadOnlySpan<char> s6, ReadOnlySpan<char> s7, ReadOnlySpan<char> s8);
}
