﻿namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class WPF0017MetadataMustBeAssignable
    {
        public const string DiagnosticId = "WPF0017";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Metadata must be of same type or super type.",
            messageFormat: "Metadata must be of same type or super type.",
            category: AnalyzerCategory.DependencyProperty,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "When overriding metadata must be of the same type or subtype of the overridden property's metadata.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}