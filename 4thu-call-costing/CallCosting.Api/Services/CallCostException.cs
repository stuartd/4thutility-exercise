namespace CallCosting.Api.Services;

/// <summary>
/// Exception used to turn call cost validation failures into sensible HTTP status codes
/// and meaningful error responses, as required by the exercise.
/// </summary>
public sealed class CallCostException(int statusCode, string code, string message) : Exception(message)
{
	public int StatusCode { get; } = statusCode;

	public string Code { get; } = code;
}
