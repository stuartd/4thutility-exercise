using CallCosting.Api.Services;
using Shouldly;

namespace CallCosting.Tests;

public sealed class BillingCalculatorTests
{
	[TestCase(0, 1)]
	[TestCase(1, 1)]
	[TestCase(60, 1)]
	[TestCase(61, 2)]
	[TestCase(125, 3)]
	public void GetBillableMinutesForCallDuration(int durationSeconds, int expectedMinutes)
	{
		int result = BillingCalculator.GetBillableMinutes(durationSeconds);

		result.ShouldBe(expectedMinutes);
	}

	[Test]
	public void BillableMinutesCannotBeNegative()
	{
		Should.Throw<ArgumentOutOfRangeException>(() => BillingCalculator.GetBillableMinutes(-1));
	}
}
