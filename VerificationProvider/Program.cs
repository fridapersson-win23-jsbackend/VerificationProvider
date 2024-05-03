using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using VerificationProvider.Data.Contexts;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddDbContext<DataContext>(x => x.UseSqlServer(Environment.GetEnvironmentVariable("SqlServer")));
        
    })
    .Build();


using (var scope = host.Services.CreateScope())
{
    try
    {
        //uppdaterar databasen om migration finns, eftersom vi inte kan köra update-database eftersom vi kör en enviroment variabel i azure functions
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var migrations = context.Database.GetPendingMigrations();
        if (migrations != null && migrations.Any())
        {
            context.Database.Migrate();
        }
    }
    catch(Exception ex)
    {
        Debug.WriteLine($"ERROR : VerificationProvider.Program.cs :: {ex.Message}");
    }

}



host.Run();
