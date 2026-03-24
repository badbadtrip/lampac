namespace Shared.Models.Module
{
    public record ModuleOnlineItem(string name, string url, string plugin, int index);

    public record ModuleOnlineSpiderItem(string name, string url, int index);
}
