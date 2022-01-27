using System.Text.Json;

namespace NotificationService.Infrastructure;

public abstract class ServiceClientBase
{
    protected readonly HttpClient _httpClient;
    protected static readonly JsonSerializerOptions _serializationOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ServiceClientBase(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    protected abstract string ErrorMessage { get; }

    protected async Task<T?> GetAsync<T>(string uri, CancellationToken cancellationToken)
        where T : class
    {
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return response switch
        {
            { IsSuccessStatusCode: true } => await DeserialiseResponseAsync<T>(response, cancellationToken),
            { StatusCode: System.Net.HttpStatusCode.NotFound } => null,
            _ => throw new ApplicationException(ErrorMessage),
        };
    }

    protected async Task<T?> PostAsync<T>(string uri, HttpContent content, CancellationToken cancellationToken)
        where T : class
    {
        var response = await _httpClient.PostAsync(uri, content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return response switch
        {
            { IsSuccessStatusCode: true } => await DeserialiseResponseAsync<T>(response, cancellationToken),
            _ => throw new ApplicationException(ErrorMessage),
        };
    }

    protected static async Task<T?> DeserialiseResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        where T : class
    {
        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(responseStream, _serializationOptions, cancellationToken);
    }
}
