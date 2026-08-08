using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SajhaSikshya.Configurations;
using SajhaSikshya.Constants;
using SajhaSikshya.Data.Entities.AI;
using SajhaSikshya.Repositories.Interfaces;
using SajhaSikshya.Services.AI.Gemini;
using SajhaSikshya.Services.Interfaces.AI;

namespace SajhaSikshya.Services.AI;

/// <summary>
/// Gemini-backed implementation of <see cref="IAIService"/>. A typed <see cref="HttpClient"/>
/// (registered via <c>AddHttpClient&lt;IAIService, GeminiAIService&gt;</c>) talks to Google's
/// Generative Language REST API directly — no third-party Gemini SDK dependency, so the
/// request/response shape, timeout, and failure handling are fully under this project's
/// control (see <see cref="Gemini.GeminiGenerateContentRequest"/> for the minimal wire subset used).
/// Every call, whether it reaches Gemini, is served from cache, or is rejected before
/// either, is written to <see cref="AIUsageLog"/> — this is the one and only place that
/// happens, so no feature service needs to remember to log anything itself.
/// </summary>
public class GeminiAIService : IAIService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GeminiAIService> _logger;

    public GeminiAIService(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        IMemoryCache cache,
        IUnitOfWork unitOfWork,
        ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<AIGenerationResult>> GenerateAsync(AIGenerationRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length > AIConstants.MaxPromptLength)
        {
            await LogAsync(request, success: false, tokenCount: null, responseTimeMs: 0, fromCache: false, error: "Prompt failed validation (empty or too long).");
            return ServiceResult<AIGenerationResult>.Failure("Unable to process this request.");
        }

        if (request.CacheKey is not null && _cache.TryGetValue<CachedAIResponse>(request.CacheKey, out var cached) && cached is not null)
        {
            _logger.LogInformation("AI cache hit for feature {Feature}, key '{CacheKey}'.", request.Feature, request.CacheKey);
            await LogAsync(request, success: true, tokenCount: cached.TokenCount, responseTimeMs: 0, fromCache: true, error: null);
            return ServiceResult<AIGenerationResult>.Success(new AIGenerationResult
            {
                Text = cached.Text,
                TokenCount = cached.TokenCount,
                ResponseTimeMs = 0,
                FromCache = true,
            });
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Gemini API key is not configured; AI feature {Feature} is unavailable.", request.Feature);
            await LogAsync(request, success: false, tokenCount: null, responseTimeMs: 0, fromCache: false, error: "AI service is not configured.");
            return ServiceResult<AIGenerationResult>.Failure("AI features are not available right now.");
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            var wireRequest = new GeminiGenerateContentRequest
            {
                Contents = new List<GeminiContent> { new() { Parts = new List<GeminiPart> { new() { Text = request.Prompt } } } },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = request.Temperature ?? AIConstants.DefaultTemperature,
                    MaxOutputTokens = request.MaxOutputTokens ?? AIConstants.DefaultMaxOutputTokens,
                    ResponseMimeType = request.ResponseSchema is null ? null : "application/json",
                    ResponseSchema = request.ResponseSchema,
                },
            };

            using var httpResponse = await _httpClient.PostAsJsonAsync(url, wireRequest, JsonOptions, cts.Token);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var body = await httpResponse.Content.ReadAsStringAsync(cts.Token);
                _logger.LogError(
                    "Gemini API returned {StatusCode} for feature {Feature}: {Body}",
                    (int)httpResponse.StatusCode,
                    request.Feature,
                    Truncate(body));
                stopwatch.Stop();
                await LogAsync(request, success: false, tokenCount: null, responseTimeMs: (int)stopwatch.ElapsedMilliseconds, fromCache: false, error: $"HTTP {(int)httpResponse.StatusCode}");
                return ServiceResult<AIGenerationResult>.Failure("AI service is temporarily unavailable. Please try again.");
            }

            var wireResponse = await httpResponse.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cts.Token);

            // gemini-flash-latest is a "thinking" model — a longer structured-JSON answer
            // can come back as more than one Part, so every Part's Text must be
            // concatenated in order. Taking only the first Part risks silently truncating
            // valid JSON into something that fails to parse downstream.
            var parts = wireResponse?.Candidates?.FirstOrDefault()?.Content?.Parts;
            var text = parts is null ? null : string.Concat(parts.Select(p => p.Text));

            if (string.IsNullOrWhiteSpace(text))
            {
                stopwatch.Stop();
                _logger.LogError("Gemini returned no usable content for feature {Feature}.", request.Feature);
                await LogAsync(request, success: false, tokenCount: null, responseTimeMs: (int)stopwatch.ElapsedMilliseconds, fromCache: false, error: "Empty response from Gemini.");
                return ServiceResult<AIGenerationResult>.Failure("AI service returned an unexpected response. Please try again.");
            }

            stopwatch.Stop();
            var tokenCount = wireResponse?.UsageMetadata?.TotalTokenCount;

            if (request.CacheKey is not null)
            {
                _cache.Set(request.CacheKey, new CachedAIResponse(text, tokenCount), TimeSpan.FromMinutes(AIConstants.DefaultCacheDurationMinutes));
            }

            await LogAsync(request, success: true, tokenCount: tokenCount, responseTimeMs: (int)stopwatch.ElapsedMilliseconds, fromCache: false, error: null);

            return ServiceResult<AIGenerationResult>.Success(new AIGenerationResult
            {
                Text = text,
                TokenCount = tokenCount,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                FromCache = false,
            });
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogError("Gemini API call timed out for feature {Feature}.", request.Feature);
            await LogAsync(request, success: false, tokenCount: null, responseTimeMs: (int)stopwatch.ElapsedMilliseconds, fromCache: false, error: "Request timed out.");
            return ServiceResult<AIGenerationResult>.Failure("AI service took too long to respond. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Gemini API call failed for feature {Feature}.", request.Feature);
            await LogAsync(request, success: false, tokenCount: null, responseTimeMs: (int)stopwatch.ElapsedMilliseconds, fromCache: false, error: Truncate(ex.Message));
            return ServiceResult<AIGenerationResult>.Failure("AI service is temporarily unavailable. Please try again.");
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to parse Gemini response for feature {Feature}.", request.Feature);
            await LogAsync(request, success: false, tokenCount: null, responseTimeMs: (int)stopwatch.ElapsedMilliseconds, fromCache: false, error: "Malformed response.");
            return ServiceResult<AIGenerationResult>.Failure("AI service returned an unexpected response. Please try again.");
        }
    }

    /// <summary>Never lets a logging failure surface as the AI call's own failure — this is an audit trail, not a critical path.</summary>
    private async Task LogAsync(AIGenerationRequest request, bool success, int? tokenCount, int responseTimeMs, bool fromCache, string? error)
    {
        try
        {
            await _unitOfWork.Repository<AIUsageLog>().AddAsync(new AIUsageLog
            {
                UserId = request.UserId,
                Feature = request.Feature,
                PromptType = request.PromptType,
                Success = success,
                ErrorMessage = error is null ? null : Truncate(error),
                TokenCount = tokenCount,
                ResponseTimeMs = responseTimeMs,
                FromCache = fromCache,
            });
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write AI usage log for feature {Feature}.", request.Feature);
        }
    }

    private static string Truncate(string value) =>
        value.Length <= AIConstants.MaxErrorMessageLength ? value : value[..AIConstants.MaxErrorMessageLength];

    private sealed record CachedAIResponse(string Text, int? TokenCount);
}
