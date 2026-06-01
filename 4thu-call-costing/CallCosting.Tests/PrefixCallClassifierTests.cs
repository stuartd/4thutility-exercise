using CallCosting.Api.Models;
using CallCosting.Api.Services;
using Shouldly;

namespace CallCosting.Tests;

public sealed class PrefixCallClassifierTests
{
	private readonly PrefixCallClassifier classifier = new();

	[TestCaseSource(nameof(DestinationCodeTestCases))]
	public void Classify_DetectsCallTypeFromPrefix(string destinationNumber, CallDestinationCode destinationCode)
	{
		var result = classifier.Classify(destinationNumber);

		result.ShouldBe(destinationCode);
	}

	[Test]
	public void Classify_PrioritisesUkMobileBeforeUk()
	{
		var result = classifier.Classify("+447700900123");

		result.ShouldBe(CallDestinationCode.UkMobile);
	}

	[TestCase("")]
	[TestCase(null)]
	public void Classify_RejectsEmptyNumber(string? destinationNumber)
	{
		var exception = Should.Throw<CallCostException>(() => classifier.Classify(destinationNumber));

		exception.Code.ShouldBe("InvalidDestinationNumber");
	}

	[TestCase("441234567890")] // no loading +
	[TestCase("+44 1234567890")] // space
	[TestCase("+44ABC")] // letters
	public void Classify_RejectsMalformedNumber(string destinationNumber)
	{
		var exception = Should.Throw<CallCostException>(() => classifier.Classify(destinationNumber));

		exception.Code.ShouldBe("InvalidDestinationNumber");
	}

	private static IEnumerable<TestCaseData> DestinationCodeTestCases()
	{
		yield return new TestCaseData("+447700900123", CallDestinationCode.UkMobile).SetName("UK mobile number");

		yield return new TestCaseData("+441234567890", CallDestinationCode.Uk).SetName("UK landline number");

		yield return new TestCaseData("+12025550123", CallDestinationCode.Usa).SetName("USA number");

		yield return new TestCaseData("+35315550123", CallDestinationCode.Ireland).SetName("Ireland number");

		yield return new TestCaseData("+33123456789", CallDestinationCode.International).SetName("International number (default)");
	}
}
