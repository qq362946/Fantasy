#pragma warning disable CS8618
namespace Fantasy;

public static class AppDefine
{
    public static CommandLineOptions Options;
    public static uint AppId => Options.AppId;
}