using CRJSwaggerExcelExport_NET10.Data;
using CRJSwaggerExcelExport_NET10.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CrjDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrjDatabase")));
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<CRJSwaggerExcelExport_NET10.Data.SqlServerCrjRepository>();

var app = builder.Build();

// Optional convenience for a fresh demo database. Existing databases should use
// the supplied SQL script and set InitializeOnStartup=false if schema is managed separately.
if (!string.Equals(app.Configuration["Database:Provider"], "SqlServer", StringComparison.OrdinalIgnoreCase)
    && app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CrjDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (app.Configuration.GetValue<bool>("Database:SeedDemoData"))
        await DemoDataSeeder.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
