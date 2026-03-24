using Greenshot.Maui.Core.Capabilities;

namespace Greenshot.Maui.Core.Services;

public interface IPlatformCapabilityService
{
	IReadOnlyList<CapabilityStatus> GetCapabilities();
}
