using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.Audio;

public interface IAudioStreamEvent
{
	int StreamRequestData(Span<byte> buffer, int bytesRequested, int offset);
}
