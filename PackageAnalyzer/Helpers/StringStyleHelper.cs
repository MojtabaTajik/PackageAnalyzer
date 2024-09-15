namespace PackageAnalyzer.Helpers;

public static class StringStyleHelper
{
    public static string PromptStyle(this string text) => $"[bold orange1]{text}[/]";
    public static string ErrorStyle(this string text) => $"[bold red]{text}[/]";
    public static string InfoStyle(this string text) => $"[bold yellow2]{text}[/]";
}