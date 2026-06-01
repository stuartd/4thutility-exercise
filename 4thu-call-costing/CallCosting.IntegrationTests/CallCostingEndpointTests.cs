using CallCosting.Api.Models;
using CallCosting.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace CallCosting.IntegrationTests;

public sealed class CallCostingEndpointTests
{
	private CallCostingTestsApiFactory factory = null!;
	private HttpClient client = null!;

	[SetUp]
	public void SetUp()
	{
		factory = new();
		client = factory.CreateClient();
	}

	[TearDown]
	public void TearDown()
	{
		client.Dispose();
		factory.Dispose();
	}

	[Test]
	public async Task RootEndpointReturnsOk()
	{
		var response = await client.GetAsync("/");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
	}

	[Test]
	public async Task RateCardCanBeStoredAndRetrievedFromDocumentedEndpoint()
	{
		var response = await client.PutAsJsonAsync("/customers/1001/rate-card", new
		{
			rates = new[]
			{
				new { callType = "UK", costPerMinute = 0.05m },
				new { callType = "International", costPerMinute = 0.25m },
			},
		});

		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		var retrievedResponse = await client.GetAsync("/customers/1001/rate-card");

		retrievedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

		var rateCard = await retrievedResponse.Content.ReadFromJsonAsync<RateCardResponse>();

		rateCard.ShouldNotBeNull();
		rateCard.CustomerId.ShouldBe(1001);
		rateCard.Rates.Count.ShouldBe(2);
		rateCard.Rates.ShouldContain(rate => rate.CallType == "UK" && rate.CostPerMinute == 0.05m);
	}

	[Test]
	public async Task RateCardRejectsDuplicateCallTypes()
	{
		var response = await client.PutAsJsonAsync("/customers/1001/rate-card", new
		{
			rates = new[]
			{
				new { callType = "UK", costPerMinute = 0.05m },
				new { callType = "UK", costPerMinute = 0.06m },
			},
		});

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		var error = await response.Content.ReadFromJsonAsync<ApiError>();

		error.ShouldNotBeNull();
		error.Code.ShouldBe("DuplicateCallType");
	}

	[Test]
	public async Task RateCardRejectsUnknownCallType()
	{
		var response = await client.PutAsJsonAsync("/customers/1001/rate-card", new
		{
			rates = new[]
			{
				new { callType = "Scotland", costPerMinute = 0.12m },
			},
		});

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		var error = await response.Content.ReadFromJsonAsync<ApiError>();

		error.ShouldNotBeNull();
		error.Code.ShouldBe("UnknownCallType");
	}

	[Test]
	public async Task RateCardsCanBeListed()
	{
		await client.PutAsJsonAsync("/customers/1002/rate-card", new
		{
			rates = new[]
			{
				new { callType = "UK", costPerMinute = 0.08m },
				new { callType = "International", costPerMinute = 0.30m },
			},
		});

		await client.PutAsJsonAsync("/customers/1001/rate-card", new
		{
			rates = new[]
			{
				new { callType = "UK", costPerMinute = 0.05m },
				new { callType = "International", costPerMinute = 0.25m },
			},
		});

		var response = await client.GetAsync("/rate-cards");

		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		var rateCards = await response.Content.ReadFromJsonAsync<IReadOnlyList<RateCardResponse>>();

		rateCards.ShouldNotBeNull();
		rateCards.Count.ShouldBe(2);
		rateCards[0].CustomerId.ShouldBe(1001);
		rateCards[1].CustomerId.ShouldBe(1002);
		rateCards[0].Rates.ShouldContain(rate => rate.CallType == "UK" && rate.CostPerMinute == 0.05m);
	}

	[Test]
	public async Task CalculateCostsReturnsCostedCallLines()
	{
		await client.PutAsJsonAsync("/customers/1001/rate-card", new
		{
			rates = new[]
			{
				new { callType = "UK", costPerMinute = 0.05m },
				new { callType = "International", costPerMinute = 0.25m },
			},
		});

		var response = await client.PostAsJsonAsync("/calls/calculate-costs", new
		{
			calls = new[]
			{
				new
				{
					customerId = 1001,
					callDate = DateTimeOffset.UtcNow,
					destinationNumber = "+441234567890",
					durationSeconds = 125,
				},
			},
		});

		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		var callCosts = await response.Content.ReadFromJsonAsync<IReadOnlyList<CallCostResponse>>();
		var callCost = callCosts.ShouldNotBeNull().ShouldHaveSingleItem();

		callCost.CustomerId.ShouldBe(1001);
		callCost.DestinationNumber.ShouldBe("+441234567890");
		callCost.CallType.ShouldBe("UK");
		callCost.BillableMinutes.ShouldBe(3);
		callCost.RateApplied.ShouldBe(0.05m);
		callCost.Cost.ShouldBe(0.15m);
	}

	[Test]
	public async Task CalculateCostsRejectsNegativeDuration()
	{
		await client.PutAsJsonAsync("/customers/1001/rate-card", new
		{
			rates = new[]
			{
				new { callType = "UK", costPerMinute = 0.05m },
			},
		});

		var response = await client.PostAsJsonAsync("/calls/calculate-costs", new
		{
			calls = new[]
			{
				new
				{
					customerId = 1001,
					callDate = DateTimeOffset.UtcNow,
					destinationNumber = "+441234567890",
					durationSeconds = -1,
				},
			},
		});

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		var error = await response.Content.ReadFromJsonAsync<ApiError>();

		error.ShouldNotBeNull();
		error.Code.ShouldBe("InvalidDuration");
	}

	[Test]
	public async Task UnexpectedErrorsReturnInternalServerError()
	{
		await using var errorFactory = new CallCostingTestsApiFactory(services =>
		{
			services.RemoveAll<ICallCostService>();
			services.AddScoped<ICallCostService, ThrowingCallCostService>();
		});

		using var errorClient = errorFactory.CreateClient();

		var response = await errorClient.PostAsJsonAsync("/calls/calculate-costs", new
		{
			calls = new[]
			{
				new
				{
					customerId = 1001,
					callDate = DateTimeOffset.UtcNow,
					destinationNumber = "+441234567890",
					durationSeconds = 125,
				},
			},
		});

		response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

		var error = await response.Content.ReadFromJsonAsync<ApiError>();

		error.ShouldNotBeNull();
		error.Code.ShouldBe("InternalServerError");
	}

	private sealed class ThrowingCallCostService : ICallCostService
	{
		public Task<IReadOnlyList<CallCostResponse>> CalculateCostsAsync(
			IReadOnlyList<CallDetailRecord> calls,
			CancellationToken cancellationToken)
		{
			throw new InvalidOperationException("Test exception.");
		}
	}
}
