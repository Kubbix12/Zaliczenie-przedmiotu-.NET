using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrudWpfApp.Services
{
    public sealed class JsonFileStore
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public async Task SaveAsync<T>(string path, T data)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            await using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, data, _jsonOptions);
        }

        public async Task<T?> LoadAsync<T>(string path)
        {
            if (!File.Exists(path)) return default;

            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(fs, _jsonOptions);
        }
    }
}
