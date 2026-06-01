using CallCosting.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CallCosting.Api.Data;

public sealed class CallCostDbContext(DbContextOptions<CallCostDbContext> options) : DbContext(options)
{
	public DbSet<Customer> Customers => Set<Customer>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// In this implementation a customer owns its rate-card lines so applying cascade delete keeps those lines in step
		// when a customer or replaced rate card is removed.
		modelBuilder.Entity<Customer>(entity =>
		{
			entity.HasKey(customer => customer.Id);

			entity.HasMany(customer => customer.Rates)
				.WithOne()
				.HasForeignKey(rate => rate.CustomerId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<CustomerCallRate>(entity =>
		{
			entity.HasKey(rate => rate.Id);

			// This table represents the customer's current rate card, so there can only be
			// one active line per call type. A historical model would version these rows.
			entity.HasIndex(rate => new { rate.CustomerId, rate.CallType }).IsUnique();

			entity.Property(rate => rate.CallType).HasMaxLength(64).IsRequired();
		});
	}
}
