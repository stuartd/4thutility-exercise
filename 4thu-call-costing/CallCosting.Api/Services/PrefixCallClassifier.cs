using CallCosting.Api.Models;
using System.Text.RegularExpressions;

namespace CallCosting.Api.Services;

/// <inheritdoc/>
public sealed partial class PrefixCallClassifier : ICallClassifier
{
	/// <inheritdoc/>
	public CallDestinationCode Classify(string? destinationNumber)
	{
		if (string.IsNullOrWhiteSpace(destinationNumber))
		{
			throw new CallCostException(
				StatusCodes.Status400BadRequest,
				"InvalidDestinationNumber",
				"Destination number must have a value.");
		}

		if (!DestinationNumberRegex().IsMatch(destinationNumber))
		{
			throw new CallCostException(
				StatusCodes.Status400BadRequest,
				"InvalidDestinationNumber",
				"Destination number must start with '+' followed by digits only.");
		}

		return CallDestinationCode.GetCallDestination(destinationNumber);
	}

	[GeneratedRegex(@"^\+\d+$")]
	private static partial Regex DestinationNumberRegex();
}
