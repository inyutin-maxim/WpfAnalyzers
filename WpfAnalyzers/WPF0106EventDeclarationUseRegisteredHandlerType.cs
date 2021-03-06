namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class WPF0106EventDeclarationUseRegisteredHandlerType
    {
        public const string DiagnosticId = "WPF0106";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use the registered handler type.",
            messageFormat: "Use the registered handler type {0}.",
            category: AnalyzerCategory.RoutedEvent,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Use the registered handler type.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
