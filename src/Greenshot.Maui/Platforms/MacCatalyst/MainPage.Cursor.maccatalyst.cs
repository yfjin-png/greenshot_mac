#if MACCATALYST
using AppKit;

namespace Greenshot.Maui;

public partial class MainPage
{
	partial void SetSelectionPointerCursorCore(bool isActive)
	{
		if (isActive)
		{
			NSCursor.CrosshairCursor.Set();
			return;
		}

		NSCursor.ArrowCursor.Set();
	}
}
#endif
