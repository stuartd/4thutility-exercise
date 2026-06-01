namespace CallCosting.Api.Services;

public sealed record CallCostCalculation(
	int BillableMinutes,
	decimal RateApplied,
	decimal Cost);

public static class CallCostCalculator
{
	public static CallCostCalculation Calculate(int durationSeconds, decimal costPerMinute)
	{
		int billableMinutes = BillingCalculator.GetBillableMinutes(durationSeconds);
		decimal cost = decimal.Round(billableMinutes * costPerMinute, 2, MidpointRounding.AwayFromZero);

		return new(billableMinutes, costPerMinute, cost);
	}
}
