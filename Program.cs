// API de ejemplo del curso: catálogo de convocatorias de financiación.
// Versión sin base de datos (los datos viven en memoria) para los labs 05 y 06.

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();

var convocatorias = new List<Convocatoria>
{
    new(1, "Fondo de Cooperación Municipal 2027", "Diputación", 1_250_000m),
    new(2, "Plan de Inversiones Financieramente Sostenibles", "Ayuntamiento", 480_000m),
    new(3, "Línea de anticipos a entidades locales", "Comunidad Autónoma", 3_000_000m)
};

// Información del contenedor: útil para ver qué instancia responde y qué configuración ha recibido.
app.MapGet("/", (IConfiguration config) => new
{
    servicio = "API de Convocatorias",
    mensaje = config["Mensaje"],
    entorno = app.Environment.EnvironmentName,
    maquina = Environment.MachineName,
    usuario = Environment.UserName,
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    sistemaOperativo = System.Runtime.InteropServices.RuntimeInformation.OSDescription
});

app.MapGet("/convocatorias", () => convocatorias);

app.MapGet("/convocatorias/{id:int}", (int id) =>
    convocatorias.FirstOrDefault(c => c.Id == id) is { } c
        ? Results.Ok(c)
        : Results.NotFound(new { error = $"No existe la convocatoria {id}" }));

app.MapHealthChecks("/health");

app.Run();

record Convocatoria(int Id, string Titulo, string Organismo, decimal Importe);
