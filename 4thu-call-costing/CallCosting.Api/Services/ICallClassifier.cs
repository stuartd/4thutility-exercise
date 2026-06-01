using CallCosting.Api.Models;

namespace CallCosting.Api.Services;

/// <summary>
/// Determines the call destination from the destination number.
/// </summary>
public interface ICallClassifier
{
	/// <summary>
	/// Classifies a destination number into one of the supported call destinations.
	/// </summary>
	CallDestinationCode Classify(string? destinationNumber);
}
