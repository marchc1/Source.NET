using CommunityToolkit.HighPerformance;

using Source.Common.Audio;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using ZstdSharp.Unsafe;

namespace Source.Engine;


public partial class Sound
{
	internal readonly List<PaintBuffer> PaintBuffers = [];
	internal PortableSamplePair[]? CurPaintBuffer = null;
	internal PortableSamplePair[]? CurRearPaintBuffer = null;
	internal PortableSamplePair[]? CurCenterPaintBuffer = null;

	public void MIX_SetCurrentPaintbuffer(int ipaintbuffer) {
		Assert(ipaintbuffer < PaintBuffers.Count);

		CurPaintBuffer = PaintBuffers[ipaintbuffer].Buf;

		if (PaintBuffers[ipaintbuffer].Surround) {
			CurRearPaintBuffer = PaintBuffers[ipaintbuffer].BufRear;

			CurCenterPaintBuffer = null;

			if (PaintBuffers[ipaintbuffer].SurroundCenter)
				CurCenterPaintBuffer = PaintBuffers[ipaintbuffer].BufCenter;
		}
		else {
			CurRearPaintBuffer = null;
			CurCenterPaintBuffer = null;
		}

		Assert(CurPaintBuffer != null);
	}

	public PortableSamplePair[]? MIX_GetPFrontFromIPaint(int ipaintbuffer) => PaintBuffers[ipaintbuffer].Buf;
	public PortableSamplePair[]? MIX_GetPRearFromIPaint(int ipaintbuffer) {
		if (PaintBuffers[ipaintbuffer].Surround)
			return PaintBuffers[ipaintbuffer].BufRear;
		return null;
	}
	public PortableSamplePair[]? MIX_GetPCenterFromIPaint(int ipaintbuffer) {
		if (PaintBuffers[ipaintbuffer].SurroundCenter)
			return PaintBuffers[ipaintbuffer].BufCenter;
		return null;
	}
	public int MIX_GetIPaintFromPFront(PortableSamplePair[]? pbuf) {
		for (int i = 0; i < PaintBuffers.Count; i++)
			if (pbuf == PaintBuffers[i].Buf)
				return i;

		return 0;
	}
	public PaintBuffer MIX_GetPPaintFromPFront(PortableSamplePair[]? pbuf) {
		int i = MIX_GetIPaintFromPFront(pbuf);
		return PaintBuffers[i];
	}
	public void MIX_ConvertBufferToSurround(int ipaintbuffer) {
		PaintBuffer paint = PaintBuffers[ipaintbuffer];

		// duplicate channel data as needed
		if (AudioDevice!.IsSurround()) {
			// set buffer flags

			paint.Surround = AudioDevice.IsSurround();
			paint.SurroundCenter = AudioDevice.IsSurroundCenter();

			PortableSamplePair[]? pfront = MIX_GetPFrontFromIPaint(ipaintbuffer);
			PortableSamplePair[]? prear = MIX_GetPRearFromIPaint(ipaintbuffer);
			PortableSamplePair[]? pcenter = MIX_GetPCenterFromIPaint(ipaintbuffer);

			// copy front to rear
			memcpy(prear, pfront);

			// copy front to center
			if (AudioDevice.IsSurroundCenter())
				memcpy(pcenter, pfront);
		}
	}
	public void MIX_ActivatePaintbuffer(int ipaintbuffer) {
		Assert(ipaintbuffer < PaintBuffers.Count);
		PaintBuffers[ipaintbuffer].Active = true;
	}
	public void MIX_DeactivatePaintbuffer(int ipaintbuffer) {
		Assert(ipaintbuffer < PaintBuffers.Count);
		PaintBuffers[ipaintbuffer].Active = false;
	}
	public void MIX_DeactivateAllPaintbuffers() {
		for (int i = 0; i < PaintBuffers.Count; i++)
			PaintBuffers[i].Active = false;
	}
	public PaintBuffer MIX_GetPPaintFromIPaint(int ipaintbuffer) {
		Assert(ipaintbuffer < PaintBuffers.Count);
		return PaintBuffers[ipaintbuffer];
	}
	public void MIX_CenterFromLeftRight(ref int pl, ref int pr, ref int pc) {
		int l = pl;
		int r = pr;
		int c = 0;
		c = (l + r) / 2;
		pc = c;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SWAP<T>(ref T a, ref T b, ref T t) { t = a; a = b; b = t; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int AVG(int a, int b) => (a + b) >> 1;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int AVG4(int a, int b, int c, int d) => (a + b + c + d) >> 2;

	public void MIX_MixPaintbuffers(int buf1, int buf2, int buf3, int count, float fgain_out) {
		int i;
		PortableSamplePair[] pbuf1 = default!, pbuf2 = default!, pbuf3 = default!, pbuft = default!;
		PortableSamplePair[] pbufrear1 = default!, pbufrear2 = default!, pbufrear3 = default!, pbufreart = default!;
		PortableSamplePair[] pbufcenter1 = default!, pbufcenter2 = default!, pbufcenter3 = default!, pbufcentert = default!;
		int cchan1 = default, cchan2 = default, cchan3 = default, cchant = default;
		int xl, xr;
		int l, r, l2, r2, c = default, c2 = default;
		int gain_out;

		gain_out = (int)(256 * fgain_out);

		Assert(count <= PaintBuffer.PAINTBUFFER_SIZE);
		Assert(buf1 < PaintBuffers.Count);
		Assert(buf2 < PaintBuffers.Count);
		Assert(buf3 < PaintBuffers.Count);

		pbuf1 = PaintBuffers[buf1].Buf!;
		pbuf2 = PaintBuffers[buf2].Buf!;
		pbuf3 = PaintBuffers[buf3].Buf!;

		pbufrear1 = PaintBuffers[buf1].BufRear!;
		pbufrear2 = PaintBuffers[buf2].BufRear!;
		pbufrear3 = PaintBuffers[buf3].BufRear!;

		pbufcenter1 = PaintBuffers[buf1].BufCenter!;
		pbufcenter2 = PaintBuffers[buf2].BufCenter!;
		pbufcenter3 = PaintBuffers[buf3].BufCenter!;

		cchan1 = 2 + (PaintBuffers[buf1].Surround ? 2 : 0) + (PaintBuffers[buf1].SurroundCenter ? 1 : 0);
		cchan2 = 2 + (PaintBuffers[buf2].Surround ? 2 : 0) + (PaintBuffers[buf2].SurroundCenter ? 1 : 0);
		cchan3 = 2 + (PaintBuffers[buf3].Surround ? 2 : 0) + (PaintBuffers[buf3].SurroundCenter ? 1 : 0);

		if (cchan2 < cchan1) {
			SWAP(ref cchan1, ref cchan2, ref cchant);
			SWAP(ref pbuf1, ref pbuf2, ref pbuft);
			SWAP(ref pbufrear1, ref pbufrear2, ref pbufreart);
			SWAP(ref pbufcenter1, ref pbufcenter2, ref pbufcentert);
		}

		if (cchan3 == 2) {
			if (cchan1 == 2 && cchan2 == 2) {
				for (i = 0; i < count; i++) {
					pbuf3[i].Left = pbuf1[i].Left + pbuf2[i].Left;
					pbuf3[i].Right = pbuf1[i].Right + pbuf2[i].Right;
				}
				goto gain2ch;
			}

			if (cchan1 == 2 && cchan2 == 4) {
				// avg rear chan l/r

				for (i = 0; i < count; i++) {
					pbuf3[i].Left = pbuf1[i].Left + AVG(pbuf2[i].Left, pbufrear2[i].Left);
					pbuf3[i].Right = pbuf1[i].Right + AVG(pbuf2[i].Right, pbufrear2[i].Right);
				}
				goto gain2ch;
			}

			if (cchan1 == 4 && cchan2 == 4) {
				// avg rear chan l/r

				for (i = 0; i < count; i++) {
					pbuf3[i].Left = AVG(pbuf1[i].Left, pbufrear1[i].Left) + AVG(pbuf2[i].Left, pbufrear2[i].Left);
					pbuf3[i].Right = AVG(pbuf1[i].Right, pbufrear1[i].Right) + AVG(pbuf2[i].Right, pbufrear2[i].Right);
				}
				goto gain2ch;
			}

			if (cchan1 == 2 && cchan2 == 5) {
				// avg rear chan l/r + center split into left/right

				for (i = 0; i < count; i++) {
					l = pbuf2[i].Left + ((pbufcenter2[i].Left) >> 1);
					r = pbuf2[i].Right + ((pbufcenter2[i].Left) >> 1);

					pbuf3[i].Left = pbuf1[i].Left + AVG(l, pbufrear2[i].Left);
					pbuf3[i].Right = pbuf1[i].Right + AVG(r, pbufrear2[i].Right);
				}
				goto gain2ch;
			}

			if (cchan1 == 4 && cchan2 == 5) {
				for (i = 0; i < count; i++) {
					l = pbuf2[i].Left + ((pbufcenter2[i].Left) >> 1);
					r = pbuf2[i].Right + ((pbufcenter2[i].Left) >> 1);

					pbuf3[i].Left = AVG(pbuf1[i].Left, pbufrear1[i].Left) + AVG(l, pbufrear2[i].Left);
					pbuf3[i].Right = AVG(pbuf1[i].Right, pbufrear1[i].Right) + AVG(r, pbufrear2[i].Right);
				}
				goto gain2ch;
			}

			if (cchan1 == 5 && cchan2 == 5) {
				for (i = 0; i < count; i++) {
					l = pbuf1[i].Left + ((pbufcenter1[i].Left) >> 1);
					r = pbuf1[i].Right + ((pbufcenter1[i].Left) >> 1);

					l2 = pbuf2[i].Left + ((pbufcenter2[i].Left) >> 1);
					r2 = pbuf2[i].Right + ((pbufcenter2[i].Left) >> 1);

					pbuf3[i].Left = AVG(l, pbufrear1[i].Left) + AVG(l2, pbufrear2[i].Left);
					pbuf3[i].Right = AVG(r, pbufrear1[i].Right) + AVG(r2, pbufrear2[i].Right);
				}
				goto gain2ch;
			}

		}

		// destination buffer quad - duplicate n chans up to quad

		if (cchan3 == 4) {

			// pb1 4ch		  + pb2 4ch			-> pb3 4ch
			// pb1 (2ch->4ch) + pb2 4ch			-> pb3 4ch
			// pb1 (2ch->4ch) + pb2 (2ch->4ch)	-> pb3 4ch

			if (cchan1 == 4 && cchan2 == 4) {
				// mix front -> front, rear -> rear

				for (i = 0; i < count; i++) {
					pbuf3[i].Left = pbuf1[i].Left + pbuf2[i].Left;
					pbuf3[i].Right = pbuf1[i].Right + pbuf2[i].Right;

					pbufrear3[i].Left = pbufrear1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbufrear1[i].Right + pbufrear2[i].Right;
				}
				goto gain4ch;
			}

			if (cchan1 == 2 && cchan2 == 4) {

				for (i = 0; i < count; i++) {
					// split 2 ch left ->  front left, rear left
					// split 2 ch right -> front right, rear right

					xl = pbuf1[i].Left;
					xr = pbuf1[i].Right;

					pbuf3[i].Left = xl + pbuf2[i].Left;
					pbuf3[i].Right = xr + pbuf2[i].Right;

					pbufrear3[i].Left = xl + pbufrear2[i].Left;
					pbufrear3[i].Right = xr + pbufrear2[i].Right;
				}
				goto gain4ch;
			}

			if (cchan1 == 2 && cchan2 == 2) {
				// mix l,r, split into front l, front r

				for (i = 0; i < count; i++) {
					xl = pbuf1[i].Left + pbuf2[i].Left;
					xr = pbuf1[i].Right + pbuf2[i].Right;

					pbufrear3[i].Left = pbuf3[i].Left = xl;
					pbufrear3[i].Right = pbuf3[i].Right = xr;
				}
				goto gain4ch;
			}


			if (cchan1 == 2 && cchan2 == 5) {
				for (i = 0; i < count; i++) {
					// split center of chan2 into left/right

					l2 = pbuf2[i].Left + ((pbufcenter2[i].Left) >> 1);
					r2 = pbuf2[i].Right + ((pbufcenter2[i].Left) >> 1);

					xl = pbuf1[i].Left;
					xr = pbuf1[i].Right;

					pbuf3[i].Left = xl + l2;
					pbuf3[i].Right = xr + r2;

					pbufrear3[i].Left = xl + pbufrear2[i].Left;
					pbufrear3[i].Right = xr + pbufrear2[i].Right;
				}
				goto gain4ch;
			}

			if (cchan1 == 4 && cchan2 == 5) {

				for (i = 0; i < count; i++) {
					l2 = pbuf2[i].Left + ((pbufcenter2[i].Left) >> 1);
					r2 = pbuf2[i].Right + ((pbufcenter2[i].Left) >> 1);

					pbuf3[i].Left = pbuf1[i].Left + l2;
					pbuf3[i].Right = pbuf1[i].Right + r2;

					pbufrear3[i].Left = pbufrear1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbufrear1[i].Right + pbufrear2[i].Right;
				}
				goto gain4ch;
			}

			if (cchan1 == 5 && cchan2 == 5) {
				for (i = 0; i < count; i++) {
					l = pbuf1[i].Left + ((pbufcenter1[i].Left) >> 1);
					r = pbuf1[i].Right + ((pbufcenter1[i].Left) >> 1);

					l2 = pbuf2[i].Left + ((pbufcenter2[i].Left) >> 1);
					r2 = pbuf2[i].Right + ((pbufcenter2[i].Left) >> 1);

					pbuf3[i].Left = l + l2;
					pbuf3[i].Right = r + r2;

					pbufrear3[i].Left = pbufrear1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbufrear1[i].Right + pbufrear2[i].Right;
				}
				goto gain4ch;
			}
		}

		// 5 channel destination

		if (cchan3 == 5) {
			// up convert from 2 or 4 ch buffer to 5 ch buffer: 
			// center channel is synthesized from front left, front right

			if (cchan1 == 2 && cchan2 == 2) {
				for (i = 0; i < count; i++) {
					// split 2 ch left ->  front left, center, rear left
					// split 2 ch right -> front right, center, rear right

					l = pbuf1[i].Left;
					r = pbuf1[i].Right;

					MIX_CenterFromLeftRight(ref l, ref r, ref c);

					l2 = pbuf2[i].Left;
					r2 = pbuf2[i].Right;

					MIX_CenterFromLeftRight(ref l2, ref r2, ref c2);

					pbuf3[i].Left = l + l2;
					pbuf3[i].Right = r + r2;

					pbufrear3[i].Left = pbuf1[i].Left + pbuf2[i].Left;
					pbufrear3[i].Right = pbuf1[i].Right + pbuf2[i].Right;

					pbufcenter3[i].Left = c + c2;
				}
				goto gain5ch;
			}

			if (cchan1 == 2 && cchan2 == 4) {
				for (i = 0; i < count; i++) {
					l = pbuf1[i].Left;
					r = pbuf1[i].Right;

					MIX_CenterFromLeftRight(ref l, ref r, ref c);

					l2 = pbuf2[i].Left;
					r2 = pbuf2[i].Right;

					MIX_CenterFromLeftRight(ref l2, ref r2, ref c2);

					pbuf3[i].Left = l + l2;
					pbuf3[i].Right = r + r2;

					pbufrear3[i].Left = pbuf1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbuf1[i].Right + pbufrear2[i].Right;

					pbufcenter3[i].Left = c + c2;
				}
				goto gain5ch;
			}

			if (cchan1 == 2 && cchan2 == 5) {
				for (i = 0; i < count; i++) {
					l = pbuf1[i].Left;
					r = pbuf1[i].Right;

					MIX_CenterFromLeftRight(ref l, ref r, ref c);

					pbuf3[i].Left = l + pbuf2[i].Left;
					pbuf3[i].Right = r + pbuf2[i].Right;

					pbufrear3[i].Left = pbuf1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbuf1[i].Right + pbufrear2[i].Right;

					pbufcenter3[i].Left = c + pbufcenter2[i].Left;
				}
				goto gain5ch;
			}

			if (cchan1 == 4 && cchan2 == 4) {
				for (i = 0; i < count; i++) {
					l = pbuf1[i].Left;
					r = pbuf1[i].Right;

					MIX_CenterFromLeftRight(ref l, ref r, ref c);

					l2 = pbuf2[i].Left;
					r2 = pbuf2[i].Right;

					MIX_CenterFromLeftRight(ref l2, ref r2, ref c2);

					pbuf3[i].Left = l + l2;
					pbuf3[i].Right = r + r2;

					pbufrear3[i].Left = pbufrear1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbufrear1[i].Right + pbufrear2[i].Right;

					pbufcenter3[i].Left = c + c2;
				}
				goto gain5ch;
			}


			if (cchan1 == 4 && cchan2 == 5) {
				for (i = 0; i < count; i++) {
					l = pbuf1[i].Left;
					r = pbuf1[i].Right;

					MIX_CenterFromLeftRight(ref l, ref r, ref c);

					pbuf3[i].Left = l + pbuf2[i].Left;
					pbuf3[i].Right = r + pbuf2[i].Right;

					pbufrear3[i].Left = pbufrear1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbufrear1[i].Right + pbufrear2[i].Right;

					pbufcenter3[i].Left = c + pbufcenter2[i].Left;
				}
				goto gain5ch;
			}

			if (cchan2 == 5 && cchan1 == 5) {
				for (i = 0; i < count; i++) {
					pbuf3[i].Left = pbuf1[i].Left + pbuf2[i].Left;
					pbuf3[i].Right = pbuf1[i].Right + pbuf2[i].Right;
					pbufrear3[i].Left = pbufrear1[i].Left + pbufrear2[i].Left;
					pbufrear3[i].Right = pbufrear1[i].Right + pbufrear2[i].Right;
					pbufcenter3[i].Left = pbufcenter1[i].Left + pbufcenter2[i].Left;
				}
				goto gain5ch;
			}
		}

	gain2ch:
		if (gain_out == 256)        // KDB: perf
			return;

		for (i = 0; i < count; i++) {
			pbuf3[i].Left = (pbuf3[i].Left * gain_out) >> 8;
			pbuf3[i].Right = (pbuf3[i].Right * gain_out) >> 8;
		}
		return;

	gain4ch:
		if (gain_out == 256)        // KDB: perf
			return;

		for (i = 0; i < count; i++) {
			pbuf3[i].Left = (pbuf3[i].Left * gain_out) >> 8;
			pbuf3[i].Right = (pbuf3[i].Right * gain_out) >> 8;
			pbufrear3[i].Left = (pbufrear3[i].Left * gain_out) >> 8;
			pbufrear3[i].Right = (pbufrear3[i].Right * gain_out) >> 8;
		}
		return;

	gain5ch:
		if (gain_out == 256)        // KDB: perf
			return;

		for (i = 0; i < count; i++) {
			pbuf3[i].Left = (pbuf3[i].Left * gain_out) >> 8;
			pbuf3[i].Right = (pbuf3[i].Right * gain_out) >> 8;
			pbufrear3[i].Left = (pbufrear3[i].Left * gain_out) >> 8;
			pbufrear3[i].Right = (pbufrear3[i].Right * gain_out) >> 8;
			pbufcenter3[i].Left = (pbufcenter3[i].Left * gain_out) >> 8;
		}
		return;
	}
	public void MIX_ScalePaintBuffer(int bufferIndex, int count, float fgain) {
		PortableSamplePair[]? pbuf = PaintBuffers[bufferIndex].Buf;
		PortableSamplePair[]? pbufrear = PaintBuffers[bufferIndex].BufRear;
		PortableSamplePair[]? pbufcenter = PaintBuffers[bufferIndex].BufCenter;

		int gain = (int)(256 * fgain);
		int i;

		if (gain == 256)
			return;

		if (!PaintBuffers[bufferIndex].Surround) {
			for (i = 0; i < count; i++) {
				pbuf![i].Left = (pbuf[i].Left * gain) >> 8;
				pbuf[i].Right = (pbuf[i].Right * gain) >> 8;
			}
		}
		else {
			for (i = 0; i < count; i++) {
				pbuf![i].Left = (pbuf[i].Left * gain) >> 8;
				pbuf[i].Right = (pbuf[i].Right * gain) >> 8;
				pbufrear![i].Left = (pbufrear[i].Left * gain) >> 8;
				pbufrear[i].Right = (pbufrear[i].Right * gain) >> 8;
			}

			if (PaintBuffers[bufferIndex].SurroundCenter) 
				for (i = 0; i < count; i++) 
					pbufcenter![i].Left = (pbufcenter[i].Left * gain) >> 8;
		}
	}
	public int MIX_GetCurrentPaintbufferIndex() {
		for (int i = 0; i < PaintBuffers.Count; i++)
			if (CurPaintBuffer == PaintBuffers[i].Buf)
				return i;

		return 0;
	}

	public PaintBuffer MIX_GetCurrentPaintbufferPtr() {
		int ipaint = MIX_GetCurrentPaintbufferIndex();
		Assert(ipaint < PaintBuffers.Count);
		return PaintBuffers[ipaint];
	}

	public void MIX_ClearAllPaintBuffers(int sampleCount, bool clearFilters) {
		if (PaintBuffers.Count <= 0)
			return;

		int i;
		int count = Math.Min(sampleCount, PaintBuffer.PAINTBUFFER_SIZE);

		Span<PaintBuffer> paintBuffers = PaintBuffers.AsSpan();
		for (i = 0; i < paintBuffers.Length; i++) {
			PaintBuffer paintBuffer = paintBuffers[i];
			if (paintBuffer.Buf != null)
				memset(paintBuffer.Buf[..(count + 1)], default);

			if (paintBuffer.BufRear != null)
				memset(paintBuffer.BufRear[..(count + 1)], default);

			if (paintBuffer.BufCenter != null)
				memset(paintBuffer.BufCenter[..(count + 1)], default);

			if (clearFilters) {
				memreset(paintBuffer.FilterMem.elements);
				memreset(paintBuffer.FilterMemRear.elements);
				memreset(paintBuffer.FilterMemCenter.elements);
			}
		}

		if (clearFilters)
			MIX_ResetPaintbufferFilterCounters();
	}

	private void MIX_ResetPaintbufferFilterCounters() {
		int i;
		Span<PaintBuffer> paintBuffers = PaintBuffers.AsSpan();
		for (i = 0; i < paintBuffers.Length; i++)
			paintBuffers[i].Filter = 0;
	}

	public void MIX_PaintChannels(uint endtime, bool isUnderwater) {

	}

	public void MXR_DebugShowMixVolumes() {

	}

	public void MXR_UpdateAllDuckerVolumes() {

	}
}
