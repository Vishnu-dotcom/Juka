using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();





app.MapGet("/{src}", (string src) =>
{
    JukaCompiler.Compiler compiler = new JukaCompiler.Compiler();
    string sourceAsString = src;
    var outputValue = compiler.Go(sourceAsString, false);

    if (compiler.HasErrors())
    {
        var errors = compiler.ListErrors().ToString();
        return JsonSerializer.Serialize("{errors: " + errors + "}");
    }
    return JsonSerializer.Serialize("{output: " + outputValue + "}");
})
.WithName("Juka");

app.Run();