using CallCosting.Api.Data;
using CallCosting.Api.Models;
using CallCosting.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<CallCostDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("CallCosting")
					  ?? "Data Source=call-costing.db"));

builder.Services.AddScoped<ICallClassifier, PrefixCallClassifier>();
builder.Services.AddScoped<ICallCostService, CallCostService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<CallCostDbContext>();
	db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
	errorApp.Run(async context =>
	{
		var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
		var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();

		if (exceptionFeature?.Error is not null)
		{
			logger.LogError(exceptionFeature.Error, "Unhandled API exception.");
		}

		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		context.Response.ContentType = "application/json";

		await context.Response.WriteAsJsonAsync(
			new ApiError("InternalServerError", "An unexpected error occurred."));
	});
});

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Content("OK"));

app.MapPut("/customers/{customerId:int}/rate-card",
	async (
		int customerId,
		UpdateRateCardsRequest? request,
		CallCostDbContext db,
		ILogger<Program> logger,
		CancellationToken cancellationToken) =>
	{
		if (customerId < 0)
		{
			logger.LogWarning("Rejected rate card update for invalid customer id {CustomerId}.", customerId);
			return Results.BadRequest(new ApiError("InvalidCustomerId", "Customer id must not be negative."));
		}

		var validationError = RateCardValidator.Validate(request);

		if (validationError is not null)
		{
			logger.LogWarning("Rejected rate card update for customer {CustomerId}: {ErrorCode}.",
				customerId,
				validationError.Code);

			return Results.BadRequest(validationError);
		}

		var rates = request!.Rates;

		var customer = await db.Customers
			.Include(c => c.Rates)
			.SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken);

		if (customer is null)
		{
			customer = new() { Id = customerId };
			db.Customers.Add(customer);
		}
		else
		{
			customer.Rates.Clear();
		}

		foreach (var rate in rates)
		{
			customer.Rates.Add(new()
			{
				CallType = rate.CallType.Trim(),
				CostPerMinute = rate.CostPerMinute ?? throw new InvalidOperationException($"Rate card was not validated - {nameof(rate.CostPerMinute)} must not be null"),
			});
		}

		await db.SaveChangesAsync(cancellationToken);

		return Results.Ok(ToRateCardResponse(customer));
	});

app.MapGet("/customers/{customerId:int}/rate-card",
	async (int customerId, ILogger<Program> logger, CallCostDbContext db, CancellationToken cancellationToken) =>
	{
		if (customerId < 0)
		{
			logger.LogWarning("Rejected rate card request for invalid customer id {CustomerId}.", customerId);
			return Results.BadRequest(new ApiError("InvalidCustomerId", "Customer id must not be negative."));
		}

		var customer = await db.Customers
			.AsNoTracking()
			.Include(c => c.Rates)
			.SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken);

		return customer is null
			? Results.NotFound(new ApiError("UnknownCustomer", $"Customer {customerId} does not have a rate card."))
			: Results.Ok(ToRateCardResponse(customer));
	});

app.MapGet("/rate-cards",
	async (CallCostDbContext db, CancellationToken cancellationToken) =>
	{
		var customers = await db.Customers
			.AsNoTracking()
			.Include(c => c.Rates)
			.OrderBy(c => c.Id)
			.ToListAsync(cancellationToken);

		return Results.Ok(customers.Select(ToRateCardResponse).ToList());
	});

app.MapPost("/calls/calculate-costs",
	async (
		[FromBody] CalculateCallCostsRequest? request,
		ICallCostService callCostService,
		ILogger<Program> logger,
		CancellationToken cancellationToken) =>
	{
		if (request?.Calls is null)
		{
			logger.LogWarning("Rejected call cost request because the calls array was missing.");
			return Results.BadRequest(new ApiError("InvalidRequest", "Request body must include a calls array."));
		}

		try
		{
			var callCosts = await callCostService.CalculateCostsAsync(request.Calls, cancellationToken);
			return Results.Ok(callCosts);
		}
		catch (CallCostException ex)
		{
			logger.LogWarning(
				ex,
				"Call cost request failed with {ErrorCode} and HTTP {StatusCode} for {CallCount} calls.",
				ex.Code,
				ex.StatusCode,
				request.Calls.Count);

			var error = new ApiError(ex.Code, ex.Message);

			return ex.StatusCode switch
			{
				StatusCodes.Status404NotFound => Results.NotFound(error),
				StatusCodes.Status422UnprocessableEntity => Results.UnprocessableEntity(error),
				_ => Results.BadRequest(error),
			};
		}
	});

app.Run();
return;

static RateCardResponse ToRateCardResponse(Customer customer)
{
	return new(
		customer.Id,
		customer.Rates
			.OrderBy(r => r.CallType)
			.Select(r => new RateResponse(r.CallType, r.CostPerMinute))
			.ToList());
}
