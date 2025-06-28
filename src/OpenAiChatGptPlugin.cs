using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.PluginCore.Helpers;
using FlowSynx.Plugins.Md5Hashing.Models;
using FlowSynx.Plugins.OpenAi.ChatGpt.Models;
using System.Text.Json;

namespace FlowSynx.Plugins.OpenAi.ChatGpt;

public class OpenAiChatGptPlugin : IPlugin
{
    private IPluginLogger? _logger;
    private OpenAiChatGptPluginSpecifications _chatGptSpecifications = null!;
    private bool _isInitialized;
    private readonly HttpClient _httpClient = new();

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("6cd26dcb-6979-433b-b897-3aae5735b4d9"),
                Name = "OpenAi.ChatGpt",
                CompanyName = "FlowSynx",
                Description = Resources.PluginDescription,
                Version = new PluginVersion(1, 0, 0),
                Category = PluginCategory.AI,
                Authors = new List<string> { "FlowSynx" },
                Copyright = "© FlowSynx. All rights reserved.",
                Icon = "flowsynx.png",
                ReadMe = "README.md",
                RepositoryUrl = "https://github.com/flowsynx/plugin-openai-chatgpt",
                ProjectUrl = "https://flowsynx.io",
                Tags = new List<string>() { "flowSynx", "openai", "chatgpt", "artificial-intelligence" }
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }

    public Type SpecificationsType => typeof(OpenAiChatGptPluginSpecifications);

    public IReadOnlyCollection<string> SupportedOperations => new List<string>();

    public Task Initialize(IPluginLogger logger)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _chatGptSpecifications = Specifications.ToObject<OpenAiChatGptPluginSpecifications>();
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var inputParameter = parameters.ToObject<InputParameter>();
        string prompt = ExtractPrompt(inputParameter);

        var request = BuildHttpRequest(prompt);

        _logger?.LogDebug("Sending request to OpenAI ChatGPT API.");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        var reply = ProcessResponse(response, responseBody);

        var result = new PluginContext(Guid.NewGuid().ToString(), "Data")
        {
            Format = "AI",
            Content = reply
        };
        return Task.FromResult<object?>(result);
    }

    private string ExtractPrompt(InputParameter parameters)
    {
        if (string.IsNullOrEmpty(parameters.Prompt) || parameters.Prompt is not string prompt)
            throw new ArgumentException("Parameter 'prompt' is required and must be a string.");
        return prompt;
    }

    private HttpRequestMessage BuildHttpRequest(string prompt)
    {
        var requestBody = new
        {
            model = _chatGptSpecifications.Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        return new HttpRequestMessage(HttpMethod.Post, _chatGptSpecifications.ApiUrl)
        {
            Headers = { { "Authorization", $"Bearer {_chatGptSpecifications.ApiKey}" } },
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private string? ProcessResponse(HttpResponseMessage response, string responseBody)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError($"OpenAI API error: {response.StatusCode} - {responseBody}");
            throw new HttpRequestException($"OpenAI API call failed: {response.StatusCode}");
        }

        _logger?.LogDebug("Received response from OpenAI ChatGPT API.");

        using var jsonDoc = JsonDocument.Parse(responseBody);
        return jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}