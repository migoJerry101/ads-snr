using Quartz;
using ads.Repository;
using ads.Interface;
using Microsoft.EntityFrameworkCore;
using ads.Data;
using System;

var builder = WebApplication.CreateBuilder(args);

//Independe Injection
builder.Services.AddScoped<IInventory, InventoryRepo>();
builder.Services.AddScoped<ISales, SalesRepo>();
builder.Services.AddScoped<IAds, AdsRepo>();
builder.Services.AddScoped<IOpenQuery, OpenQueryRepo>();
builder.Services.AddScoped<ILogs, LogsRepo>();

builder.Services.AddScoped<IImportInventory, ImportInventoryRepo>();

builder.Services.AddScoped<IAdsBackGroundTask, AdsBackGroundTaskRepo>();
builder.Services.AddScoped<ITotalAdsChain, TotalAdsChainRepo>();
builder.Services.AddScoped<ITotalAdsClub, TotalAdsClubRepo>();
builder.Services.AddScoped<IClub, ClubRepo>();
builder.Services.AddScoped<IItem, ItemRepo>();
builder.Services.AddScoped<IInventoryBackup, InventoryBackup>();
builder.Services.AddScoped<IExcel, ExcelRepo>();

//Quartz run for cronjob
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    var jobKey = new JobKey("DataRepo");
    q.AddJob<CronJobsADSRepo>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("DataRepo-trigger")
        .WithCronSchedule("01 00 03 * * ?"));
});



builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Web Api",
        Version = "v1",
    });

    // Add this line to modify the controller names in Swagger
    options.CustomSchemaIds(type => type.FullName);
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", policy =>
    {
        policy.AllowAnyHeader()
                .AllowAnyMethod() //allow any http methods
                .SetIsOriginAllowed(isOriginAllowed: _ => true) //no restriction in any domain
                .AllowCredentials();
    });
});

builder.Services.AddDbContextPool<AdsContex>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnection"), sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.CommandTimeout(999);
    })
);

var app = builder.Build();

app.UseRouting();
app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADS");

    });
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
