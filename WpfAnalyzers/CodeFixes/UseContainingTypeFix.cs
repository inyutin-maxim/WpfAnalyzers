namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseContainingTypeFix))]
    [Shared]
    internal class UseContainingTypeFix : DocumentEditorCodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            WPF0011ContainingTypeShouldBeRegisteredOwner.DiagnosticId,
            WPF0101RegisterContainingTypeAsOwner.DiagnosticId,
            WPF0121RegisterContainingTypeAsOwnerForRoutedCommand.DiagnosticId,
            WPF0140UseContainingTypeComponentResourceKey.DiagnosticId);

        /// <inheritdoc/>
        protected override async Task RegisterCodeFixesAsync(DocumentEditorCodeFixContext context)
        {
            var document = context.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken)
                                              .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (syntaxRoot.TryFindNodeOrAncestor(diagnostic, out TypeOfExpressionSyntax typeofExpression) &&
                    typeofExpression.TryFirstAncestor(out TypeDeclarationSyntax typeDeclaration) &&
                    semanticModel.TryGetSymbol(typeDeclaration, context.CancellationToken, out var containingType))
                {
                    var containingTypeName = containingType.ToMinimalDisplayString(semanticModel, typeofExpression.SpanStart, SymbolDisplayFormat.MinimallyQualifiedFormat);

                    context.RegisterCodeFix(
                        $"Use containing type: {containingTypeName}.",
                        (editor, _) => editor.ReplaceNode(
                            typeofExpression,
                            x => x.WithType(SyntaxFactory.ParseTypeName(containingTypeName))),
                        "Use containing type.",
                        diagnostic);
                }
            }
        }
    }
}
