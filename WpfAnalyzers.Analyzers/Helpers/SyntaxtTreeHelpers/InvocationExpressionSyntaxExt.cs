﻿namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class InvocationExpressionSyntaxExt
    {
        internal static string Name(this InvocationExpressionSyntax invocation)
        {
            if (invocation == null)
            {
                return null;
            }

            switch (invocation.Kind())
            {
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.TypeOfExpression:
                    var identifierName = invocation.Expression as IdentifierNameSyntax;
                    if (identifierName == null)
                    {
                        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                        if (memberAccess != null)
                        {
                            identifierName = memberAccess.Name as IdentifierNameSyntax;
                        }
                    }

                    return identifierName?.Identifier.ValueText;
                default:
                    return null;
            }
        }

        internal static bool IsNameOf(this InvocationExpressionSyntax invocation)
        {
            return invocation.Name() == "nameof";
        }

        internal static bool IsGetValue(this InvocationExpressionSyntax invocation)
        {
            return invocation.Name() == Names.GetValue && invocation?.ArgumentList?.Arguments.Count == 1;
        }

        internal static bool IsSetValue(this InvocationExpressionSyntax invocation)
        {
            return invocation.Name() == Names.SetValue && invocation?.ArgumentList?.Arguments.Count == 2;
        }

        internal static bool IsSetSetCurrentValue(this InvocationExpressionSyntax invocation)
        {
            return invocation.Name() == Names.SetCurrentValue && invocation?.ArgumentList?.Arguments.Count == 2;
        }
    }
}