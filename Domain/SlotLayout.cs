namespace CharmCore.Domain;

/// <summary>A rectangle in screen pixels (y grows upwards): its centre and its size.</summary>
internal readonly struct LabelBox
{
	internal LabelBox(float centerX, float centerY, float width, float height)
	{
		CenterX = centerX;
		CenterY = centerY;
		Width = width;
		Height = height;
	}

	internal float CenterX { get; }

	internal float CenterY { get; }

	internal float Width { get; }

	internal float Height { get; }

	internal float Top => CenterY + Height / 2f;

	internal float Bottom => CenterY - Height / 2f;
}

/// <summary>
/// Splits the VISIBLE part of an inventory slot between the cooldown's seconds (upper box) and the stored valuable's value
/// (lower box), so both texts use as much of the slot as possible without touching each other. The slots sit at the very
/// bottom of the screen and the screen cuts them off, so the caller passes the visible rectangle (the slot clipped to the
/// screen). Each box is meant for an auto-sizing text, which then picks the largest font that fits. Game-free so it can be
/// unit tested.
/// </summary>
internal static class SlotLayout
{
	private const float WidthShare = 0.96f;
	private const float SecondsHeightShare = 0.40f;
	private const float ValueHeightShare = 0.46f;
	private const float TopMargin = 0.04f;
	private const float BottomMargin = 0.02f;

	/// <summary>
	/// Computes the two boxes inside the rectangle between <paramref name="left"/>/<paramref name="right"/> and
	/// <paramref name="bottom"/>/<paramref name="top"/> (screen pixels). A rectangle without area gives empty boxes.
	/// </summary>
	internal static void Split(float left, float right, float top, float bottom, out LabelBox seconds, out LabelBox value)
	{
		float height = top - bottom;
		float fullWidth = right - left;
		if (height <= 0f || fullWidth <= 0f)
		{
			seconds = new LabelBox(left, bottom, 0f, 0f);
			value = new LabelBox(left, bottom, 0f, 0f);
			return;
		}

		float centerX = (left + right) / 2f;
		float width = fullWidth * WidthShare;
		float secondsHeight = height * SecondsHeightShare;
		float valueHeight = height * ValueHeightShare;
		seconds = new LabelBox(centerX, top - height * TopMargin - secondsHeight / 2f, width, secondsHeight);
		value = new LabelBox(centerX, bottom + height * BottomMargin + valueHeight / 2f, width, valueHeight);
	}
}
