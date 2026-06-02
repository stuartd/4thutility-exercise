# 4th Utility Phone Call Costing Exercise

An ASP.NET Core API for storing customer rate cards and calculating line-level call costs from call detail records.

## Terminology

As per the exercise notes, the call country code is referred to as the "call type" in the API, data model, and this document.

## Stack

Built with .NET 10, ASP.NET Core Minimal APIs, EF Core, SQLite, NUnit, and Shouldly.

## Run the application

From the repository root:

```
dotnet run --project .\4thu-call-costing\CallCosting.Api\CallCosting.Api.csproj
```

The API creates a local persistent SQLite database file named `call-costing.db` on startup.

## Testing

Open `4thu-call-costing\CallCosting.Api\CallCosting.Api.http` in Visual Studio to run example requests against the application.

## Run the application tests

From the repository root:

```
dotnet test .\4thu-call-costing\4thu-call-costing.slnx
```

## Rules and Behaviour

- Number prefix matching is done using 'longest first' order, so `+447` is classified as 'UK Mobile' rather than using the `+44` 'UK' rule.
- Calls use whole-second durations and are billed per started minute.
- Costs are rounded to two decimal places.
- A single cost calculation request may include calls for different customers.
- Batch cost calculation fails the whole request if any of the supplied calls are invalid.

## Assumptions

- Rate cards represent current active pricing only; previous rates are out of scope.
- Call types are fixed reference data for this exercise.
- Calls with a duration of zero seconds are still billed as one minute, as the call was connected just for a moment.
- IDs can be zero.

## Design Notes

Unit tests cover the calculation and validation rules. Integration tests cover the public endpoints using `WebApplicationFactory` with in-memory SQLite.

The API uses a file-backed SQLite database by default, so rate-card data persists between normal application runs. Integration tests replace that with in-memory SQLite so test data is isolated and does not persist.

For this exercise, the customer rate-card table represents the current active rate card only. That is why each customer can have only one active rate line per call type. A production system that needs rate-card history would usually model versions or effective date ranges.

Call types are treated as fixed reference data for the exercise. A production implementation would store them in a lookup table with stable identifiers and expose display names separately.

## Potential enhancements

- Add EF migrations and environment-specific database configuration.
- Add rate-card versions or effective date ranges for historical pricing.
- Support partial success for batch cost calculation if the business needs per-line errors.
- Add structured logging and request correlation.
- Version the API
- Increase resilience and detection of malformed inputs
