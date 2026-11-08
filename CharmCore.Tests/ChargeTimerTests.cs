using CharmCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CharmCore.Tests;

[TestClass]
public class ChargeTimerTests
{
	[TestMethod]
	public void ChargesLinearlyWhileStoredUntilFull()
	{
		ChargeTimer timer = default;
		timer.Store(10f);
		Assert.AreEqual(0f, timer.Fraction(10f, 240f));
		Assert.AreEqual(0.5f, timer.Fraction(130f, 240f), 0.0001f);
		Assert.AreEqual(1f, timer.Fraction(250f, 240f));
		Assert.AreEqual(1f, timer.Fraction(5000f, 240f));
	}

	[TestMethod]
	public void TakingItOutKeepsTheChargeAndStopsTheGrowth()
	{
		ChargeTimer timer = default;
		timer.Store(0f);
		timer.Remove(60f);
		Assert.AreEqual(60f, timer.Seconds(60f));
		Assert.AreEqual(60f, timer.Seconds(1000f), "it must not charge outside a pocket");
	}

	[TestMethod]
	public void AnotherPlayerStoringItContinuesFromTheKeptCharge()
	{
		ChargeTimer timer = default;
		timer.Store(0f);
		timer.Remove(60f);
		timer.Store(500f);
		Assert.AreEqual(60f, timer.Seconds(500f));
		Assert.AreEqual(90f, timer.Seconds(530f), 0.0001f);
		timer.Remove(560f);
		timer.Store(900f);
		Assert.AreEqual(120f, timer.Seconds(900f), 0.0001f);
	}

	[TestMethod]
	public void SpendingResetsToZeroAndKeepsChargingWhileStored()
	{
		ChargeTimer timer = default;
		timer.Store(0f);
		timer.Spend(100f);
		Assert.AreEqual(0f, timer.Seconds(100f));
		Assert.AreEqual(20f, timer.Seconds(120f), 0.0001f);
	}

	[TestMethod]
	public void SpendingWhileOutOfAPocketLeavesItEmptyUntilStoredAgain()
	{
		ChargeTimer timer = default;
		timer.Store(0f);
		timer.Remove(50f);
		timer.Spend(60f);
		Assert.AreEqual(0f, timer.Seconds(200f));
		timer.Store(300f);
		Assert.AreEqual(10f, timer.Seconds(310f), 0.0001f);
	}

	[TestMethod]
	public void StoringTwiceOrRemovingTwiceDoesNotDoubleCount()
	{
		ChargeTimer timer = default;
		timer.Store(0f);
		timer.Store(30f);
		timer.Remove(60f);
		timer.Remove(90f);
		Assert.AreEqual(60f, timer.Seconds(100f), 0.0001f);
	}

	[TestMethod]
	public void NoChargeTimeMeansAlwaysFull()
	{
		ChargeTimer timer = default;
		Assert.AreEqual(1f, timer.Fraction(0f, 0f));
		Assert.AreEqual(1f, timer.Fraction(0f, -3f));
	}
}
