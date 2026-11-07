using CharmCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CharmCore.Tests;

[TestClass]
public class SlotLayoutTests
{
	[TestMethod]
	public void ValueAndSecondsStayInsideThePictureAndApart()
	{
		float height = 60f;
		float valueFromBottom = SlotLayout.ValueFromBottom(height);
		float secondsFromTop = SlotLayout.SecondsFromTop(height);
		Assert.IsTrue(valueFromBottom > 0f && valueFromBottom < height / 2f);
		Assert.IsTrue(secondsFromTop > 0f && secondsFromTop < height / 2f);

		// Distance between the two text centres: well over the height of one small line of text.
		float apart = height - valueFromBottom - secondsFromTop;
		Assert.IsTrue(apart > height * 0.5f);
	}

	[TestMethod]
	public void ZeroOrNegativeHeightGivesNoOffset()
	{
		Assert.AreEqual(0f, SlotLayout.ValueFromBottom(0f));
		Assert.AreEqual(0f, SlotLayout.SecondsFromTop(-5f));
	}

	[TestMethod]
	public void CompactValueIsSmallerButStillReadable()
	{
		Assert.IsTrue(SlotLayout.CompactValueFontScale < 1f);
		Assert.IsTrue(SlotLayout.CompactValueFontScale >= 0.5f);
	}
}
