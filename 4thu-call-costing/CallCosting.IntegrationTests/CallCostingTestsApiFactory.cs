using CallCosting.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CallCosting.IntegrationTests;

internal sealed class CallCostingTestsApiFactory : WebApplicationFactory<Program>
{
	private readonly SqliteConnection connection = new("Data Source=:memory:");
	private readonly Action<IServiceCollection>? configureServices;

	public CallCostingTestsApiFactory(Action<IServiceCollection>? configureServices = null)
	{
		this.configureServices = configureServices;
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureServices(services =>
		{
			connection.Open();

			services.RemoveAll<DbContextOptions<CallCostDbContext>>();
			services.AddDbContext<CallCostDbContext>(options => options.UseSqlite(connection));
			configureServices?.Invoke(services);
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		connection.Dispose();
	}
}
