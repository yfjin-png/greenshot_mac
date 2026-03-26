using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui;

public partial class MainPage
{
	private sealed record EditorColorOption(string Label, ImageEditorColor Color);

	private static readonly EditorColorOption[] StrokeColorOptions =
	[
		new("Red", new ImageEditorColor(220, 53, 69)),
		new("Orange", new ImageEditorColor(213, 106, 47)),
		new("Yellow", new ImageEditorColor(173, 130, 0)),
		new("Green", new ImageEditorColor(21, 135, 92)),
		new("Blue", new ImageEditorColor(41, 107, 163)),
		new("Purple", new ImageEditorColor(108, 74, 182)),
		new("Black", new ImageEditorColor(34, 34, 34)),
		new("White", new ImageEditorColor(255, 255, 255))
	];

	private static readonly EditorColorOption[] FillColorOptions =
	[
		new("Transparent", new ImageEditorColor(0, 0, 0, 0)),
		new("Soft Yellow", new ImageEditorColor(255, 230, 91, 96)),
		new("White", new ImageEditorColor(255, 255, 255, 255)),
		new("Soft Green", new ImageEditorColor(198, 239, 221, 176)),
		new("Soft Blue", new ImageEditorColor(214, 234, 252, 196)),
		new("Soft Orange", new ImageEditorColor(255, 214, 186, 196)),
		new("Soft Red", new ImageEditorColor(255, 205, 210, 196)),
		new("Black", new ImageEditorColor(34, 34, 34, 224))
	];

	private static readonly EditorColorOption[] TextColorOptions =
	[
		new("Black", new ImageEditorColor(34, 34, 34)),
		new("White", new ImageEditorColor(255, 255, 255)),
		new("Red", new ImageEditorColor(220, 53, 69)),
		new("Green", new ImageEditorColor(21, 135, 92)),
		new("Blue", new ImageEditorColor(41, 107, 163)),
		new("Orange", new ImageEditorColor(213, 106, 47)),
		new("Purple", new ImageEditorColor(108, 74, 182))
	];

	private bool _isUpdatingEditorControls;

	private void InitializeEditorStylePanel()
	{
		EditorStrokeColorPicker.ItemsSource = StrokeColorOptions.Select(option => option.Label).ToList();
		EditorFillColorPicker.ItemsSource = FillColorOptions.Select(option => option.Label).ToList();
		EditorTextColorPicker.ItemsSource = TextColorOptions.Select(option => option.Label).ToList();
		EditorStrokeStylePicker.ItemsSource = Enum.GetNames<ImageEditorStrokeStyle>().ToList();

		EditorStrokeThicknessSlider.Value = 4d;
		EditorTextSizeSlider.Value = 24d;
		EditorStylePanel.IsVisible = false;
	}

	private void UpdateEditorPropertyPanel()
	{
		if (!_imageEditorSession.IsEditing)
		{
			EditorStylePanel.IsVisible = false;
			return;
		}

		var tool = _imageEditorSession.SelectedAnnotation?.Tool ?? _imageEditorSession.ActiveTool;
		var style = _imageEditorSession.CurrentStyle;
		var supportsStroke = ToolSupportsStroke(tool);
		var supportsFill = ToolSupportsFill(tool);
		var supportsText = ToolSupportsText(tool);
		var isInlineTextEditing = _inlineTextEditingAnnotationId.HasValue;

		EditorStylePanel.IsVisible = supportsStroke || supportsFill || supportsText;
		EditorStrokeColorPanel.IsVisible = supportsStroke;
		EditorStrokeStylePanel.IsVisible = supportsStroke;
		EditorStrokeThicknessPanel.IsVisible = supportsStroke;
		EditorFillColorPanel.IsVisible = supportsFill;
		EditorTextColorPanel.IsVisible = supportsText;
		EditorTextSizePanel.IsVisible = supportsText;
		EditorTextContentPanel.IsVisible = supportsText && !isInlineTextEditing;
		EditorFillColorLabel.Text = tool is ImageEditorTool.Text or ImageEditorTool.SpeechBubble
			? "Background color"
			: "Fill / background";
		EditorTextContentLabel.Text = tool == ImageEditorTool.SpeechBubble
			? "Callout text"
			: "Text";

		_isUpdatingEditorControls = true;
		try
		{
			SetPickerSelection(EditorStrokeColorPicker, StrokeColorOptions, style.StrokeColor);
			SetPickerSelection(EditorFillColorPicker, FillColorOptions, style.FillColor);
			SetPickerSelection(EditorTextColorPicker, TextColorOptions, style.TextColor);
			EditorStrokeStylePicker.SelectedIndex = (int)style.StrokeStyle;

			if (Math.Abs(EditorStrokeThicknessSlider.Value - style.StrokeThickness) > 0.01d)
			{
				EditorStrokeThicknessSlider.Value = style.StrokeThickness;
			}

			if (Math.Abs(EditorTextSizeSlider.Value - style.TextSize) > 0.01d)
			{
				EditorTextSizeSlider.Value = style.TextSize;
			}

			var text = _imageEditorSession.CurrentText;
			if (!string.Equals(EditorTextInput.Text, text, StringComparison.Ordinal))
			{
				EditorTextInput.Text = text;
			}

			EditorStrokeThicknessValueLabel.Text = $"{style.StrokeThickness:0.#} px";
			EditorTextSizeValueLabel.Text = $"{style.TextSize:0.#} pt";
		}
		finally
		{
			_isUpdatingEditorControls = false;
		}
	}

	private static bool ToolSupportsStroke(ImageEditorTool tool) =>
		tool is not ImageEditorTool.Image;

	private static bool ToolSupportsFill(ImageEditorTool tool) =>
		tool is ImageEditorTool.Rectangle or ImageEditorTool.Highlight or ImageEditorTool.Text or ImageEditorTool.SpeechBubble;

	private static bool ToolSupportsText(ImageEditorTool tool) =>
		tool is ImageEditorTool.Text or ImageEditorTool.SpeechBubble;

	private static void SetPickerSelection(Picker picker, IReadOnlyList<EditorColorOption> options, ImageEditorColor color)
	{
		var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Color.Equals(color));
		picker.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
	}

	private void OnEditorStrokeColorChanged(object? sender, EventArgs e)
	{
		if (_isUpdatingEditorControls || EditorStrokeColorPicker.SelectedIndex < 0 || !_imageEditorSession.IsEditing)
		{
			return;
		}

		_imageEditorSession.SetStrokeColor(StrokeColorOptions[EditorStrokeColorPicker.SelectedIndex].Color);
		UpdateEditorVisual();
	}

	private void OnEditorFillColorChanged(object? sender, EventArgs e)
	{
		if (_isUpdatingEditorControls || EditorFillColorPicker.SelectedIndex < 0 || !_imageEditorSession.IsEditing)
		{
			return;
		}

		_imageEditorSession.SetFillColor(FillColorOptions[EditorFillColorPicker.SelectedIndex].Color);
		UpdateEditorVisual();
	}

	private void OnEditorTextColorChanged(object? sender, EventArgs e)
	{
		if (_isUpdatingEditorControls || EditorTextColorPicker.SelectedIndex < 0 || !_imageEditorSession.IsEditing)
		{
			return;
		}

		_imageEditorSession.SetTextColor(TextColorOptions[EditorTextColorPicker.SelectedIndex].Color);
		UpdateEditorVisual();
	}

	private void OnEditorStrokeStyleChanged(object? sender, EventArgs e)
	{
		if (_isUpdatingEditorControls || EditorStrokeStylePicker.SelectedIndex < 0 || !_imageEditorSession.IsEditing)
		{
			return;
		}

		_imageEditorSession.SetStrokeStyle((ImageEditorStrokeStyle)EditorStrokeStylePicker.SelectedIndex);
		UpdateEditorVisual();
	}

	private void OnEditorStrokeThicknessChanged(object? sender, ValueChangedEventArgs e)
	{
		if (_isUpdatingEditorControls || !_imageEditorSession.IsEditing)
		{
			return;
		}

		_imageEditorSession.SetStrokeThickness((float)e.NewValue);
		UpdateEditorVisual();
	}

	private void OnEditorTextSizeChanged(object? sender, ValueChangedEventArgs e)
	{
		if (_isUpdatingEditorControls || !_imageEditorSession.IsEditing)
		{
			return;
		}

		_imageEditorSession.SetTextSize((float)e.NewValue);
		UpdateEditorVisual();
	}

	private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (_isUpdatingEditorControls || !_imageEditorSession.IsEditing)
		{
			return;
		}

		_imageEditorSession.SetText(e.NewTextValue ?? string.Empty);
		UpdateEditorVisual();
	}
}
