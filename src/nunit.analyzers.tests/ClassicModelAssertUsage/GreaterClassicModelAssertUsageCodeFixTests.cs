using System.Collections.Immutable;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.ClassicModelAssertUsage;
using NUnit.Analyzers.Constants;
using NUnit.Framework;

namespace NUnit.Analyzers.Tests.ClassicModelAssertUsage
{
    [TestFixture]
    public sealed class GreaterClassicModelAssertUsageCodeFixTests
    {
        private static readonly DiagnosticAnalyzer analyzer = new ClassicModelAssertUsageAnalyzer();
        private static readonly CodeFixProvider fix = new GreaterClassicModelAssertUsageCodeFix();
        private static readonly ExpectedDiagnostic expectedDiagnostic =
            ExpectedDiagnostic.Create(AnalyzerIdentifiers.GreaterUsage);

        [Test]
        public void VerifyGetFixableDiagnosticIds()
        {
            var fix = new GreaterClassicModelAssertUsageCodeFix();
            var ids = fix.FixableDiagnosticIds.ToImmutableArray();

            Assert.That(ids, Is.EquivalentTo(new[] { AnalyzerIdentifiers.GreaterUsage }));
        }

        [Test]
        public void VerifyGreaterFix()
        {
            var code = TestUtility.WrapMethodInClassNamespaceAndAddUsings($@"
        public void TestMethod()
        {{
            ↓Assert.Greater(2d, 3d);
        }}");
            var fixedCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void TestMethod()
        {
            Assert.That(2d, Is.GreaterThan(3d));
        }");
            AnalyzerAssert.CodeFix(analyzer, fix, expectedDiagnostic, code, fixedCode, fixTitle: CodeFixConstants.TransformToConstraintModelDescription);
        }

        [Test]
        public void VerifyGreaterFixWithMessage()
        {
            var code = TestUtility.WrapMethodInClassNamespaceAndAddUsings($@"
        public void TestMethod()
        {{
            ↓Assert.Greater(2d, 3d, ""message"");
        }}");
            var fixedCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void TestMethod()
        {
            Assert.That(2d, Is.GreaterThan(3d), ""message"");
        }");
            AnalyzerAssert.CodeFix(analyzer, fix, expectedDiagnostic, code, fixedCode, fixTitle: CodeFixConstants.TransformToConstraintModelDescription);
        }

        [Test]
        public void VerifyGreaterFixWithMessageAndParams()
        {
            var code = TestUtility.WrapMethodInClassNamespaceAndAddUsings($@"
        public void TestMethod()
        {{
            ↓Assert.Greater(2d, 3d, ""message"", Guid.NewGuid());
        }}");
            var fixedCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void TestMethod()
        {
            Assert.That(2d, Is.GreaterThan(3d), ""message"", Guid.NewGuid());
        }");
            AnalyzerAssert.CodeFix(analyzer, fix, expectedDiagnostic, code, fixedCode, fixTitle: CodeFixConstants.TransformToConstraintModelDescription);
        }

        [Test]
        public void CodeFixPreservesLineBreakBeforeMessage()
        {
            var code = TestUtility.WrapInTestMethod($@"
            Assert.Greater(2d, 3d,
                ""message"",
                Guid.NewGuid());");

            var fixedCode = TestUtility.WrapInTestMethod(@"
            Assert.That(2d, Is.GreaterThan(3d),
                ""message"",
                Guid.NewGuid());");

            AnalyzerAssert.CodeFix(analyzer, fix, expectedDiagnostic, code, fixedCode, fixTitle: CodeFixConstants.TransformToConstraintModelDescription);
        }
    }
}
