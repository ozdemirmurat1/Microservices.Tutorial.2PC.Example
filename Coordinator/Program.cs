using Coordinator.Models.Contexts;
using Coordinator.Services;
using Coordinator.Services.Abstraction;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TwoPhaseCommitContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer")));

builder.Services.AddHttpClient("OrderAPI", client => client.BaseAddress = new("https://localhost:7253/"));
builder.Services.AddHttpClient("StockAPI", client => client.BaseAddress = new("https://localhost:7092/"));
builder.Services.AddHttpClient("PaymentAPI", client => client.BaseAddress = new("https://localhost:7084/"));

builder.Services.AddTransient<ITransactionService,TransactionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/create-order-transaction", async (ITransactionService transactionService) =>
{
    // Phase 1 -Prepare

    var transactionId=await transactionService.CreateTransactionAsync();
    await transactionService.PrepareServicesAsync(transactionId);
    bool transactionState=await transactionService.CheckReadyServicesAsync(transactionId);

    if (transactionState)
    {
        // Phase 2 -Commit
        await transactionService.CommitAsync(transactionId);
        transactionState=await transactionService.CheckTransactionStateServicesAsync(transactionId);
    }

    if(!transactionState)
        await transactionService.RollbackAsync(transactionId);
});

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

app.Run();
