namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class WPF0120RegisterContainingMemberAsNameForRoutedCommand
    {
        public const string DiagnosticId = "WPF0120";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Register containing member name as name for routed command.",
            messageFormat: "Register {0} as name.",
            category: AnalyzerCategory.RoutedCommand,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Register containing member name as name for routed command.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
