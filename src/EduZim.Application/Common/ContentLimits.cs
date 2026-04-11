using EduZim.Application.Common.Configuration;
using EduZim.Domain.Enums;

namespace EduZim.Application.Common;

public static class ContentLimits
{
    public static long MaxBytesFor(ContentType type, ContentStorageOptions options) =>
        type switch
        {
            ContentType.Video => options.MaxVideoBytes,
            ContentType.Audio => options.MaxAudioBytes,
            _ => options.MaxOtherBytes,
        };
}
