using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.OpenAi.ChatGpt.Models;

public class OpenAiChatGptPluginSpecifications: PluginSpecifications
{
    [RequiredMember]
    public string ApiKey { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

    public string Model { get; set; } = "gpt-3.5-turbo";
}