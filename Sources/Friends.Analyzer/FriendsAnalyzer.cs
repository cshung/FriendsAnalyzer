// <copyright file="FriendsAnalyzer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Friends.Analyzer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>The FriendsAnalyzer is a Roslyn Analyzer that can issue warnings for inappropriate calls to methods that are meant for friends only.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FriendsAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>The diagnostic identifier.</summary>
        public const string DiagnosticId = "FriendsAnalyzer";
        private const string Category = "Naming";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        /// <summary>Gets a set of descriptors for the diagnostics that this analyzer is capable of producing.</summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        /// <summary>Called once at session start to register actions in the analysis context.</summary>
        /// <param name="context">The analysis context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            SemanticModel semanticModel = context.SemanticModel;
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
            (IMethodSymbol method, List<INamedTypeSymbol> friends) = this.GetFriends(semanticModel, invocation, context.CancellationToken);
            INamedTypeSymbol containingType = this.GetContainingType(semanticModel, invocation);
            if ((friends.Count > 1) && (!friends.Any(friend => SymbolEqualityComparer.Default.Equals(friend, containingType))))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), invocation));
            }
        }

        private INamedTypeSymbol GetContainingType(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            TypeDeclarationSyntax containingTypeSyntax = invocation.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(containingTypeSyntax);
            return typeSymbol;
        }

        private (IMethodSymbol, List<INamedTypeSymbol>) GetFriends(SemanticModel semanticModel, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            List<INamedTypeSymbol> friends = new List<INamedTypeSymbol>();
            IMethodSymbol methodSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return (methodSymbol, friends);
            }

            friends.Add(methodSymbol.ContainingType);
            ImmutableArray<AttributeData> attributes = methodSymbol.GetAttributes();
            foreach (AttributeData attribute in attributes)
            {
                // TODO: Should we make sure this is our FriendsAttribute - not just any type named FriendsAttribute?
                if (attribute.AttributeClass.Name == "FriendsAttribute")
                {
                    // TODO: What if we have null type?
                    var friend = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    friends.Add(friend);
                }
            }

            return (methodSymbol, friends);
        }
    }
}
