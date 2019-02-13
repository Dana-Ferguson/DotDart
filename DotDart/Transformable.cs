using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DotDart
{
    public class DartTransformationException : Exception
    {
        public readonly Type Target;
        public readonly object Transformable;

        public override string ToString()
        {
            try
            {
                var b = new DartStringBuilder();
                b.AppendLine($"DartType = {Transformable?.GetType().ToString() ?? "NULL"};").AppendLine($"TargetType = {Target?.Name};");
                b.Serialize(Transformable);
                return b.ToString();
            }
            catch (Exception exception)
            {
                return $"Exception Serialization Failed: {exception}";
            }
        }

        public override string Message => ToString();

        public DartTransformationException(object transformable, Type target)
        {
            Target = target;
            Transformable = transformable;
        }
    }

    public static class TransformableExtensions
    {
        // todo: don't base on the `object`
        public static ExpressionSyntax ToExpressionSyntax(this object transformable)
        {
            var est = transformable as IExpressionSyntax ?? throw new DartTransformationException(transformable, typeof(IExpressionSyntax));
            return est.ToExpressionSyntax();
        }

        public static SyntaxToken ToSyntaxToken(this object transformable)
        {
            var x = transformable as ISyntaxToken ?? throw new DartTransformationException(transformable, typeof(ISyntaxToken));
            return x.ToSyntaxToken();
        }

        public static StatementSyntax ToStatementSyntax(this object transformable)
        {
            var x = transformable as IStatementSyntax ?? throw new DartTransformationException(transformable, typeof(IStatementSyntax));
            return x.ToStatementSyntax();
        }

        public static TypeSyntax ToTypeSyntax(this object transformable)
        {
            var x = transformable as ITypeSyntax ?? throw new DartTransformationException(transformable, typeof(ITypeSyntax));
            return x.ToTypeSyntax();
        }

        public static LiteralExpressionSyntax ToLiteralExpressionSyntax(this object transformable)
        {
            var x = transformable as ILiteralExpressionSyntax ?? throw new DartTransformationException(transformable, typeof(ILiteralExpressionSyntax));
            return x.ToLiteralExpressionSyntax();
        }
    }

    public interface IExpressionSyntax
    {
        ExpressionSyntax ToExpressionSyntax();
    }

    public interface ISyntaxToken
    {
        SyntaxToken ToSyntaxToken();
    }

    public interface IStatementSyntax
    {
        StatementSyntax ToStatementSyntax();
    }

    public interface ITypeSyntax
    {
        TypeSyntax ToTypeSyntax();
    }

    public interface ILiteralExpressionSyntax
    {
        LiteralExpressionSyntax ToLiteralExpressionSyntax();
    }
}