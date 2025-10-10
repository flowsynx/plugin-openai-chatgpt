using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.OpenAi.ChatGpt;
using FlowSynx.Plugins.OpenAi.ChatGpt.Models;
using FlowSynx.Plugins.OpenAi.ChatGpt.Services;
using Moq;

namespace FlowSynx.Plugins.OpenAi.ChatGpt.UnitTests;

public class OpenAiChatGptPluginTests
{
    private static OpenAiChatGptPlugin CreatePlugin(out Mock<IGuidProvider> guidMock, out Mock<IReflectionGuard> reflectionMock)
    {
        guidMock = new Mock<IGuidProvider>(MockBehavior.Strict);
        reflectionMock = new Mock<IReflectionGuard>(MockBehavior.Strict);
        reflectionMock.Setup(x => x.IsCalledViaReflection()).Returns(false);
        return new OpenAiChatGptPlugin(guidMock.Object, reflectionMock.Object);
    }

    private static T InvokePrivate<T>(object instance, string name, params object?[] args)
    {
        var mi = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)!;
        try
        {
            var result = mi.Invoke(instance, args);
            return (T)result!;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException!;
        }
    }

    private static void InvokePrivate(object instance, string name, params object?[] args)
    {
        var mi = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)!;
        try
        {
            mi.Invoke(instance, args);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException!;
        }
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var fi = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        fi.SetValue(instance, value);
    }

    [Fact]
    public void Metadata_ShouldBePopulated()
    {
        var plugin = CreatePlugin(out _, out _);

        var md = plugin.Metadata;
        md.Should().NotBeNull();
        md.Name.Should().Be("OpenAi.ChatGpt");
        md.CompanyName.Should().Be("FlowSynx");
        md.Category.Should().Be(PluginCategory.AI);
        md.RepositoryUrl.Should().Contain("github.com/flowsynx/plugin-openai-chatgpt");
    }

    [Fact]
    public void SpecificationsType_ShouldBeExpected()
    {
        var plugin = CreatePlugin(out _, out _);
        plugin.SpecificationsType.Should().Be(typeof(OpenAiChatGptPluginSpecifications));
    }

    [Fact]
    public void SupportedOperations_ShouldBeEmpty()
    {
        var plugin = CreatePlugin(out _, out _);
        plugin.SupportedOperations.Should().NotBeNull();
        plugin.SupportedOperations.Should().BeEmpty();
    }

    [Fact]
    public async Task Initialize_ShouldThrow_WhenLoggerIsNull()
    {
        var plugin = CreatePlugin(out _, out _);
        plugin.Specifications = new OpenAiChatGptPluginSpecifications { ApiKey = "k" };
        var act = () => plugin.Initialize(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Initialize_ShouldThrow_WhenSpecificationsMissing()
    {
        var plugin = CreatePlugin(out _, out _);
        var logger = new Mock<IPluginLogger>().Object;
        var act = () => plugin.Initialize(logger);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plugin specifications must be provided before initialization.");
    }

    [Fact]
    public async Task Initialize_ShouldSucceed_WithValidSpecifications()
    {
        var plugin = CreatePlugin(out _, out _);
        plugin.Specifications = new OpenAiChatGptPluginSpecifications
        {
            ApiKey = "key",
            ApiUrl = "https://example.com",
            Model = "gpt"
        };
        var logger = new Mock<IPluginLogger>(MockBehavior.Loose);
        var act = () => plugin.Initialize(logger.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenNotInitialized()
    {
        var plugin = CreatePlugin(out _, out _);
        var act = async () => await plugin.ExecuteAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void EnsureInitialized_ShouldThrow_WhenNotInitialized()
    {
        var plugin = CreatePlugin(out _, out _);
        Action act = () => InvokePrivate(plugin, "EnsureInitialized");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureInitialized_ShouldPass_WhenInitialized()
    {
        var plugin = CreatePlugin(out _, out _);
        SetPrivateField(plugin, "_isInitialized", true);
        Action act = () => InvokePrivate(plugin, "EnsureInitialized");
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNotReflection_ShouldThrow_WhenReflectionDetected()
    {
        var guidMock = new Mock<IGuidProvider>(MockBehavior.Strict);
        var reflectionMock = new Mock<IReflectionGuard>(MockBehavior.Strict);
        reflectionMock.Setup(x => x.IsCalledViaReflection()).Returns(true);
        var plugin = new OpenAiChatGptPlugin(guidMock.Object, reflectionMock.Object);

        Action act = () => InvokePrivate(plugin, "EnsureNotReflection");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(Resources.ReflectionBasedAccessIsNotAllowed);
    }

    [Fact]
    public void ExtractPrompt_ShouldReturnValue()
    {
        var p = new FlowSynx.Plugins.Md5Hashing.Models.InputParameter { Prompt = "hello" };
        var plugin = CreatePlugin(out _, out _);
        var result = InvokePrivate<string>(plugin, "ExtractPrompt", p);
        result.Should().Be("hello");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractPrompt_ShouldThrow_OnNullOrWhitespace(string? prompt)
    {
        var p = new FlowSynx.Plugins.Md5Hashing.Models.InputParameter { Prompt = prompt };
        var plugin = CreatePlugin(out _, out _);
        Action act = () => InvokePrivate<string>(plugin, "ExtractPrompt", p);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Parameter 'prompt' is required and cannot be empty.");
    }

    [Fact]
    public void BuildHttpRequest_ShouldBuildExpectedRequest()
    {
        var plugin = CreatePlugin(out _, out _);
        var specs = new OpenAiChatGptPluginSpecifications
        {
            ApiKey = "secret",
            ApiUrl = "https://api.example.com/v1/chat/completions",
            Model = "gpt-x"
        };
        SetPrivateField(plugin, "_chatGptSpecifications", specs);

        var req = InvokePrivate<HttpRequestMessage>(plugin, "BuildHttpRequest", "Hello AI");
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.ToString().Should().Be(specs.ApiUrl);
        req.Headers.Authorization!.Scheme.Should().Be("Bearer");
        req.Headers.Authorization!.Parameter.Should().Be("secret");

        var content = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        using var doc = JsonDocument.Parse(content);
        doc.RootElement.GetProperty("model").GetString().Should().Be("gpt-x");
        var firstMsg = doc.RootElement.GetProperty("messages")[0];
        firstMsg.GetProperty("role").GetString().Should().Be("user");
        firstMsg.GetProperty("content").GetString().Should().Be("Hello AI");
    }

    [Fact]
    public void ParseChatGptResponse_ShouldReturnContent_OnSuccess()
    {
        var plugin = CreatePlugin(out _, out _);
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var body = "{\"choices\": [{\"message\": {\"content\": \"Hi!\"}}]}";
        var result = InvokePrivate<string?>(plugin, "ParseChatGptResponse", response, body);
        result.Should().Be("Hi!");
    }

    [Fact]
    public void ParseChatGptResponse_ShouldThrow_OnFailureStatus()
    {
        var plugin = CreatePlugin(out _, out _);
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var body = "{}";
        Action act = () => InvokePrivate<string?>(plugin, "ParseChatGptResponse", response, body);
        act.Should().Throw<HttpRequestException>();
    }

    [Fact]
    public void ParseChatGptResponse_ShouldThrow_OnMalformedJson()
    {
        var plugin = CreatePlugin(out _, out _);
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var body = "{\"unexpected\": 1}";
        Action act = () => InvokePrivate<string?>(plugin, "ParseChatGptResponse", response, body);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unable to parse ChatGPT response.*");
    }

    [Fact]
    public async Task SendChatRequestAsync_ShouldThrow_WhenSpecsNull()
    {
        var plugin = CreatePlugin(out _, out _);
        // Intentionally do not set _chatGptSpecifications
        var mi = plugin.GetType().GetMethod("SendChatRequestAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Func<Task> act = async () => await (Task<string?>)mi.Invoke(plugin, new object?[] { "Hello", CancellationToken.None })!;
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("ChatGPT specifications are not loaded.");
    }

    [Fact]
    public void BuildPluginContext_ShouldPopulateFields()
    {
        var plugin = CreatePlugin(out var guidMock, out _);
        var expectedId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        guidMock.Setup(g => g.NewGuid()).Returns(expectedId);

        var ctx = InvokePrivate<PluginContext>(plugin, "BuildPluginContext", "result text");

        // Use reflection to read properties to avoid tight coupling to PluginCore implementation details
        var contentProp = ctx.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance);
        var formatProp = ctx.GetType().GetProperty("Format", BindingFlags.Public | BindingFlags.Instance);

        contentProp.Should().NotBeNull();
        formatProp.Should().NotBeNull();
        contentProp!.GetValue(ctx)?.ToString().Should().Be("result text");
        formatProp!.GetValue(ctx)?.ToString().Should().Be("AI");
    }
}
