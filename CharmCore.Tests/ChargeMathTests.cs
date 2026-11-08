using CharmCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CharmCore.Tests;

[TestClass]
public class ChargeMathTests
{
	[TestMethod]
	public void ChargeGrowsLinearlyUntilFull()
	{
		Assert.AreEqual(0f, ChargeMath.Fraction(10f, 10f, 240f));
		Assert.AreEqual(0.5f, ChargeMath.Fraction(130f, 10f, 240f), 0.0001f);
		Assert.AreEqual(1f, ChargeMath.Fraction(250f, 10f, 240f));
		Assert.AreEqual(1f, ChargeMath.Fraction(5000f, 10f, 240f));
	}

	[TestMethod]
	public void AStartInTheFutureCountsAsJustStarted()
	{
		Assert.AreEqual(0f, ChargeMath.Fraction(5f, 10f, 240f));
	}

	[TestMethod]
	public void NoChargeTimeMeansAlwaysFull()
	{
		Assert.AreEqual(1f, ChargeMath.Fraction(0f, 0f, 0f));
		Assert.AreEqual(1f, ChargeMath.Fraction(0f, 0f, -3f));
	}
}
