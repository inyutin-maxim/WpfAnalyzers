namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal class WPF0084XamlSetMarkupExtensionAttributeTarget
    {
        public const string DiagnosticId = "WPF0084";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Target of [XamlSetMarkupExtension] should exist and have correct signature.",
            messageFormat: "Expected a method with signature void ReceiveMarkupExtension(object, XamlSetMarkupExtensionEventArgs).",
            category: AnalyzerCategory.MarkupExtension,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Target of [XamlSetMarkupExtension] should exist and have correct signature.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
