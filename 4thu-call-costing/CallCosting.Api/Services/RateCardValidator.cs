using CallCosting.Api.Models;

namespace CallCosting.Api.Services;

public static class RateCardValidator
{
	public static ApiError? Validate(UpdateRateCardsRequest? request)
	{
		if (request?.Rates is null)
		{
			return new("InvalidRequest", "Request body must include a rates array.");
		}

		if (request.Rates.Count == 0)
		{
			return new("EmptyRateCard", "At least one rate must be supplied.");
		}

		var seenCallTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var rate in request.Rates)
		{
			if (string.IsNullOrWhiteSpace(rate.CallType))
			{
				return new("InvalidCallType", "Rate call type is required.");
			}

			string callType = rate.CallType.Trim();

			if (!CallDestinationCode.IsSupportedCallType(callType))
			{
				return new("UnknownCallType", $"Unknown call type '{rate.CallType}'.");
			}

			// HashSet will return false if the call type already exists
			if (!seenCallTypes.Add(callType))
			{
				return new("DuplicateCallType", $"Rate card contains duplicate call type '{rate.CallType}'.");
			}

			switch (rate.CostPerMinute)
			{
				case null:
					return new("InvalidRate", "Cost per minute must be supplied.");

				case < 0:
					return new("InvalidRate", "Cost per minute cannot be negative.");

				case 0:
					return new("InvalidRate", "Cost per minute cannot be zero.");
			}
		}

		return null;
	}
}
