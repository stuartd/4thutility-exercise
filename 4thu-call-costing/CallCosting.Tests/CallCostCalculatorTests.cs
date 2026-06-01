using CallCosting.Api.Services;
using Shouldly;

namespace CallCosting.Tests;

public sealed class CallCostCalculatorTests
{
	[Test]
	public void Calculate_AppliesRateToBillableMinutes()
	{
		var result = CallCostCalculator.Calculate(125, 0.05m);

		result.BillableMinutes.ShouldBe(3);
		result.RateApplied.ShouldBe(0.05m);
		result.Cost.ShouldBe(0.15m);
	}

	[Test]
	public void Calculate_BillsZeroSecondCallAsOneMinute()
	{
		var result = CallCostCalculator.Calculate(0, 0.05m);

		result.BillableMinutes.ShouldBe(1);
		result.Cost.ShouldBe(0.05m);
	}

	[Test]
	public void Calculate_RoundsCostToTwoDecimalPlaces()
	{
		var result = CallCostCalculator.Calculate(1, 0.333m);

		result.Cost.ShouldBe(0.33m);
	}

	[Test]
	public void Calculate_RoundsMidpointsAwayFromZero()
	{
		var result = CallCostCalculator.Calculate(1, 0.005m);

		result.Cost.ShouldBe(0.01m);
	}
}
