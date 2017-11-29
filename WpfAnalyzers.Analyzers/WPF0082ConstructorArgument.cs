﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class WPF0082ConstructorArgument : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WPF0082";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "[ConstructorArgument] must match.",
            messageFormat: "[ConstructorArgument] must match. Expected: {0}",
            category: AnalyzerCategory.MarkupExtension,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "[ConstructorArgument] must match the name of the constructor parameter.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleDeclaration, SyntaxKind.Attribute);
        }

        private static void HandleDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is AttributeSyntax attribute &&
                Attribute.IsType(attribute, KnownSymbol.ConstructorArgumentAttribute, context.SemanticModel, context.CancellationToken) &&
                ConstructorArgument.IsMatch(attribute, out var arg, out var parameterName) == false)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, arg.GetLocation(), parameterName));
            }
        }
    }
}