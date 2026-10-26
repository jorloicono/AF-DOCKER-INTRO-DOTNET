// API de ejemplo del curso: catálogo de convocatorias de financiación.
// Versión con SQL Server para el lab 08 (Docker Compose).
// Usa ADO.NET con Microsoft.Data.SqlClient para no depender de Entity Framework.

using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Convocatorias")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:Convocatorias'. " +
        "En Docker se pasa con la variable de entorno ConnectionStrings__Convocatorias.");

var app = builder.Build();

// SQL Server tarda unos segundos en arrancar: la API reintenta hasta que la base de datos responde.
await InicializarBaseDeDatosAsync(connectionString, app.Logger);

app.MapGet("/", (IConfiguration config) => new
{
    servicio = "API de Convocatorias (SQL Server)",
    mensaje = config["Mensaje"],
    entorno = app.Environment.EnvironmentName,
    maquina = Environment.MachineName,
    servidorBd = new SqlConnectionStringBuilder(connectionString).DataSource
});

app.MapGet("/convocatorias", async () =>
{
    await using var cn = new SqlConnection(connectionString);
    await cn.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT Id, Titulo, Organismo, Importe FROM dbo.Convocatorias ORDER BY Id", cn);
    await using var reader = await cmd.ExecuteReaderAsync();

    var lista = new List<Convocatoria>();
    while (await reader.ReadAsync())
    {
        lista.Add(new Convocatoria(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3)));
    }
    return lista;
});

app.MapGet("/convocatorias/{id:int}", async (int id) =>
{
    await using var cn = new SqlConnection(connectionString);
    await cn.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT Id, Titulo, Organismo, Importe FROM dbo.Convocatorias WHERE Id = @id", cn);
    cmd.Parameters.AddWithValue("@id", id);
    await using var reader = await cmd.ExecuteReaderAsync();

    return await reader.ReadAsync()
        ? Results.Ok(new Convocatoria(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3)))
        : Results.NotFound(new { error = $"No existe la convocatoria {id}" });
});

app.MapPost("/convocatorias", async (NuevaConvocatoria nueva) =>
{
    await using var cn = new SqlConnection(connectionString);
    await cn.OpenAsync();
    await using var cmd = new SqlCommand(
        """
        INSERT INTO dbo.Convocatorias (Titulo, Organismo, Importe)
        OUTPUT INSERTED.Id
        VALUES (@titulo, @organismo, @importe)
        """, cn);
    cmd.Parameters.AddWithValue("@titulo", nueva.Titulo);
    cmd.Parameters.AddWithValue("@organismo", nueva.Organismo);
    cmd.Parameters.AddWithValue("@importe", nueva.Importe);

    var id = (int)(await cmd.ExecuteScalarAsync())!;
    return Results.Created($"/convocatorias/{id}",
        new Convocatoria(id, nueva.Titulo, nueva.Organismo, nueva.Importe));
});

// Comprobación de salud que incluye la base de datos.
app.MapGet("/health", async () =>
{
    try
    {
        await using var cn = new SqlConnection(connectionString);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT 1", cn);
        await cmd.ExecuteScalarAsync();
        return Results.Ok(new { estado = "Healthy", baseDeDatos = "OK" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { estado = "Unhealthy", error = ex.Message }, statusCode: 503);
    }
});

app.Run();

static async Task InicializarBaseDeDatosAsync(string connectionString, ILogger logger)
{
    var csb = new SqlConnectionStringBuilder(connectionString);
    var nombreBd = csb.InitialCatalog;
    csb.InitialCatalog = "master";

    const int maxIntentos = 12;
    for (var intento = 1; ; intento++)
    {
        try
        {
            await using var cn = new SqlConnection(csb.ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(
                $"IF DB_ID(N'{nombreBd}') IS NULL CREATE DATABASE [{nombreBd}];", cn);
            await cmd.ExecuteNonQueryAsync();
            break;
        }
        catch (SqlException ex) when (intento < maxIntentos)
        {
            logger.LogWarning("SQL Server todavía no responde (intento {Intento}/{Max}): {Error}",
                intento, maxIntentos, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    await using var conexion = new SqlConnection(connectionString);
    await conexion.OpenAsync();
    await using var crear = new SqlCommand(
        """
        IF OBJECT_ID(N'dbo.Convocatorias') IS NULL
        BEGIN
            CREATE TABLE dbo.Convocatorias (
                Id        INT IDENTITY(1,1) PRIMARY KEY,
                Titulo    NVARCHAR(200)  NOT NULL,
                Organismo NVARCHAR(100)  NOT NULL,
                Importe   DECIMAL(18,2)  NOT NULL
            );

            INSERT INTO dbo.Convocatorias (Titulo, Organismo, Importe) VALUES
                (N'Fondo de Cooperación Municipal 2027', N'Diputación', 1250000),
                (N'Plan de Inversiones Financieramente Sostenibles', N'Ayuntamiento', 480000),
                (N'Línea de anticipos a entidades locales', N'Comunidad Autónoma', 3000000);
        END
        """, conexion);
    await crear.ExecuteNonQueryAsync();

    logger.LogInformation("Base de datos '{Bd}' lista", nombreBd);
}

record Convocatoria(int Id, string Titulo, string Organismo, decimal Importe);
record NuevaConvocatoria(string Titulo, string Organismo, decimal Importe);
