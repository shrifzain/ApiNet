var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/hello", () => 
{
    return Results.Ok(new { Message = "Hello World from .NET 6! cae" });
});

app.Run();
