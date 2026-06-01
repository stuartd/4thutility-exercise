using CallCosting.Api.Services;
using Shouldly;

namespace CallCosting.Tests;

public sealed class RateCardValidatorTests
{
	[Test]
	public void Validate_AcceptsValidRateCard()
	{
		var error = RateCardValidator.Validate(new([
			new("UK", 0.05m),
			new("International", 0.25m),
		]));

		error.ShouldBeNull();
	}

	[Test]
	public void Validate_RejectsMissingRates()
	{
		var error = RateCardValidator.Validate(null);

		error.ShouldNotBeNull();
		error.Code.ShouldBe("InvalidRequest");
	}

	[Test]
	public void Validate_RejectsEmptyRateCard()
	{
		var error = RateCardValidator.Validate(new([]));

		error.ShouldNotBeNull();
		error.Code.ShouldBe("EmptyRateCard");
	}

	[Test]
	public void Validate_RejectsBlankCallType()
	{
		var error = RateCardValidator.Validate(new([
			new(" ", 0.05m),
		]));

		error.ShouldNotBeNull();
		error.Code.ShouldBe("InvalidCallType");
	}

	[Test]
	public void Validate_RejectsDuplicateCallType()
	{
		var error = RateCardValidator.Validate(new([
			new("UK", 0.05m),
			new("uk", 0.06m),
		]));

		error.ShouldNotBeNull();
		error.Code.ShouldBe("DuplicateCallType");
	}

	[Test]
	public void Validate_RejectsUnknownCallType()
	{
		var error = RateCardValidator.Validate(new([
			new("Scotland", 0.12m),
		]));

		error.ShouldNotBeNull();
		error.Code.ShouldBe("UnknownCallType");
	}

	[Test]
	public void Validate_RejectsNegativeCost()
	{
		var error = RateCardValidator.Validate(new([
			new("UK", -0.01m),
		]));

		error.ShouldNotBeNull();
		error.Code.ShouldBe("InvalidRate");
	}
}
