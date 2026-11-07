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
	public void SecondsAreAboveTheValueAndTheyDoNotOverlap()
	{
		SlotLayout.Split(0f, 60f, 40f, 0f, out LabelBox seconds, out LabelBox value);
		Assert.IsTrue(seconds.Bottom > value.Top, "the boxes must not touch");
	}

	[TestMethod]
	public void TogetherTheBoxesUseMostOfTheVisibleHeight()
	{
		SlotLayout.Split(0f, 60f, 40f, 0f, out LabelBox seconds, out LabelBox value);
		Assert.IsTrue(seconds.Height + value.Height >= 40f * 0.8f);
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
