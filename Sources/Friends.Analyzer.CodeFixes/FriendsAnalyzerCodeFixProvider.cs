// <copyright file="FriendsAnalyzerCodeFixProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Friends.Analyzer
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    /// <summary>A FriendsAnalyzerCodeFixProvider is a Roslyn code fix provider that can fix inappropriate calls to methods that are meant for friends only.</summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FriendsAnalyzerCodeFixProvider))]
    [Shared]
    public class FriendsAnalyzerCodeFixProvider : CodeFixProvider
    {
        /// <summary>Gets a list of diagnostic IDs that this provider can provide fixes for.</summary>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FriendsAnalyzer.DiagnosticId); }
        }

        /// <summary>
        /// Gets an optional <see cref="T:Microsoft.CodeAnalysis.CodeFixes.FixAllProvider">FixAllProvider</see> that can fix all/multiple occurrences of diagnostics fixed by this code fix provider.
        /// Return null if the provider doesn't support fix all/multiple occurrences.
        /// Otherwise, you can return any of the well known fix all providers from <see cref="T:Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders">WellKnownFixAllProviders</see> or implement your own fix all provider.
        /// </summary>
        /// <returns>
        ///   <br />
        /// </returns>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>Computes one or more fixes for the specified <see cref="T:Microsoft.CodeAnalysis.CodeFixes.CodeFixContext">CodeFixContext</see>.</summary>
        /// <param name="context">
        /// A <see cref="T:Microsoft.CodeAnalysis.CodeFixes.CodeFixContext">CodeFixContext</see> containing context information about the diagnostics to fix.
        /// The context must only contain diagnostics with a <see cref="P:Microsoft.CodeAnalysis.Diagnostic.Id">Id</see> included in the <see cref="P:Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider.FixableDiagnosticIds">FixableDiagnosticIds</see> for the current provider.
        /// </param>
        /// <returns>The task.</returns>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the invocation identified by the diagnostic.
                InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)root.FindNode(diagnosticSpan);

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitle,
                        createChangedSolution: c => this.AddFriendsAttributeAsync(context.Document, invocation, c),
                        equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                    diagnostic);
            }
        }

        private async Task<Solution> AddFriendsAttributeAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            IMethodSymbol methodSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;

            TypeDeclarationSyntax containingTypeSyntax = invocation.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(containingTypeSyntax);

            MethodDeclarationSyntax methodDeclaration = (await methodSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken)) as MethodDeclarationSyntax;

            // TODO: The generated code formatting is not quite right
            // TODO: Will it generates the fully qualified name if the namespace is not available?
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var attributes = methodDeclaration.AttributeLists.Add(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Friends"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(
                    SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(typeSymbol.ToDisplayString())))))))).NormalizeWhitespace());

            return document.WithSyntaxRoot(
                root.ReplaceNode(
                    methodDeclaration,
                    methodDeclaration.WithAttributeLists(attributes).WithAdditionalAnnotations(Formatter.Annotation))).Project.Solution;
        }
    }
}
