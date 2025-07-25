﻿using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Franken.SourceGenerator;

[Generator]
public class ObservableSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: (generatorSyntaxContext, _) =>
                {
                    var classDeclaration = (ClassDeclarationSyntax)generatorSyntaxContext.Node;
                    var classSymbol = generatorSyntaxContext.SemanticModel.GetDeclaredSymbol(classDeclaration);
                    return new { ClassDeclaration = classDeclaration, ClassSymbol = classSymbol };
                })
            .Where(t => t.ClassDeclaration is not null
                     && t.ClassDeclaration.HasAttribute("ObservableObject")
                     && t.ClassDeclaration.IsPartialClass())
            .Collect();

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations);

        context.RegisterSourceOutput(compilationAndClasses, (sourceProductionContext, tuple) =>
        {
            foreach (var item in tuple.Right)
            {
                var classDeclaration = item.ClassDeclaration;
                var classSymbol = item.ClassSymbol;

                var source = GenerateSourceCode(classDeclaration, classSymbol);
                sourceProductionContext.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static string GenerateSourceCode(ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol)
    {
        StringBuilder sb = new();

        string className = classDeclaration.Identifier.Text, namesp = classSymbol.ContainingNamespace?.ToString() ?? string.Empty;
        var starting = namesp == string.Empty ? string.Empty : $"namespace {namesp};\n";

        var usings = classDeclaration.GetUsingsInFile();
        foreach (var us in usings) sb.AppendLine($"using {us};");
        if (!usings.Contains("System")) sb.AppendLine("using System;");

        sb.Append($@"
{starting}
public partial class {className}
{{");
        foreach (var field in classDeclaration.ChildNodes()
                                              .OfType<FieldDeclarationSyntax>()
                                              .Where(n => n.HasAttribute("ObservableProperty")))
        {
            var fieldType = field.Declaration.Type;
            var fieldName = field.Declaration.Variables.First().Identifier.Text;
            var fieldNameNew = char.ToUpper(fieldName[0]) + fieldName.Substring(1);

            var validate = field.GetAttribute("ObservableProperty").GetUnique<LiteralExpressionSyntax>()?.Token.ValueText ?? string.Empty;

            sb.Append($@"
    /// <summary>On{fieldNameNew}Change({fieldType} oldValue, {fieldType} newValue)</summary>
    public Action<{fieldType}, {fieldType}> On{fieldNameNew}Change;
    public {fieldType} {fieldNameNew}
    {{
        get => {fieldName};
        set 
        {{");
            if (!string.IsNullOrEmpty(validate)) sb.Append($@"
            if (!{validate}(value)) return;");
            sb.Append($@"
            On{fieldNameNew}Change?.Invoke({fieldName}, value);
            {fieldName} = value;
        }}
    }}
                    ");
        }

        sb.Append("\n}");

        return sb.ToString();
    }
}
