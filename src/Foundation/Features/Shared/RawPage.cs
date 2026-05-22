using EPiServer.DataAnnotations;

namespace Foundation.Features.Shared
{
    // Stub for CMS 12 RawPage content type present in foundation.episerverdata.
    // Without this registration the XML deserializer throws on import.
    [ContentType(DisplayName = "Raw Page", GUID = "77A2E483-3D15-4D33-A1F1-5FD4F0B7B8A2", Description = "")]
    public class RawPage : PageData { }
}
