using Clinic.Api.ErrorHandling;
using Clinic.Api.OpenApi;
using Clinic.Api.Security;
using Clinic.Application.Appointments;
using Clinic.Application.Auth;
using Clinic.Application.Availability;
using Clinic.Application.Management;
using Clinic.Application.Slots;
using Clinic.Application.Staff;
using Clinic.Infrastructure;
using Clinic.Infrastructure.Persistence;
using Clinic.Infrastructure.Seed;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

// CORS: the browser frontend (Vite dev server) may call the API, including with the Authorization header.
// The allowed origins come from configuration (Cors:AllowedOrigins); the default is the Vite dev server.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins(allowedOrigins)
    .WithHeaders("Authorization", "Content-Type", "Accept")
    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorAvailabilityService, DoctorAvailabilityService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IDefaultScheduleService, DefaultScheduleService>();
builder.Services.AddScoped<IAutoScheduleService, AutoScheduleService>();
builder.Services.AddHostedService<Clinic.Api.DefaultScheduleWorker>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<Clinic.Application.Account.IAccountService, Clinic.Application.Account.AccountService>();
builder.Services.AddScoped<IManagerService, ManagerService>();
builder.Services.AddJwtAuthentication();

// Who is calling, and the ownership/scope rules that use it.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<AccessGuard>();
builder.Services.AddScoped<ISlotService, SlotService>();
// "Now" comes from TimeProvider so "Past" slots and booking in the past can be tested with a fixed clock.
builder.Services.AddSingleton(TimeProvider.System);

// Maps NotFound/Argument/InvalidOperation exceptions to 404/400/409 for the management controllers.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply pending EF Core migrations and seed sample data before serving requests.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    DbInitializer.MigrateAndSeed(dbContext);

    // Fill the booking window with the default schedule for every automatic-schedule doctor (today included).
    await scope.ServiceProvider.GetRequiredService<IDefaultScheduleService>()
        .EnsureForAllAsync(scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    // Interactive API reference at /scalar (Development only); "Bearer" lets you paste a login token.
    app.MapScalarApiReference(options => options
            .WithTitle("Clinic Appointment API")
            .AddPreferredSecuritySchemes("Bearer"))
        .AllowAnonymous();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

// Before authentication, so browser preflight (OPTIONS) requests are answered without a token.
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
