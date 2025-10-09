namespace FlowSynx.Plugins.OpenAi.ChatGpt.Services;

public interface IReflectionGuard
{
    bool IsCalledViaReflection();
}