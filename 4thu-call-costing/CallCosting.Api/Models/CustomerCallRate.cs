namespace CallCosting.Api.Models;

public sealed class CustomerCallRate
{
	public int Id { get; set; }

	public int CustomerId { get; set; }

	public required string CallType { get; set; }

	public decimal CostPerMinute { get; set; }
}
