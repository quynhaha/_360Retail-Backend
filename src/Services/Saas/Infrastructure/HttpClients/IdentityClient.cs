using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace _360Retail.Services.Saas.Infrastructure.HttpClients;

public class IdentityClient : IIdentityClient
{
    private readonly HttpClient _http;

    public IdentityClient(HttpClient http)
    {
        _http = http;
    }

    public async Task AssignStoreAsync(string accessToken, Guid storeId)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var body = new
        {
            storeId = storeId,
            roleInStore = "Owner",
            isDefault = true
        };

        var res = await _http.PostAsJsonAsync(
            "/api/auth/assign-store",
            body
        );

        if (!res.IsSuccessStatusCode)
        {
            var error = await res.Content.ReadAsStringAsync();
            throw new Exception($"Assign store failed: [{res.StatusCode}] {_http.BaseAddress}/api/auth/assign-store - {error}");
        }
    }

    public async Task<bool> HasStoreAccessAsync(
        string accessToken,
        Guid storeId,
        string roleInStore)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.GetAsync(
            $"/api/identity/has-store-access?storeId={storeId}&role={roleInStore}"
        );

        if (!response.IsSuccessStatusCode)
            return false;

        return await response.Content.ReadFromJsonAsync<bool>();
    }

    public async Task<List<UserStoreAccessDto>> GetMyStoresAsync(string accessToken)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.GetAsync("/api/identity/stores-my");

        if (!response.IsSuccessStatusCode)
            return new List<UserStoreAccessDto>();

        return await response.Content.ReadFromJsonAsync<List<UserStoreAccessDto>>()
            ?? new List<UserStoreAccessDto>();
    }
}
