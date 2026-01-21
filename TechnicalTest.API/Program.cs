using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.API.Models;
using TechnicalTest.Data;
using TechnicalTest.Data.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationContext>();
builder.Services.AddTechnicalTestDataServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Hello world");

app.MapPost("/admin/customers/", async ([FromBody] AddCustomerModel customer, ICustomerAdminModule customerModule) =>
{
    // TODO presumably an endpoint like this would be in a separate admin-only API with it's own authentication for bank staff
    // We're ignoring these problems for a small-scale tech test
    var created = await customerModule.Create(new CustomerModification(customer.Name, customer.DateOfBirth, customer.DailyLimit));

    return Results.Ok(created);
});

app.MapGet("/admin/customers/", async (ICustomerAdminModule customerModule) =>
{
    var customers =  customerModule.GetAll();

    return Results.Ok(customers);
});

app.Run();