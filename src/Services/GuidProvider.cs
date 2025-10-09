namespace FlowSynx.Plugins.OpenAi.ChatGpt.Services;

internal class GuidProvider : IGuidProvider
{
    public Guid NewGuid() => Guid.NewGuid();
}