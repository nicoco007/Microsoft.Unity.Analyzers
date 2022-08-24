/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnityObjectImplicitBoolAnalyzer : DiagnosticAnalyzer
{
	internal static readonly DiagnosticDescriptor Rule = new(
		id: "UNT0031",
		title: Strings.UnityObjectImplicitBoolDiagnosticTitle,
		messageFormat: Strings.UnityObjectImplicitBoolDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: false,
		description: Strings.UnityObjectImplicitBoolDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterOperationAction(AnalyzeConversionOperation, OperationKind.Conversion);
	}

	private static void AnalyzeConversionOperation(OperationAnalysisContext context)
	{
		IConversionOperation operation = (IConversionOperation)context.Operation;

		bool isUnityObject = operation.Operand.Type.Extends(typeof(UnityEngine.Object));
		bool isCastToBool = operation.Type?.SpecialType == SpecialType.System_Boolean;

		if (!isUnityObject || !isCastToBool)
		{
			return;
		}

		var node =
			operation.Parent is IUnaryOperation unaryOperation && unaryOperation.OperatorKind == UnaryOperatorKind.Not
				? unaryOperation.Syntax
				: operation.Syntax;

		context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class UnityObjectImplicitBoolCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnityObjectImplicitBoolAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var operation = await context.GetFixableNodeAsync<ExpressionSyntax>(c => c is not PrefixUnaryExpressionSyntax);
		if (operation == null)
		     return;

		context.RegisterCodeFix(
		     CodeAction.Create(
		         Strings.UnityObjectImplicitBoolCodeFixTitle,
		         ct => ReplaceImplicitBoolConversionAsync(context.Document, operation, ct),
		         operation.ToFullString()),
		     context.Diagnostics);
	}

	private static async Task<Document> ReplaceImplicitBoolConversionAsync(Document document, ExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
	{
		bool isPrefixUnary = expressionSyntax.Parent is PrefixUnaryExpressionSyntax;

		// obj => obj != null, !obj => obj == null
		var kind = isPrefixUnary ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression;
		var source = isPrefixUnary ? expressionSyntax.Parent! : expressionSyntax;
		var replacement = SyntaxFactory.BinaryExpression(kind, expressionSyntax, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var newRoot = root?.ReplaceNode(source, replacement);

		return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
	}
}
