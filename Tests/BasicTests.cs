using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using Xunit;

using DotDart;
using Shouldly;
using static Tests.Utility;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Tests
{
    public static class Utility
    {
        private const string preamble = "../../../";
        public static void VerifyDill(string file)
        {
            var source = $"{preamble}scripts/{file}.dart";
            var target = $"{preamble}scripts/{file}.dill";

            if (!File.Exists(source)) throw new Exception($"Source file {source} is missing.");
            // We optimistically believe the kernel file is correct
            if (File.Exists(target)) return;

            Dart.CompileDill(source, target);
        }

        public static ComponentFile Load(string file)
        {
            var target = $"{preamble}scripts/{file}.dill";
            return ComponentFile.Load(target);
        }
    }

    public class UnitTest1
    {
        private readonly ITestOutputHelper _test;

        // A setup where we verify dill files & versions?
        // Run tests against dill files?
        // Do we have tests where we load fill files? And then we have dependent tests of those
        public UnitTest1(ITestOutputHelper test)
        {
            _test = test;
            VerifyDill("hello");
        }

        [Fact]
        public void ComponentFilesMustLoad()
        {
            var hello = Load("hello");
            hello.ShouldNotBeNull();
        }

        [Fact]
        public void StringConcatenates()
        {
            // Baby steps towards useful unit tests
            var sc = new StringConcatenation(null, new []{new StringLiteral("hello, "), new StringLiteral("world!"), });

            sc = new StringConcatenation(null, new Expression[]{
                new StringLiteral("hello, "),
                new StaticGet("dart"),
                new StringLiteral("!"), });
            sc.Compile().ToString().ShouldBe("$\"hello, {dart}!\"");
        }

        [Fact]
        public void EquivalentRoslynProgram()
        {
            var compilationUnit = CompilationUnit()
                .WithUsings(
                    List<UsingDirectiveSyntax>(
                        new UsingDirectiveSyntax[]
                        {
                            UsingDirective(
                                IdentifierName("System")),
                            UsingDirective(
                                    QualifiedName(
                                        IdentifierName("DotDart"),
                                        IdentifierName("DartCore")))
                                .WithStaticKeyword(
                                    Token(SyntaxKind.StaticKeyword))
                        }))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        ClassDeclaration("Program")
                            .WithMembers(
                                List<MemberDeclarationSyntax>(
                                    new MemberDeclarationSyntax[]
                                    {
                                        FieldDeclaration(
                                                VariableDeclaration(
                                                        PredefinedType(
                                                            Token(SyntaxKind.StringKeyword)))
                                                    .WithVariables(
                                                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                                                            VariableDeclarator(
                                                                    Identifier("dart"))
                                                                .WithInitializer(
                                                                    EqualsValueClause(
                                                                        LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            Literal("dart-lang")))))))
                                            .WithModifiers(
                                                TokenList(
                                                    Token(SyntaxKind.StaticKeyword))),
                                        MethodDeclaration(
                                                PredefinedType(
                                                    Token(SyntaxKind.VoidKeyword)),
                                                Identifier("Main"))
                                            .WithModifiers(
                                                TokenList(
                                                    new[]
                                                    {
                                                        Token(SyntaxKind.PublicKeyword),
                                                        Token(SyntaxKind.StaticKeyword)
                                                    }))
                                            .WithBody(
                                                Block(
                                                    SingletonList<StatementSyntax>(
                                                        ExpressionStatement(
                                                            InvocationExpression(
                                                                    IdentifierName("print"))
                                                                .WithArgumentList(
                                                                    ArgumentList(
                                                                        SingletonSeparatedList<ArgumentSyntax>(
                                                                            Argument(
                                                                                InterpolatedStringExpression(
                                                                                        Token(SyntaxKind.InterpolatedStringStartToken))
                                                                                    .WithContents(
                                                                                        List<InterpolatedStringContentSyntax>(
                                                                                            new InterpolatedStringContentSyntax[]
                                                                                            {
                                                                                                InterpolatedStringText()
                                                                                                    .WithTextToken(
                                                                                                        Token(
                                                                                                            TriviaList(),
                                                                                                            SyntaxKind.InterpolatedStringTextToken,
                                                                                                            "hello from ",
                                                                                                            "hello from ",
                                                                                                            TriviaList())),
                                                                                                Interpolation(
                                                                                                    IdentifierName("dart")),
                                                                                                InterpolatedStringText()
                                                                                                    .WithTextToken(
                                                                                                        Token(
                                                                                                            TriviaList(),
                                                                                                            SyntaxKind.InterpolatedStringTextToken,
                                                                                                            "!",
                                                                                                            "!",
                                                                                                            TriviaList()))
                                                                                            }))))))))))
                                    }))))
                .NormalizeWhitespace();

            _test.WriteLine(compilationUnit.ToString());

            // https://github.com/dotnet/orleans/blob/master/src/Orleans.CodeGeneration/RoslynCodeGenerator.cs#L549

            // var model = CSharpCompilation.Create()

            // creation of the syntax tree for every file
            SyntaxTree programTree = compilationUnit.SyntaxTree;
            SyntaxTree[] sourceTrees = { programTree };

            // gathering the assemblies
            MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
            MetadataReference codeAnalysis = MetadataReference.CreateFromFile(typeof(SyntaxTree).GetTypeInfo().Assembly.Location);
            MetadataReference csharpCodeAnalysis = MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).GetTypeInfo().Assembly.Location);

            // todo: is there a better way to do this?
            MetadataReference dartCore = MetadataReference.CreateFromFile(typeof(DartCore).GetTypeInfo().Assembly.Location);

            MetadataReference[] references = { mscorlib, dartCore /*codeAnalysis, csharpCodeAnalysis*/ };

            // compilation
            var app = CSharpCompilation.Create("ConsoleApplication",
                sourceTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                // new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            //
            var stream = new MemoryStream();
            var emitResult = app.Emit(stream);
            _test.WriteLine($"{emitResult.Success} :: {string.Join(", ", emitResult.Diagnostics)}");
            var assembly = Assembly.Load(stream.GetBuffer());
            ExecuteFromAssembly(assembly);
        }

        private void ExecuteFromAssembly(Assembly assembly)
        {
            DartCore.printToZone = obj => _test.WriteLine($"core.print => {obj}");

            Type fooType = assembly.GetType("Program");
            MethodInfo printMethod = fooType.GetMethod("Main");
            object foo = assembly.CreateInstance("Program");
            var result = printMethod.Invoke(foo, BindingFlags.InvokeMethod, null, null, CultureInfo.CurrentCulture);
            // _test.WriteLine(result.ToString());
        }
    }
}