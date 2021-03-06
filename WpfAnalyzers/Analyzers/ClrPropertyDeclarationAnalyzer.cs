namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ClrPropertyDeclarationAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            WPF0003ClrPropertyShouldMatchRegisteredName.Descriptor,
            WPF0012ClrPropertyShouldMatchRegisteredType.Descriptor,
            WPF0032ClrPropertyGetAndSetSameDependencyProperty.Descriptor,
            WPF0035ClrPropertyUseSetValueInSetter.Descriptor,
            WPF0036AvoidSideEffectsInClrAccessors.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(x => Handle(x), SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                !context.ContainingSymbol.IsStatic &&
                context.ContainingSymbol is IPropertySymbol property &&
                property.ContainingType.IsAssignableTo(KnownSymbol.DependencyObject, context.Compilation) &&
                context.Node is PropertyDeclarationSyntax propertyDeclaration &&
                PropertyDeclarationWalker.TryGetCalls(propertyDeclaration, out var getCall, out var setCall))
            {
                if (setCall != null &&
                    propertyDeclaration.TryGetSetter(out var setter) &&
                    setter.Body != null &&
                    setter.Body.Statements.TryFirst(x => !x.Contains(setCall), out var statement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0036AvoidSideEffectsInClrAccessors.Descriptor, statement.GetLocation()));
                }

                if (getCall != null &&
                    propertyDeclaration.TryGetGetter(out var getter) &&
                    getter.Body != null &&
                    getter.Body.Statements.TryFirst(x => !x.Contains(getCall), out statement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(WPF0036AvoidSideEffectsInClrAccessors.Descriptor, statement.GetLocation()));
                }

                if (getCall.TryGetArgumentAtIndex(0, out var getArg) &&
                    getArg.Expression is IdentifierNameSyntax getIdentifier &&
                    setCall.TryGetArgumentAtIndex(0, out var setArg) &&
                    setArg.Expression is IdentifierNameSyntax setIdentifier &&
                    getIdentifier.Identifier.ValueText != setIdentifier.Identifier.ValueText &&
                    !setIdentifier.Identifier.ValueText.IsParts(getIdentifier.Identifier.ValueText, "Key"))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0032ClrPropertyGetAndSetSameDependencyProperty.Descriptor,
                            propertyDeclaration.GetLocation(),
                            context.ContainingSymbol.Name));
                }

                if (setCall.TryGetMethodName(out var setCallName) &&
                    setCallName != "SetValue")
                {
                    //// ReSharper disable once PossibleNullReferenceException
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            WPF0035ClrPropertyUseSetValueInSetter.Descriptor,
                            setCall.GetLocation(),
                            context.ContainingSymbol.Name));
                }

                if (ClrProperty.TryGetRegisterField(propertyDeclaration, context.SemanticModel, context.CancellationToken, out var fieldOrProperty))
                {
                    if (DependencyProperty.TryGetRegisteredName(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredName) &&
                        registeredName != property.Name)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                WPF0003ClrPropertyShouldMatchRegisteredName.Descriptor,
                                propertyDeclaration.Identifier.GetLocation(),
                                ImmutableDictionary<string, string>.Empty.Add("ExpectedName", registeredName),
                                property.Name,
                                registeredName));
                    }

                    if (DependencyProperty.TryGetRegisteredType(fieldOrProperty, context.SemanticModel, context.CancellationToken, out var registeredType) &&
                        !registeredType.Equals(property.Type))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                WPF0012ClrPropertyShouldMatchRegisteredType.Descriptor,
                                propertyDeclaration.Type.GetLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(nameof(TypeSyntax), registeredType.ToMinimalDisplayString(context.SemanticModel, context.Node.SpanStart)),
                                property,
                                registeredType));
                    }
                }
            }
        }

        private class PropertyDeclarationWalker : PooledWalker<PropertyDeclarationWalker>
        {
            private InvocationExpressionSyntax getCall;
            private InvocationExpressionSyntax setCall;

            public static bool TryGetCalls(PropertyDeclarationSyntax eventDeclaration, out InvocationExpressionSyntax getCall, out InvocationExpressionSyntax setCall)
            {
                using (var walker = BorrowAndVisit(eventDeclaration, () => new PropertyDeclarationWalker()))
                {
                    getCall = walker.getCall;
                    setCall = walker.setCall;
                    return getCall != null || setCall != null;
                }
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.TryGetMethodName(out var name) &&
                    (name == "SetValue" || name == "SetCurrentValue" || name == "GetValue") &&
                    node.FirstAncestor<AccessorDeclarationSyntax>() is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                    {
                        this.getCall = node;
                    }
                    else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                    {
                        this.setCall = node;
                    }
                }

                base.VisitInvocationExpression(node);
            }

            protected override void Clear()
            {
                this.getCall = null;
                this.setCall = null;
            }
        }
    }
}
