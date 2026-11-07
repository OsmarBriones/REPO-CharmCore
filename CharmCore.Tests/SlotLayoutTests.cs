using CharmCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CharmCore.Tests;

[TestClass]
public class SlotLayoutTests
{
	[TestMethod]
	public void SecondsStayInsideTheUpperHalfOfThePicture()
	{
		float fromTop = SlotLayout.SecondsFromTop(60f);
		Assert.IsTrue(fromTop > 0f && fromTop < 30f);
	}

	[TestMethod]
	public void ZeroOrNegativeHeightGivesNoOffset()
	{
		Assert.AreEqual(0f, SlotLayout.SecondsFromTop(0f));
		Assert.AreEqual(0f, SlotLayout.SecondsFromTop(-5f));
	}

	[TestMethod]
	public void CompactValueIsSmallerButStillReadable()
	{
		Assert.IsTrue(SlotLayout.CompactValueFontScale < 1f);
		Assert.IsTrue(SlotLayout.CompactValueFontScale >= 0.5f);
	}
}
