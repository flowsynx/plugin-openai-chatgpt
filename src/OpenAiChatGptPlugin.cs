using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Md5Hashing.Models;
using FlowSynx.Plugins.OpenAi.ChatGpt.Models;
using FlowSynx.Plugins.OpenAi.ChatGpt.Services;
using System.Text;
using System.Text.Json;

namespace FlowSynx.Plugins.OpenAi.ChatGpt;

public class OpenAiChatGptPlugin : IPlugin
{
    private readonly IGuidProvider _guidProvider;
    private readonly IReflectionGuard _reflectionGuard;
    private IPluginLogger? _logger;
    private OpenAiChatGptPluginSpecifications _chatGptSpecifications = null!;
    private bool _isInitialized;
    private readonly HttpClient _httpClient = new();

    public OpenAiChatGptPlugin() : this(new GuidProvider(), new DefaultReflectionGuard()) { }

    internal OpenAiChatGptPlugin(IGuidProvider guidProvider, IReflectionGuard reflectionGuard)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
        _reflectionGuard = reflectionGuard ?? throw new ArgumentNullException(nameof(reflectionGuard));
    }

    public PluginMetadata Metadata => new PluginMetadata
    {
        Id = Guid.Parse("6cd26dcb-6979-433b-b897-3aae5735b4d9"),
        Name = "OpenAi.ChatGpt",
        CompanyName = "FlowSynx",
        Description = Resources.PluginDescription,
        Version = new Version(1, 0, 0),
        Category = PluginCategory.AI,
        Authors = new List<string> { "FlowSynx" },
        Copyright = "© FlowSynx. All rights reserved.",
        Icon = "flowsynx.png",
        ReadMe = "README.md",
        RepositoryUrl = "https://github.com/flowsynx/plugin-openai-chatgpt",
        ProjectUrl = "https://flowsynx.io",
        Tags = new List<string>() { "flowSynx", "openai", "chatgpt", "artificial-intelligence" },
        MinimumFlowSynxVersion = new Version(1, 1, 1),
    };

    public PluginSpecifications? Specifications { get; set; }

    public Type SpecificationsType => typeof(OpenAiChatGptPluginSpecifications);

    public IReadOnlyCollection<string> SupportedOperations => new List<string>();

    public Task Initialize(IPluginLogger logger)
    {
        EnsureNotReflection();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (Specifications is null)
            throw new InvalidOperationException("Plugin specifications must be provided before initialization.");

        _chatGptSpecifications = Specifications.ToObject<OpenAiChatGptPluginSpecifications>()
            ?? throw new InvalidOperationException("Failed to parse plugin specifications.");

        _isInitialized = true;
        _logger.LogInfo($"Initialized {Metadata.Name} v{Metadata.Version}");
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        EnsureNotReflection();
        EnsureInitialized();

        var inputParameter = parameters.ToObject<InputParameter>()
            ?? throw new ArgumentException("Invalid input parameters for ChatGPT plugin.");

        var prompt = ExtractPrompt(inputParameter);
        _logger?.LogDebug($"Executing ChatGPT request with prompt: {prompt}");

        var reply = await SendChatRequestAsync(prompt, cancellationToken);
        return BuildPluginContext(reply);
    }

    #region private Methods
    private void EnsureNotReflection()
    {
        if (_reflectionGuard.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");
    }

    private static string ExtractPrompt(InputParameter parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Prompt))
            throw new ArgumentException("Parameter 'prompt' is required and cannot be empty.");

        return parameters.Prompt;
    }

    private async Task<string?> SendChatRequestAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_chatGptSpecifications is null)
            throw new InvalidOperationException("ChatGPT specifications are not loaded.");

        var request = BuildHttpRequest(prompt);
        _logger?.LogDebug("Sending request to OpenAI ChatGPT API...");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseChatGptResponse(response, responseBody);
    }

    private HttpRequestMessage BuildHttpRequest(string prompt)
    {
        var body = new
        {
            model = _chatGptSpecifications!.Model,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var json = JsonSerializer.Serialize(body);
        var request = new HttpRequestMessage(HttpMethod.Post, _chatGptSpecifications.ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _chatGptSpecifications.ApiKey);
        return request;
    }

    private string? ParseChatGptResponse(HttpResponseMessage response, string responseBody)
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError($"OpenAI API call failed: {response.StatusCode} - {responseBody}");
            throw new HttpRequestException($"OpenAI API returned status code {response.StatusCode}");
        }

        _logger?.LogDebug("Received valid response from OpenAI ChatGPT API.");

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to parse OpenAI response: {ex.Message}");
            throw new InvalidOperationException("Unable to parse ChatGPT response.", ex);
        }
    }

    private PluginContext BuildPluginContext(string? reply)
    {
        return new PluginContext(_guidProvider.NewGuid().ToString(), "Data")
        {
            Format = "AI",
            Content = reply ?? string.Empty
        };
    }
    #endregion
}