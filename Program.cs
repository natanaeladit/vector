using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

var dataSourceBuilder = new NpgsqlDataSourceBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__MyDB"));
dataSourceBuilder.UseVector();
await using var dataSource = dataSourceBuilder.Build();
var conn = dataSource.OpenConnection();

Console.WriteLine("Connection is opened");

await using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", conn))
{
    await cmd.ExecuteNonQueryAsync();
}
conn.ReloadTypes();

Console.WriteLine("Extension is enabled");

Console.WriteLine("Create table");

await using (var cmd = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS items (id serial PRIMARY KEY, embedding vector(3))", conn))
{
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("Insert vector");

await using (var cmd = new NpgsqlCommand("INSERT INTO items (embedding) VALUES ($1)", conn))
{
    var embedding = new Vector(new float[] { 1, 1, 1 });
    cmd.Parameters.AddWithValue(embedding);
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("the nearest neighbors\r\n\r\n");

await using (var cmd = new NpgsqlCommand("SELECT * FROM items ORDER BY embedding <-> $1 LIMIT 5", conn))
{
    var embedding = new Vector(new float[] { 1, 1, 1 });
    cmd.Parameters.AddWithValue(embedding);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            Console.WriteLine(reader.GetValue(0));
            Console.WriteLine(reader.GetValue(1));
        }
    }
}

Console.WriteLine("Add an approximate index");

await using (var cmd = new NpgsqlCommand("CREATE INDEX ON items USING ivfflat (embedding vector_l2_ops) WITH (lists = 100)", conn))
{
    await cmd.ExecuteNonQueryAsync();
}