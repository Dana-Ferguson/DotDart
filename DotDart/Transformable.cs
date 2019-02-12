using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DotDart
{
    public static class TransformableExtensions
    {
        // todo: don't base on the `object`
        public static ExpressionSyntax ToExpressionSyntax(this object transformable)
        {
            var est = transformable as IExpressionSyntax ?? throw new Exception($"Could not transform {transformable.GetType()}:{transformable} to {typeof(IExpressionSyntax)}.");
            return est.ToExpressionSyntax();
        }

        public static SyntaxToken ToSyntaxToken(this object transformable)
        {
            var x = transformable as ISyntaxToken ?? throw new Exception($"Could not transform {transformable.GetType()}:{transformable} to {typeof(ISyntaxToken)}.");
            return x.ToSyntaxToken();
        }

        public static StatementSyntax ToStatementSyntax(this object transformable)
        {
            var x = transformable as IStatementSyntax ?? throw new Exception($"Could not transform {transformable.GetType()}:{transformable} to {typeof(IStatementSyntax)}.");
            return x.ToStatementSyntax();
        }

        public static TypeSyntax ToTypeSyntax(this object transformable)
        {
            var x = transformable as ITypeSyntax ?? throw new Exception($"Could not transform {transformable.GetType()}:{transformable} to {typeof(ITypeSyntax)}.");
            return x.ToTypeSyntax();
        }

        public static LiteralExpressionSyntax ToLiteralExpressionSyntax(this object transformable)
        {
            var x = transformable as ILiteralExpressionSyntax ?? throw new Exception($"Could not transform {transformable.GetType()}:{transformable} to {typeof(ITypeSyntax)}.");
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