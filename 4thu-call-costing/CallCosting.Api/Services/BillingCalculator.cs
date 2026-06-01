namespace CallCosting.Api.Services;

/// <summary>
/// Calculates billable duration using the exercise rule that calls are billed per started minute.
/// </summary>
public static class BillingCalculator
{
	/// <summary>
	/// Converts a duration in seconds to billable minutes: 1-60 seconds is 1 minute,
	/// 61-120 seconds is 2 minutes, and so on.
	/// </summary>
	/// <remarks>
	/// Conceptually a call could last for 0 seconds (ie 'less than 1 second')
	/// which isn't covered by the spec - however the call has been placed, and so should be billed.
	/// </remarks>
	public static int GetBillableMinutes(int durationSeconds)
	{
		if (durationSeconds < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration cannot be less than zero.");
		}

		if (durationSeconds == 0)
		{
			// If this is the case, then, bill for one minute
			return 1;
		}

		return (int)Math.Ceiling(durationSeconds / 60m);
	}
}
