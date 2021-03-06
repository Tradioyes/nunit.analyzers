using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.Extensions;
using NUnit.Analyzers.Helpers;
using NUnit.Analyzers.Syntax;

namespace NUnit.Analyzers.SameAsOnValueTypes
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SameAsOnValueTypesAnalyzer : BaseAssertionAnalyzer
    {
        private static readonly DiagnosticDescriptor descriptor = DiagnosticDescriptorCreator.Create(
            id: AnalyzerIdentifiers.SameAsOnValueTypes,
            title: SameAsOnValueTypesConstants.Title,
            messageFormat: SameAsOnValueTypesConstants.Message,
            category: Categories.Assertion,
            defaultSeverity: DiagnosticSeverity.Error,
            description: SameAsOnValueTypesConstants.Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        protected override void AnalyzeAssertInvocation(SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax assertExpression, IMethodSymbol methodSymbol)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;

            ExpressionSyntax? actualExpression;
            ExpressionSyntax? expectedExpression = null;

            if (methodSymbol.Name.Equals(NunitFrameworkConstants.NameOfAssertAreSame) ||
                methodSymbol.Name.Equals(NunitFrameworkConstants.NameOfAssertAreNotSame))
            {
                actualExpression = assertExpression.GetArgumentExpression(methodSymbol, NunitFrameworkConstants.NameOfActualParameter);
                expectedExpression = assertExpression.GetArgumentExpression(methodSymbol, NunitFrameworkConstants.NameOfExpectedParameter);
            }
            else
            {
                if (!AssertHelper.TryGetActualAndConstraintExpressions(assertExpression, semanticModel,
                    out actualExpression, out var constraintExpression))
                {
                    return;
                }

                foreach (var constraintPartExpression in constraintExpression.ConstraintParts)
                {
                    if (HasIncompatiblePrefixes(constraintPartExpression)
                        || constraintPartExpression.HasUnknownExpressions())
                    {
                        return;
                    }

                    var constraintMethod = constraintPartExpression.GetConstraintMethod();

                    if (constraintMethod?.Name != NunitFrameworkConstants.NameOfIsSameAs
                        || constraintMethod.ReturnType?.GetFullMetadataName() != NunitFrameworkConstants.FullNameOfSameAsConstraint)
                    {
                        continue;
                    }

                    expectedExpression = constraintPartExpression.GetExpectedArgumentExpression();
                    break;
                }
            }

            if (actualExpression == null || expectedExpression == null)
                return;

            var actualType = AssertHelper.GetUnwrappedActualType(actualExpression, semanticModel, cancellationToken);

            var expectedType = semanticModel.GetTypeInfo(expectedExpression, cancellationToken).Type;

            if (actualType == null || expectedType == null)
                return;

            if (actualType.IsValueType || expectedType.IsValueType)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor,
                    assertExpression.GetLocation()));
            }
        }

        private static bool HasIncompatiblePrefixes(ConstraintPartExpression constraintPartExpression)
        {
            // Currently only 'Not' suffix supported, as all other suffixes change actual type for constraint
            // (e.g. All, Some, Property, Count, etc.)

            return constraintPartExpression.GetPrefixesNames().Any(s => s != NunitFrameworkConstants.NameOfIsNot);
        }
    }
}
