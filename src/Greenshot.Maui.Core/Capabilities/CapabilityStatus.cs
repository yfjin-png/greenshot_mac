namespace Greenshot.Maui.Core.Capabilities;

public sealed record CapabilityStatus(
	GreenshotCapability Capability,
	string Title,
	bool IsReady,
	string Detail,
	string AccentHex)
{
	public string StateLabel => IsReady ? "Ready" : "Planned";

	public string CardBackgroundHex => IsReady ? "#E8FFF1" : "#FFF4E7";
}
