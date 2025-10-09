using FlowSynx.PluginCore.Helpers;

namespace FlowSynx.Plugins.OpenAi.ChatGpt.Services;

internal class DefaultReflectionGuard : IReflectionGuard
{
    public bool IsCalledViaReflection() => ReflectionHelper.IsCalledViaReflection();
}