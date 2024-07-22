using Npgsql;
using StackExchange.Redis;
using System.Data;
using Newtonsoft.Json;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;

class Program
{
    static async Task Main(string[] args)
    {
        // create cache if not exists and create object with data

        var redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        var redisDb = redis.GetDatabase();

        string cacheKey = "GeometriesCache";

        var cacheDataTable = await redisDb.StringGetAsync(cacheKey);
        DataTable dataTable;

        if (cacheDataTable.HasValue)
        {
            dataTable = JsonConvert.DeserializeObject<DataTable>(cacheDataTable);

            Console.WriteLine("DataTable obtido do cache.");
        }
        else
        {
            var connectionString = "Server=127.0.0.1;Port=5432;Database=analysis;User Id=postgres;Password=postgis;";

            dataTable = new DataTable();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT id, ST_AsText(geom) AS geom, created_at FROM geometries_cache";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        dataTable.Columns.Add(reader.GetName(i), typeof(string));
                    }

                    while (await reader.ReadAsync())
                    {
                        var row = dataTable.NewRow();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i).ToString();
                        }

                        dataTable.Rows.Add(row);
                    }
                }
            }

            var serializeDataTable = JsonConvert.SerializeObject(dataTable);
            await redisDb.StringSetAsync(cacheKey, serializeDataTable, TimeSpan.FromHours(1));

            Console.WriteLine("DataTable armazenado no cache.");
        }

        // intersection
        var inputWkt = "MULTIPOLYGON (((-64.02318965344425 -1.2203149145803245, -64.4119007685688 -0.1523399030845993, -65.39615186313283 0.4159177313121294, -66.51540091125739 0.2185639259955041, -67.24593884425738 -0.6520572801835954, -67.24593884425738 -1.788572548977053, -66.51540091125739 -2.659193755156153, -65.39615186313283 -2.8565475604727784, -64.4119007685688 -2.2882899260760503, -64.02318965344425 -1.2203149145803245)), ((-61.20746078445172 9.585040247402354, -61.542723086335286 10.616871514544895, -62.420451187813 11.254578308293858, -63.50538278700128 11.254578308293858, -64.383110888479 10.616871514544895, -64.71837319036257 9.585040247402354, -64.383110888479 8.553208980259813, -63.50538278700128 7.91550218651085, -62.420451187813 7.9155021865108495, -61.542723086335286 8.553208980259813, -61.20746078445172 9.585040247402354)))";
        var wktReader = new WKTReader();
        var inputGeometry = wktReader.Read(inputWkt);

        List<Geometry> intersectionResult = new List<Geometry>();
        var invalidsGeometries = 0;

        foreach (DataRow row in dataTable.Rows)
        {
            var geomWkt = row["geom"].ToString();

            if (string.IsNullOrEmpty(geomWkt))
            {
                continue;
            }

            try
            {
                var geometry = wktReader.Read(geomWkt);

                if (!geometry.IsValid)
                {
                    invalidsGeometries++;
                    continue;
                }

                var intersection = inputGeometry.Intersection(geometry);

                if (intersection.IsEmpty)
                {
                    continue;
                }

                intersectionResult.Add(intersection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar geometria: {ex.Message}");
            }
        }

        if (intersectionResult != null && intersectionResult.Count > 0)
        {
            Console.WriteLine($"Quantidade de intersecções: {intersectionResult.Count()}");
            Console.WriteLine($"Geometrias inválidas: {invalidsGeometries}");
        }
        else
        {
            Console.WriteLine("Nenhuma intersecção encontrada");
            Console.WriteLine($"Geometrias inválidas: {invalidsGeometries}");
        }
    }
}