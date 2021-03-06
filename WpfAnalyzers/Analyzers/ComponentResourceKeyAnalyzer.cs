namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ComponentResourceKeyAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            WPF0140UseContainingTypeComponentResourceKey.Descriptor,
            WPF0141UseContainingMemberComponentResourceKey.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Handle(c), SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.ArgumentList is ArgumentListSyntax argumentList &&
                objectCreation.Type == KnownSymbol.ComponentResourceKey &&
                context.SemanticModel.TryGetSymbol(objectCreation, KnownSymbol.ComponentResourceKey, context.CancellationToken, out var constructor) &&
                FieldOrProperty.TryCreate(context.ContainingSymbol, out var fieldOrProperty))
            {
                if (constructor.Parameters.Length == 0)
                {
                    var containingTypeString = context.ContainingSymbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, objectCreation.SpanStart);
                    var argumentListText = $"typeof({containingTypeString}), nameof({fieldOrProperty.Name})";
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0140UseContainingTypeComponentResourceKey.Descriptor,
                            argumentList.GetLocation(),
                            ImmutableDictionary<string, string>.Empty.Add(nameof(ArgumentListSyntax), argumentListText),
                            argumentListText));
                }
                else
                {
                    if (constructor.TryFindParameter("typeInTargetAssembly", out var parameter) &&
                        objectCreation.TryFindArgument(parameter, out var arg) &&
                        arg.TryGetTypeofValue(context.SemanticModel, context.CancellationToken, out var type) &&
                        !type.Equals(context.ContainingSymbol.ContainingType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                WPF0140UseContainingTypeComponentResourceKey.Descriptor,
                                arg.GetLocation(),
                                context.ContainingSymbol.ContainingType.ToMinimalDisplayString(context.SemanticModel, objectCreation.SpanStart)));
                    }

                    if (constructor.TryFindParameter("resourceId", out parameter) &&
                        objectCreation.TryFindArgument(parameter, out arg) &&
                        context.SemanticModel.TryGetConstantValue(arg.Expression, context.CancellationToken, out string name) &&
                        name != fieldOrProperty.Name)
                    {
                        var keyText = $"nameof({fieldOrProperty.Name})";
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                WPF0141UseContainingMemberComponentResourceKey.Descriptor,
                                arg.GetLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(nameof(ArgumentSyntax), keyText),
                                keyText));
                    }
                }
            }
        }
    }
}
