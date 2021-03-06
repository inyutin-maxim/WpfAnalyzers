namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal class WPF0085XamlSetTypeConverterTarget
    {
        public const string DiagnosticId = "WPF0085";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Target of [XamlSetTypeConverter] should exist and have correct signature.",
            messageFormat: "Expected a method with signature void ReceiveTypeConverter(object, XamlSetTypeConverterEventArgs).",
            category: AnalyzerCategory.MarkupExtension,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Target of [XamlSetTypeConverter] should exist and have correct signature.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
