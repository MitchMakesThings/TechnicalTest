using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TechnicalTest.API;
using TechnicalTest.API.Authentication;
using TechnicalTest.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddSwaggerGen(options =>
    {
        options
            .AddSecurityDefinition("bearer", new OpenApiSecurityScheme()
            {
                Description =
                    "It is assumed that an external login provider will authenticate users and provide JWT  tokens. For tech test convenience POST /api/admin/customers/X will generate an auth token for customerID X",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement()
        {
            [new OpenApiSecuritySchemeReference("bearer", document)] = []
        });
    });

// Introduce a very generic error handler, to return our response structure 500
builder.Services.AddExceptionHandler<ExceptionHandlerMiddleware>();

builder.Services.AddDbContext<ApplicationContext>();
builder.Services.AddTechnicalTestDataServices();

// Mock login handler
builder.Services.AddSingleton<AdminJwtManager>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not set"),
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not set"),
            // ASSUMPTION: This is quick enough for a proof of concept.
            // Reality should use asymmetric keys, and should be pulled from KeyVault etc - somewhere more secure than an exposed key in a config file in source control!
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not set"))
            ),
        };
    });
// Force all endpoints to require authentication by default.
// ASSUMPTION: We're building endpoints for a customer-facing mobile app. Users will be pre-authenticated, so all endpoints should require auth.
// For the tech test, the admin endpoints allow anonymous access, as admin auth is out of scope.
builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// For reasons I have not investigated, an action _must_ be provided to UseExceptionHandler(), but it doesn't need to actually do anything..
app.UseExceptionHandler(_ => { });

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();