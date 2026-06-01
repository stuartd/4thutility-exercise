using CallCosting.Api.Data;
using CallCosting.Api.Models;
using CallCosting.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CallCosting.Tests;

public sealed class CallCostServiceTests
{
	private SqliteConnection dbconnection = null!;
	private CallCostDbContext dbcontext = null!;

	[SetUp]
	public async Task SetUp()
	{
		dbconnection = new("Data Source=:memory:");
		await dbconnection.OpenAsync();

		var options = new DbContextOptionsBuilder<CallCostDbContext>()
			.UseSqlite(dbconnection)
			.Options;

		dbcontext = new(options);
		await dbcontext.Database.EnsureCreatedAsync();
	}

	[TearDown]
	public async Task TearDown()
	{
		await dbcontext.DisposeAsync();
		await dbconnection.DisposeAsync();
	}

	[Test]
	public async Task CalculateCostsAsync_AppliesCustomerRateAndCalculatesCost()
	{
		dbcontext.Customers.Add(new()
		{
			Id = 1001,
			Rates =
			{
				new()
				{
					CallType = CallDestinationCode.Uk.CallType,
					CostPerMinute = 0.05m,
				},

				new()
				{
					CallType = CallDestinationCode.UkMobile.CallType,
					CostPerMinute = 0.08m,
				},
			},
		});
		await dbcontext.SaveChangesAsync();

		var service = new CallCostService(dbcontext, new PrefixCallClassifier());

		var callDuration = 125;
		CallDetailRecord callRecord = new(1001, DateTimeOffset.UtcNow, "+441234567890", callDuration);

		var result = await service.CalculateCostsAsync([callRecord], CancellationToken.None);

		var callCost = result.ShouldHaveSingleItem();

		// 125 seconds is billed as 3 started minutes, so 3 * £0.05 = £0.15.
		callCost.CallType.ShouldBe(CallDestinationCode.Uk.CallType);
		callCost.BillableMinutes.ShouldBe(3);
		callCost.RateApplied.ShouldBe(0.05m);
		callCost.Cost.ShouldBe(0.15m);
	}

	[Test]
	public async Task CalculateCostsAsync_ThrowsWhenDetectedCallTypeHasNoRate()
	{
		dbcontext.Customers.Add(new()
		{
			Id = 1001,
			Rates =
			{
				new() { CallType = CallDestinationCode.Uk.CallType, CostPerMinute = 0.05m },
			},
		});
		await dbcontext.SaveChangesAsync();

		var service = new CallCostService(dbcontext, new PrefixCallClassifier());

		var exception = await Should.ThrowAsync<CallCostException>(() =>
			service.CalculateCostsAsync([
				new(1001, DateTimeOffset.UtcNow, "+12025550123", 60),
			], CancellationToken.None));

		exception.Code.ShouldBe("MissingRate");
	}
}
