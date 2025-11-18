namespace Demo.Api;

public sealed class ExternalClient(HttpClient http)
{
    public async Task<string> GetAsync(string path, CancellationToken ct = default)
    {
        var res = await http.GetAsync(path, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }
}