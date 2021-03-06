namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class WPF0081MarkupExtensionReturnTypeMustUseCorrectType
    {
        public const string DiagnosticId = "WPF0081";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "MarkupExtensionReturnType must use correct return type.",
            messageFormat: "MarkupExtensionReturnType must use correct return type. Expected: {0}",
            category: AnalyzerCategory.MarkupExtension,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MarkupExtensionReturnType must use correct return type.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
