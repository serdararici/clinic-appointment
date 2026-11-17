using Clinic.Application.Appointments;
using Clinic.Application.Availability;
using Clinic.Application.Staff;
using Clinic.Infrastructure;
using Clinic.Infrastructure.Persistence;
using Clinic.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorAvailabilityService, DoctorAvailabilityService>();
builder.Services.AddScoped<IStaffService, StaffService>();

var app = builder.Build();

// Apply pending EF Core migrations and seed sample data before serving requests.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    DbInitializer.MigrateAndSeed(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
