namespace Greenshot.Maui;

public partial class MainPage
{
	private bool _isSelectionPointerCursorActive;

	partial void SetSelectionPointerCursorCore(bool isActive);

	private void SetSelectionPointerCursor(bool isActive)
	{
		if (_isSelectionPointerCursorActive == isActive)
		{
			return;
		}

		SetSelectionPointerCursorCore(isActive);
		_isSelectionPointerCursorActive = isActive;
	}

	protected override void OnDisappearing()
	{
		SetSelectionPointerCursor(false);
		base.OnDisappearing();
	}
}
