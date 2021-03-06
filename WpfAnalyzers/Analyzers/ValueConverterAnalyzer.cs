namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ValueConverterAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            WPF0070ConverterDoesNotHaveDefaultField.Descriptor,
            WPF0071ConverterDoesNotHaveAttribute.Descriptor,
            WPF0072ValueConversionMustUseCorrectTypes.Descriptor,
            WPF0073ConverterDoesNotHaveAttributeUnknownTypes.Descriptor,
            WPF0074DefaultMemberOfWrongType.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.ClassDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.ContainingSymbol is INamedTypeSymbol type &&
                type.IsAssignableToEither(KnownSymbol.IValueConverter, KnownSymbol.IMultiValueConverter, context.Compilation) &&
                context.Node is ClassDeclarationSyntax classDeclaration &&
                !type.IsAbstract &&
                type.DeclaredAccessibility != Accessibility.Private &&
                type.DeclaredAccessibility != Accessibility.Protected)
            {
                if (!type.IsAssignableTo(KnownSymbol.MarkupExtension, context.Compilation))
                {
                    if (ValueConverter.TryGetDefaultFieldsOrProperties(type, context.Compilation, out var defaults))
                    {
                        foreach (var fieldOrProperty in defaults)
                        {
                            if (fieldOrProperty.TryGetAssignedValue(context.CancellationToken, out var assignedValue) &&
                                context.SemanticModel.TryGetType(assignedValue, context.CancellationToken, out var assignedType) &&
                                !Equals(assignedType, type))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(WPF0074DefaultMemberOfWrongType.Descriptor, assignedValue.GetLocation()));
                            }
                        }
                    }
                    else if (!Virtual.HasVirtualOrAbstractOrProtectedMembers(type) &&
                             !type.Constructors.TryFirst(x => x.Parameters.Length > 0, out _) &&
                             !Mutable.HasMutableInstanceMembers(type))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(WPF0070ConverterDoesNotHaveDefaultField.Descriptor, classDeclaration.Identifier.GetLocation()));
                    }
                }

                if (type.IsAssignableTo(KnownSymbol.IValueConverter, context.Compilation))
                {
                    if (Attribute.TryFind(classDeclaration, KnownSymbol.ValueConversionAttribute, context.SemanticModel, context.CancellationToken, out var attribute))
                    {
                        if (ValueConverter.TryGetConversionTypes(classDeclaration, context.SemanticModel, context.CancellationToken, out var sourceType, out var targetType))
                        {
                            if (sourceType != null &&
                                sourceType != QualifiedType.System.Object &&
                                Attribute.TryFindArgument(attribute, 0, "sourceType", out var arg) &&
                                arg.Expression is TypeOfExpressionSyntax sourceTypeOf &&
                                TypeOf.TryGetType(sourceTypeOf, type, context.SemanticModel, context.CancellationToken, out var argType) &&
                                !Equals(argType, sourceType))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(WPF0072ValueConversionMustUseCorrectTypes.Descriptor, arg.GetLocation(), sourceType));
                            }

                            if (Attribute.TryFindArgument(attribute, 1, "targetType", out arg) &&
                                arg.Expression is TypeOfExpressionSyntax targetTypeOf &&
                                TypeOf.TryGetType(targetTypeOf, type, context.SemanticModel, context.CancellationToken, out argType) &&
                                !Equals(argType, targetType))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(WPF0072ValueConversionMustUseCorrectTypes.Descriptor, arg.GetLocation(), targetType));
                            }
                        }
                    }
                    else
                    {
                        if (ValueConverter.TryGetConversionTypes(classDeclaration, context.SemanticModel, context.CancellationToken, out _, out _))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(WPF0071ConverterDoesNotHaveAttribute.Descriptor, classDeclaration.Identifier.GetLocation()));
                        }
                        else
                        {
                            context.ReportDiagnostic(Diagnostic.Create(WPF0073ConverterDoesNotHaveAttributeUnknownTypes.Descriptor, classDeclaration.Identifier.GetLocation()));
                        }
                    }
                }
            }
        }
    }
}
