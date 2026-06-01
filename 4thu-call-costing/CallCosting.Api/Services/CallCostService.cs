using CallCosting.Api.Data;
using CallCosting.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CallCosting.Api.Services;

public interface ICallCostService
{
	/// <summary>
	/// Calculates line-level costs for call records.
	/// </summary>
	Task<IReadOnlyList<CallCostResponse>> CalculateCostsAsync(
		IReadOnlyList<CallDetailRecord> calls,
		CancellationToken cancellationToken);
}

/// <summary>
/// Calculates line-level costs for submitted call detail records by applying each customer's stored pricing.
/// <br></br><br></br>
/// Applies the costing rules: validate call records, detect call type,
/// find the customer's matching rate, bill per started minute, and calculate cost.
/// </summary>
public sealed class CallCostService(
	CallCostDbContext db,
	ICallClassifier classifier) : ICallCostService
{
	/// <inheritdoc />
	public async Task<IReadOnlyList<CallCostResponse>> CalculateCostsAsync(
		IReadOnlyList<CallDetailRecord> calls,
		CancellationToken cancellationToken)
	{
		// Spec: "Ability to submit **one or more call records** for cost calculation"
		if (calls.Count == 0)
		{
			throw new CallCostException(
				StatusCodes.Status400BadRequest,
				"EmptyCallBatch",
				"At least one call record must be supplied.");
		}

		var customerIds = calls.Select(call => call.CustomerId).Distinct().ToList();

		var customers = await db.Customers
			.AsNoTracking()
			.Include(customer => customer.Rates)
			.Where(customer => customerIds.Contains(customer.Id))
			.ToDictionaryAsync(customer => customer.Id, cancellationToken);

		var costedCalls = new List<CallCostResponse>(calls.Count);

		for (var index = 0; index < calls.Count; index++)
		{
			var call = calls[index];
			ValidateCall(call, index);

			// Have to do this because DurationSeconds is nullable, to catch if it was misspelled
			int durationSeconds = call.DurationSeconds ?? throw new InvalidOperationException("Call duration was not validated");

			if (!customers.TryGetValue(call.CustomerId, out var customer))
			{
				throw new CallCostException(
					StatusCodes.Status404NotFound,
					"UnknownCustomer",
					$"Call at index {index} of parameter {nameof(calls)} references an unknown customer - {call.CustomerId}.");
			}

			var callDestinationCode = classifier.Classify(call.DestinationNumber);

			var rate = customer.Rates.SingleOrDefault(r =>
				string.Equals(r.CallType, callDestinationCode.CallType, StringComparison.OrdinalIgnoreCase));

			if (rate is null)
			{
				throw new CallCostException(
					StatusCodes.Status422UnprocessableEntity,
					"MissingRate",
					$"Customer {call.CustomerId} has no rate for call type '{callDestinationCode.CallType}'.");
			}

			var calculation = CallCostCalculator.Calculate(durationSeconds, rate.CostPerMinute);

			costedCalls.Add(new(
				call.CustomerId,
				call.DestinationNumber,
				durationSeconds,
				callDestinationCode.CallType,
				calculation.BillableMinutes,
				calculation.RateApplied,
				calculation.Cost));
		}

		return costedCalls;
	}

	private static void ValidateCall(CallDetailRecord callDetail, int index)
	{
		if (callDetail.CustomerId < 0)
		{
			throw new CallCostException(
				StatusCodes.Status400BadRequest,
				"InvalidCustomerId",
				$"Call at index {index} has a negative customer id.");
		}

		if (callDetail.DurationSeconds is null or < 0)
		{
			throw new CallCostException(
				StatusCodes.Status400BadRequest,
				"InvalidDuration",
				$"Call at index {index} has a null or negative duration.");
		}
	}
}
