namespace CallCosting.Api.Models;

/// <summary>
/// Map a dialled destination number prefix to a 'country code' 
/// </summary>
public sealed record CallDestinationCode(string Prefix, string CallType)
{
	public static CallDestinationCode UkMobile => new("+447", "UK Mobile");
	public static CallDestinationCode Uk => new("+44", "UK");
	public static CallDestinationCode Usa => new("+1", "USA");
	public static CallDestinationCode Ireland => new("+353", "Ireland");

	/// <remarks>
	/// 'International' here is a fallback for when the supplied number doesn't match any of the options.
	/// This would not be the case in an actual implementation.
	/// </remarks>
	public static CallDestinationCode International => new(string.Empty, "International");

	public static bool IsSupportedCallType(string callType)
	{
		return supportedCallTypes.Contains(callType);
	}

	public static CallDestinationCode GetCallDestination(string destinationNumber)
	{
		// Given +447273123456, find the rules that match the destination number
		// and take the longest so 447 matches before 44

		var ruleMatches = prefixRules
			.Where(rule => destinationNumber.StartsWith(rule.Prefix, StringComparison.Ordinal))
			.OrderByDescending(rule => rule.Prefix.Length)
			.ToArray(); // avoiding multiple enumeration on principle, as the source of prefixRules could change to a database

		return ruleMatches.Any() ? ruleMatches.First() : International;
	}

	/// <summary>
	/// The E.164 prefix codes here are manually ordered so +447 is classified as UK Mobile before +44 is classified as UK.
	/// If no match is found it falls back to a generic 'International' code
	/// 
	/// (The E.164 standard codes avoid prefix ambiguity)
	/// </summary>
	private static readonly IReadOnlyList<CallDestinationCode> prefixRules =
	[
		UkMobile,
		Ireland,
		Uk,
		Usa,
	];

	private static readonly HashSet<string> supportedCallTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		UkMobile.CallType,
		Uk.CallType,
		Usa.CallType,
		Ireland.CallType,
		International.CallType,
	};
}
