﻿using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlyleafLib.MediaPlayer.Translation.Services;

#nullable enable

public class DeepLXTranslateService : ITranslateService
{
    private readonly HttpClient _httpClient;
    private string? _srcLang;
    private string? _targetLang;
    private readonly DeepLXTranslateSettings _settings;

    public DeepLXTranslateService(DeepLXTranslateSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new TranslationConfigException(
                $"Endpoint for {ServiceType} is not configured.");
        }

        _settings = settings;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(settings.Endpoint);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(settings.TimeoutMs);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public TranslateServiceType ServiceType => TranslateServiceType.DeepLX;

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public void Initialize(Language src, TargetLanguage target)
    {
        (TranslateLanguage srcLang, _) = this.TryGetLanguage(src, target);

        _srcLang = ToSourceCode(srcLang.ISO6391);
        _targetLang = ToTargetCode(target);
    }

    private string ToSourceCode(string iso6391)
    {
        return DeepLTranslateService.ToSourceCode(iso6391);
    }

    private string ToTargetCode(TargetLanguage target)
    {
        return DeepLTranslateService.ToTargetCode(target);
    }

    public async Task<string> TranslateAsync(string text, CancellationToken token)
    {
        if (_srcLang == null || _targetLang == null)
        {
            throw new InvalidOperationException("must be initialized");
        }

        string jsonResultString = "";
        int statusCode = -1;

        try
        {
            DeepLXTranslateRequest requestBody = new()
            {
                source_lang = _srcLang,
                target_lang = _targetLang,
                text = text
            };

            string jsonRequest = JsonSerializer.Serialize(requestBody, JsonOptions);
            using StringContent content = new(jsonRequest, Encoding.UTF8, "application/json");

            using var result = await _httpClient.PostAsync("/translate", content, token).ConfigureAwait(false);
            jsonResultString = await result.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            statusCode = (int)result.StatusCode;
            result.EnsureSuccessStatusCode();

            DeepLXTranslateResult? responseData = JsonSerializer.Deserialize<DeepLXTranslateResult>(jsonResultString);
            return responseData!.data;
        }
        catch (OperationCanceledException ex)
            when (!ex.Message.StartsWith("The request was canceled due to the configured HttpClient.Timeout"))
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TranslationException($"Cannot request to {ServiceType}: {ex.Message}", ex)
            {
                Data =
                {
                    ["status_code"] = statusCode.ToString(),
                    ["response"] = jsonResultString
                }
            };
        }
    }

    private class DeepLXTranslateRequest
    {
        public required string text { get; init; }
        public string? source_lang { get; init; }
        public required string target_lang { get; init; }
    }

    private class DeepLXTranslateResult
    {
        public required string[] alternatives { get; init; }
        public required string data { get; init; }
        public required string source_lang { get; init; }
        public required string target_lang { get; init; }
    }
}
