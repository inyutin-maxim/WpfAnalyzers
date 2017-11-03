﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ChangeValueConversionAttributeArgumentFix))]
    [Shared]
    internal class ChangeValueConversionAttributeArgumentFix : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(WPF0072ValueConversionMustUseCorrectTypes.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken)
                                              .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText))
                {
                    continue;
                }

                var argument = syntaxRoot.FindNode(diagnostic.Location.SourceSpan)
                                                 .FirstAncestorOrSelf<ArgumentSyntax>();
                var attribute = argument.FirstAncestor<AttributeSyntax>();
                if (argument != null &&
                    ValueConverter.TryGetConversionTypes(attribute.FirstAncestor<ClassDeclarationSyntax>(), semanticModel, context.CancellationToken, out var inType, out var outType))
                {
                    context.RegisterDocumentEditorFix(
                        $"Add default field.",
                        (e, _) => AddAttribute(e, attribute, inType, outType),
                        diagnostic);
                }
            }
        }

        private static void AddAttribute(DocumentEditor editor, AttributeSyntax attributeSyntax, ITypeSymbol inType, ITypeSymbol outType)
        {
            editor.ReplaceNode(
                attributeSyntax.ArgumentList.Arguments[0],
                editor.Generator.AttributeArgument(
                    editor.Generator.TypeOfExpression(editor.Generator.TypeExpression(inType))));

            editor.ReplaceNode(
                attributeSyntax.ArgumentList.Arguments[1],
                editor.Generator.AttributeArgument(
                    editor.Generator.TypeOfExpression(editor.Generator.TypeExpression(outType))));
        }
    }
}