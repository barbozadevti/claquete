using System.Diagnostics;
using Claquete.Api.Dados;
using Claquete.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var opcoesBanco = builder.Configuration.GetSection("Banco").Get<OpcoesBanco>() ?? new OpcoesBanco();
var pastaDados = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
builder.Services.AddSingleton(new Banco(opcoesBanco, pastaDados));
builder.Services.AddProblemDetails();

var app = builder.Build();

await InicializadorBanco.GarantirAsync(app.Services.GetRequiredService<Banco>());

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapearFilmes();
app.MapearAtores();
app.MapearPainel();
app.MapearLaboratorio();

// Usado pelo atalho da Área de Trabalho: abre o navegador quando o servidor fica pronto.
if (app.Configuration.GetValue<bool>("AbrirNavegador"))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var endereco = app.Urls.FirstOrDefault() ?? "http://localhost:5190";
        Process.Start(new ProcessStartInfo(endereco) { UseShellExecute = true });
    });
}

app.Run();

public partial class Program;
