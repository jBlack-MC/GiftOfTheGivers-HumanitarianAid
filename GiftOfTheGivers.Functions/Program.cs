using GiftOfTheGivers.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set the ConnectionStrings__DefaultConnection application setting.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
    })
    .Build();

host.Run();