using Npgsql;
using Pgvector;
using Pgvector.Npgsql;
using System.Text.Json;

var dataSourceBuilder = new NpgsqlDataSourceBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__MyDB"));
dataSourceBuilder.UseVector();
await using var dataSource = dataSourceBuilder.Build();

var conn = dataSource.OpenConnection();

await using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", conn))
{
    await cmd.ExecuteNonQueryAsync();
}

conn.ReloadTypes();

await using (var cmd = new NpgsqlCommand("DROP TABLE IF EXISTS items", conn))
{
    await cmd.ExecuteNonQueryAsync();
}

await using (var cmd = new NpgsqlCommand("CREATE TABLE items (id serial PRIMARY KEY, embedding vector(3))", conn))
{
    await cmd.ExecuteNonQueryAsync();
}

await using (var cmd = new NpgsqlCommand("INSERT INTO items (embedding) VALUES ($1), ($2), ($3)", conn))
{
    var embedding1 = new Vector(new float[] { 1, 1, 1 });
    var embedding2 = new Vector(new float[] { 2, 2, 2 });
    var embedding3 = new Vector(new float[] { 1, 1, 2 });
    cmd.Parameters.AddWithValue(embedding1);
    cmd.Parameters.AddWithValue(embedding2);
    cmd.Parameters.AddWithValue(embedding3);
    await cmd.ExecuteNonQueryAsync();
}

await using (var cmd = new NpgsqlCommand("SELECT * FROM items ORDER BY embedding <-> $1 LIMIT 5", conn))
{
    var embedding = new Vector(new float[] { 1, 1, 1 });
    cmd.Parameters.AddWithValue(embedding);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        var ids = new List<int>();
        var embeddings = new List<Vector>();

        while (await reader.ReadAsync())
        {
            ids.Add((int)reader.GetValue(0));
            embeddings.Add((Vector)reader.GetValue(1));
        }

        Console.WriteLine(JsonSerializer.Serialize(ids.ToArray()));
        Console.WriteLine(JsonSerializer.Serialize(embeddings[0].ToArray()));
        Console.WriteLine(JsonSerializer.Serialize(embeddings[1].ToArray()));
        Console.WriteLine(JsonSerializer.Serialize(embeddings[2].ToArray()));
    }
}

await using (var cmd = new NpgsqlCommand("CREATE INDEX ON items USING ivfflat (embedding vector_l2_ops) WITH (lists = 100)", conn))
{
    await cmd.ExecuteNonQueryAsync();
}

await using (var cmd = new NpgsqlCommand("SELECT $1", conn))
{
    var embedding = new Vector(new float[16000]);
    cmd.Parameters.AddWithValue(embedding);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        await reader.ReadAsync();
        Console.WriteLine(JsonSerializer.Serialize(embedding.ToArray()));
        Console.WriteLine(JsonSerializer.Serialize(((Vector)reader.GetValue(0)).ToArray()));
    }
}

try
{
    await using (var cmd = new NpgsqlCommand("SELECT $1", conn))
    {
        var embedding = new Vector(new float[65536]);
        cmd.Parameters.AddWithValue(embedding);
        await cmd.ExecuteReaderAsync();
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}