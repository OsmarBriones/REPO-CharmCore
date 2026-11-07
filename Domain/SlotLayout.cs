namespace CharmCore.Domain;

/// <summary>
/// Layout numbers for a slot that shows both the stored valuable's value (Pockets, near the lower edge) and a cooldown's seconds
/// (CharmCore). The slot is tiny, so while a cooldown runs the value gets smaller (it keeps its place: the slots are at the very
/// bottom of the screen and anything lower is cut off) and the seconds go to the upper edge. Game-free so it can be unit tested.
/// </summary>
internal static class SlotLayout
{
	/// <summary>Share of its normal size the value label has while a cooldown is shown.</summary>
	internal const float CompactValueFontScale = 0.75f;

	/// <summary>Share of the template font size used for the cooldown seconds.</summary>
	internal const float SecondsFontScale = 0.5f;

	private const float EdgeMargin = 0.22f;

	/// <summary>Distance of the seconds' centre from the top edge of the picture, for a picture of the given height.</summary>
	internal static float SecondsFromTop(float pictureHeight)
	{
		return pictureHeight <= 0f ? 0f : pictureHeight * EdgeMargin;
	}
}
