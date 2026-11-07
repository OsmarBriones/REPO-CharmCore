using CharmCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CharmCore.Tests;

[TestClass]
public class SlotLayoutTests
{
	[TestMethod]
	public void BothBoxesStayInsideTheVisibleRectangle()
	{
		SlotLayout.Split(100f, 160f, 752f, 712f, out LabelBox seconds, out LabelBox value);
		foreach (LabelBox box in new[] { seconds, value })
		{
			Assert.IsTrue(box.Bottom >= 712f - 0.001f, "below the visible area");
			Assert.IsTrue(box.Top <= 752f + 0.001f, "above the visible area");
			Assert.IsTrue(box.CenterX - box.Width / 2f >= 100f - 0.001f);
			Assert.IsTrue(box.CenterX + box.Width / 2f <= 160f + 0.001f);
		}
	}

	[TestMethod]
	public void TheDigitsOfBothTextsDoNotTouch()
	{
		foreach (float height in new[] { 20f, 44f, 100f })
		{
			SlotLayout.Split(0f, 60f, height, 0f, out LabelBox seconds, out LabelBox value);

			// The boxes may overlap, but the digits (about half of a box's height) must stay apart.
			float glyphHeight = seconds.Height * SlotLayout.GlyphHeightShare;
			float centresApart = seconds.CenterY - value.CenterY;
			Assert.IsTrue(centresApart >= glyphHeight + 0.5f, $"digits touch for a slot {height} high");
		}
	}

	[TestMethod]
	public void SecondsAreAboveTheValue()
	{
		SlotLayout.Split(0f, 60f, 44f, 0f, out LabelBox seconds, out LabelBox value);
		Assert.IsTrue(seconds.CenterY > value.CenterY);
	}

	[TestMethod]
	public void TheBoxesReachTowardsTheEdgesOfTheVisibleHeight()
	{
		SlotLayout.Split(0f, 60f, 44f, 0f, out LabelBox seconds, out LabelBox value);
		Assert.IsTrue(seconds.Top >= 44f * 0.9f);
		Assert.IsTrue(value.Bottom <= 44f * 0.1f);
		Assert.IsTrue(seconds.Height >= 44f * 0.5f);
	}

	[TestMethod]
	public void ARectangleWithoutAreaGivesEmptyBoxes()
	{
		SlotLayout.Split(0f, 0f, 10f, 10f, out LabelBox seconds, out LabelBox value);
		Assert.AreEqual(0f, seconds.Width);
		Assert.AreEqual(0f, value.Height);
		SlotLayout.Split(0f, 50f, -5f, 20f, out seconds, out value);
		Assert.AreEqual(0f, seconds.Height);
		Assert.AreEqual(0f, value.Width);
	}
}
