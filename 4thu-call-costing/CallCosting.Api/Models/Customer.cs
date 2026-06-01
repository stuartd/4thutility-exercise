namespace CallCosting.Api.Models;

public sealed class Customer
{
	public int Id { get; set; }

	public List<CustomerCallRate> Rates { get; } = [];
}
