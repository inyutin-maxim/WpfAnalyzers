namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ClrMethodDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            WPF0004ClrMethodShouldMatchRegisteredName.Descriptor,
            WPF0013ClrMethodMustMatchRegisteredType.Descriptor,
            WPF0033UseAttachedPropertyBrowsableForTypeAttribute.Descriptor,
            WPF0034AttachedPropertyBrowsableForTypeAttributeArgument.Descriptor,
            WPF0042AvoidSideEffectsInClrAccessors.Descriptor,
            WPF0061DocumentClrMethod.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.MethodDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is MethodDeclarationSyntax methodDeclaration &&
                context.ContainingSymbol is IMethodSymbol method &&
                method.IsStatic &&
                method.Parameters.TryElementAt(0, out var parameter) &&
                parameter.Type.IsAssignableTo(KnownSymbol.DependencyObject, context.Compilation))
            {
                if (method.Parameters.TryElementAt(1, out var valueParameter) &&
                    ClrMethod.IsAttachedSet(methodDeclaration, context.SemanticModel, context.CancellationToken, out var setValueCall, out var fieldOrProperty))
                {
                    if (DependencyProperty.TryGetRegisteredName(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredName) &&
                        !method.Name.IsParts("Set", registeredName))
                    {
                        context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0004ClrMethodShouldMatchRegisteredName.Descriptor,
                            methodDeclaration.Identifier.GetLocation(),
                            ImmutableDictionary<string, string>.Empty.Add("ExpectedName", "Set" + registeredName),
                            method.Name,
                            "Set" + registeredName));
                    }

                    if (DependencyProperty.TryGetRegisteredType(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredType) &&
                        !Equals(valueParameter.Type, registeredType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                WPF0013ClrMethodMustMatchRegisteredType.Descriptor,
                                methodDeclaration.ParameterList.Parameters[1].Type.GetLocation(),
                                "Value type",
                                registeredType));
                    }

                    if (methodDeclaration.Body is BlockSyntax body &&
                        body.Statements.TryFirst(x => !x.Contains(setValueCall), out var statement))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(WPF0042AvoidSideEffectsInClrAccessors.Descriptor, statement.GetLocation()));
                    }

                    if (method.DeclaredAccessibility.IsEither(Accessibility.Protected, Accessibility.Internal, Accessibility.Public) &&
                        !methodDeclaration.TryGetDocumentationComment(out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(WPF0061DocumentClrMethod.Descriptor, methodDeclaration.Identifier.GetLocation()));
                    }
                }
                else if (ClrMethod.IsAttachedGet(methodDeclaration, context.SemanticModel, context.CancellationToken, out var getValueCall, out fieldOrProperty))
                {
                    if (DependencyProperty.TryGetRegisteredName(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredName) &&
                        !method.Name.IsParts("Get", registeredName))
                    {
                        context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0004ClrMethodShouldMatchRegisteredName.Descriptor,
                            methodDeclaration.Identifier.GetLocation(),
                            ImmutableDictionary<string, string>.Empty.Add("ExpectedName", "Get" + registeredName),
                            method.Name,
                            "Get" + registeredName));
                    }

                    if (DependencyProperty.TryGetRegisteredType(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredType) &&
                        !Equals(method.ReturnType, registeredType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                WPF0013ClrMethodMustMatchRegisteredType.Descriptor,
                                methodDeclaration.ReturnType.GetLocation(),
                                "Return type",
                                registeredType));
                    }

                    if (method.DeclaredAccessibility.IsEither(Accessibility.Protected, Accessibility.Internal, Accessibility.Public) &&
                        !methodDeclaration.TryGetDocumentationComment(out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(WPF0061DocumentClrMethod.Descriptor, methodDeclaration.Identifier.GetLocation()));
                    }

                    if (methodDeclaration.Body is BlockSyntax body &&
                        body.Statements.TryFirst(x => !x.Contains(getValueCall), out var statement))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(WPF0042AvoidSideEffectsInClrAccessors.Descriptor, statement.GetLocation()));
                    }

                    if (Attribute.TryFind(methodDeclaration, KnownSymbol.AttachedPropertyBrowsableForTypeAttribute, context.SemanticModel, context.CancellationToken, out var attribute))
                    {
                        if (attribute.TrySingleArgument(out var argument) &&
                            argument.Expression is TypeOfExpressionSyntax typeOf &&
                            TypeOf.TryGetType(typeOf, method.ContainingType, context.SemanticModel, context.CancellationToken, out var argumentType) &&
                            !argumentType.IsAssignableTo(parameter.Type, context.Compilation))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    WPF0034AttachedPropertyBrowsableForTypeAttributeArgument.Descriptor,
                                    argument.GetLocation(),
                                    parameter.Type.ToMinimalDisplayString(
                                        context.SemanticModel,
                                        argument.SpanStart)));
                        }
                    }
                    else
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                    WPF0033UseAttachedPropertyBrowsableForTypeAttribute.Descriptor,
                                    methodDeclaration.Identifier.GetLocation(),
                                    parameter.Type.ToMinimalDisplayString(context.SemanticModel, methodDeclaration.SpanStart)));
                    }
                }
            }
        }
    }
}
