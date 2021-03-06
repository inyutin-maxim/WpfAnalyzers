namespace WpfAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class AttributeAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            WPF0051XmlnsDefinitionMustMapExistingNamespace.Descriptor,
            WPF0081MarkupExtensionReturnTypeMustUseCorrectType.Descriptor,
            WPF0082ConstructorArgument.Descriptor,
            WPF0084XamlSetMarkupExtensionAttributeTarget.Descriptor,
            WPF0085XamlSetTypeConverterTarget.Descriptor,
            WPF0132UsePartPrefix.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.Attribute);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is AttributeSyntax attribute)
            {
                if (Attribute.IsType(attribute, KnownSymbol.XmlnsDefinitionAttribute, context.SemanticModel, context.CancellationToken) &&
                    Attribute.TryFindArgument(attribute, 1, KnownSymbol.XmlnsDefinitionAttribute.ClrNamespaceArgumentName, out var arg) &&
                    context.SemanticModel.TryGetConstantValue(arg.Expression, context.CancellationToken, out string @namespace) &&
                    context.Compilation.GetSymbolsWithName(x => !string.IsNullOrEmpty(x) && @namespace.EndsWith(x), SymbolFilter.Namespace)
                           .All(x => x.ToMinimalDisplayString(context.SemanticModel, 0) != @namespace))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0051XmlnsDefinitionMustMapExistingNamespace.Descriptor, arg.GetLocation(), arg));
                }
                else if (Attribute.IsType(attribute, KnownSymbol.MarkupExtensionReturnTypeAttribute, context.SemanticModel, context.CancellationToken) &&
                         context.ContainingSymbol is ITypeSymbol containingType &&
                         !containingType.IsAbstract &&
                         containingType.IsAssignableTo(KnownSymbol.MarkupExtension, context.Compilation) &&
                         attribute.TryFirstAncestor<ClassDeclarationSyntax>(out var classDeclaration) &&
                         MarkupExtension.TryGetReturnType(classDeclaration, context.SemanticModel, context.CancellationToken, out var returnType) &&
                         returnType != KnownSymbol.Object &&
                         Attribute.TryFindArgument(attribute, 0, "returnType", out arg) &&
                         arg.Expression is TypeOfExpressionSyntax typeOf &&
                         context.SemanticModel.TryGetType(typeOf.Type, context.CancellationToken, out var argType) &&
                         !ReferenceEquals(argType, returnType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0081MarkupExtensionReturnTypeMustUseCorrectType.Descriptor, arg.GetLocation(), returnType));
                }
                else if (Attribute.IsType(attribute, KnownSymbol.ConstructorArgumentAttribute, context.SemanticModel, context.CancellationToken) &&
                         ConstructorArgument.TryGetArgumentName(attribute, out var argument, out var argumentName) &&
                         attribute.TryFirstAncestor<PropertyDeclarationSyntax>(out var propertyDeclaration) &&
                         context.SemanticModel.TryGetSymbol(propertyDeclaration, context.CancellationToken, out var property) &&
                         ConstructorArgument.TryGetParameterName(property, context.SemanticModel, context.CancellationToken, out var parameterName) &&
                         argumentName != parameterName)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0082ConstructorArgument.Descriptor,
                            argument.GetLocation(),
                            ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string>(nameof(ConstructorArgument), parameterName) }),
                            parameterName));
                }
                else if (Attribute.IsType(attribute, KnownSymbol.XamlSetMarkupExtensionAttribute, context.SemanticModel, context.CancellationToken) &&
                         Attribute.TryFindArgument(attribute, 0, "xamlSetMarkupExtensionHandler", out arg) &&
                         context.SemanticModel.TryGetConstantValue(arg.Expression, context.CancellationToken, out string target) &&
                         !(context.ContainingSymbol as ITypeSymbol).TryFindFirstMethodRecursive(target, m => IsMarkupExtensionHandler(m), out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0084XamlSetMarkupExtensionAttributeTarget.Descriptor, arg.Expression.GetLocation()));
                }
                else if (Attribute.IsType(attribute, KnownSymbol.XamlSetTypeConverterAttribute, context.SemanticModel, context.CancellationToken) &&
                         Attribute.TryFindArgument(attribute, 0, "xamlSetTypeConverterHandler", out arg) &&
                         context.SemanticModel.TryGetConstantValue(arg.Expression, context.CancellationToken, out target) &&
                         !(context.ContainingSymbol as ITypeSymbol).TryFindFirstMethodRecursive(target, m => IsTypeConverterHandler(m), out _))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0085XamlSetTypeConverterTarget.Descriptor, arg.Expression.GetLocation()));
                }
                else if (Attribute.IsType(attribute, KnownSymbol.TemplatePartAttribute, context.SemanticModel, context.CancellationToken) &&
                         Attribute.TryFindArgument(attribute, 0, "Name", out arg) &&
                         context.SemanticModel.TryGetConstantValue(arg.Expression, context.CancellationToken, out string partName) &&
                         !partName.StartsWith("PART_"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0132UsePartPrefix.Descriptor, arg.Expression.GetLocation(), arg));
                }
            }
        }

        private static bool IsMarkupExtensionHandler(IMethodSymbol candidate)
        {
            return candidate.ReturnsVoid &&
                   candidate.Parameters.Length == 2 &&
                   candidate.Parameters.TryElementAt(0, out var parameter) &&
                   parameter.Type == KnownSymbol.Object &&
                   candidate.Parameters.TryElementAt(1, out parameter) &&
                   parameter.Type == KnownSymbol.XamlSetMarkupExtensionEventArgs;
        }

        private static bool IsTypeConverterHandler(IMethodSymbol candidate)
        {
            return candidate.ReturnsVoid &&
                   candidate.Parameters.Length == 2 &&
                   candidate.Parameters.TryElementAt(0, out var parameter) &&
                   parameter.Type == KnownSymbol.Object &&
                   candidate.Parameters.TryElementAt(1, out parameter) &&
                   parameter.Type == KnownSymbol.XamlSetTypeConverterEventArgs;
        }
    }
}
