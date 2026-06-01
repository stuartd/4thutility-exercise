namespace CallCosting.Api.Models;

/// <summary>
/// Request to create or replace a customer's rate card.
/// The exercise requires storing customer-specific pricing for each supported call type.
/// </summary>
public sealed record UpdateRateCardsRequest(IReadOnlyList<RateRequest> Rates);

/// <summary>
/// A rate card line defining the cost per minute for a detected call type.
/// </summary>
public sealed record RateRequest(string CallType, decimal? CostPerMinute);

/// <summary>
/// A customer's stored rates.
/// </summary>
public sealed record RateCardResponse(int CustomerId, IReadOnlyList<RateResponse> Rates);

/// <summary>
/// A stored rate card line returned by the API.
/// </summary>
public sealed record RateResponse(string CallType, decimal CostPerMinute);

/// <summary>
/// Request to submit one or more completed call detail records for cost calculation.
/// </summary>
public sealed record CalculateCallCostsRequest(IReadOnlyList<CallDetailRecord> Calls);

/// <summary>
/// A completed call record containing the customer, destination number, call date,
/// and duration in seconds.
/// </summary>
/// <example>
/// <code>
/// {
///	    "customerId": 1001,
///	    "callDate": "2026-05-31T10:15:00Z",
///	    "destinationNumber": "+441234567890",
///	    "durationSeconds": 125
/// }
/// </code>
/// </example>
public sealed record CallDetailRecord(
	int CustomerId,
	DateTimeOffset CallDate, // Unused currently but included as would be required with rate cards that have effective dates
	string DestinationNumber,
	int? DurationSeconds);

/// <summary>
/// Costed output for a call, including the detected call type, billable minutes,
/// applied customer rate, and calculated line cost.
/// </summary>
public sealed record CallCostResponse(
	int CustomerId,
	string DestinationNumber,
	int DurationSeconds,
	string CallType,
	int BillableMinutes,
	decimal RateApplied,
	decimal Cost);

/// <summary>
/// Meaningful error response returned when validation or cost calculation fails.
/// </summary>
public sealed record ApiError(string Code, string Message);
