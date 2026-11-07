using CharmCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CharmCore.Tests;

[TestClass]
public class DomainTests
{
	[TestMethod]
	public void ValueLost_IsThePercentOfTheOriginalValue()
	{
		Assert.AreEqual(200f, WearMath.ValueLost(1000f, 1000f, 20f, 1f));
	}

	[TestMethod]
	public void ValueLost_ScalesWithTheMultiplier()
	{
		Assert.AreEqual(400f, WearMath.ValueLost(1000f, 1000f, 20f, 2f));
		Assert.AreEqual(100f, WearMath.ValueLost(1000f, 1000f, 20f, 0.5f));
	}

	[TestMethod]
	public void ValueLost_IsAtLeastOne()
	{
		Assert.AreEqual(1f, WearMath.ValueLost(100f, 100f, 0.1f, 1f));
	}

	[TestMethod]
	public void ValueLost_NeverExceedsWhatIsLeft()
	{
		Assert.AreEqual(50f, WearMath.ValueLost(1000f, 50f, 20f, 1f));
	}

	[TestMethod]
	public void ValueLost_IsZeroForNothingToWear()
	{
		Assert.AreEqual(0f, WearMath.ValueLost(0f, 0f, 20f, 1f));
		Assert.AreEqual(0f, WearMath.ValueLost(1000f, 1000f, 0f, 1f));
		Assert.AreEqual(0f, WearMath.ValueLost(1000f, 1000f, 20f, 0f));
	}

	[TestMethod]
	public void CooldownFraction_RunsFromOneToZero()
	{
		Assert.AreEqual(1f, WearMath.CooldownFraction(3f, 3f));
		Assert.AreEqual(0.5f, WearMath.CooldownFraction(1.5f, 3f));
		Assert.AreEqual(0f, WearMath.CooldownFraction(0f, 3f));
		Assert.AreEqual(0f, WearMath.CooldownFraction(1f, 0f));
	}

	[TestMethod]
	public void Cooldown_BlocksUntilItEnds()
	{
		CooldownTimer timer = default;
		Assert.IsTrue(timer.IsReady(0f));
		timer.Start(10f, 3f);
		Assert.IsFalse(timer.IsReady(11f));
		Assert.AreEqual(2f, timer.Remaining(11f), 0.0001f);
		Assert.IsTrue(timer.IsReady(13f));
	}

	[TestMethod]
	public void Cooldown_OfZeroSecondsNeverBlocks()
	{
		CooldownTimer timer = default;
		timer.Start(10f, 0f);
		Assert.IsTrue(timer.IsReady(10f));
		Assert.AreEqual(0f, timer.Total);
	}

	[TestMethod]
	public void Cooldown_ResetMakesItReady()
	{
		CooldownTimer timer = default;
		timer.Start(10f, 3f);
		timer.Reset();
		Assert.IsTrue(timer.IsReady(10f));
	}
}
