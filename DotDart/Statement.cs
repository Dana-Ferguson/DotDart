using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace DotDart
{

  public static class StatementExtensions
  {
    public static Statement ReadStatement(this ComponentReader reader)
    {
      var tag = reader.ReadByte();
      switch (tag)
      {
        case AssertBlock.Tag: return new AssertBlock(reader);
        case AssertStatement.Tag: return new AssertStatement(reader);
        case AsyncForInStatement.Tag: return new AsyncForInStatement(reader);
        case Block.Tag: return new Block(reader);
        case BreakStatement.Tag: return new BreakStatement(reader);
        case ContinueSwitchStatement.Tag: return new ContinueSwitchStatement(reader);
        case DoStatement.Tag: return new DoStatement(reader);
        case EmptyStatement.Tag: return new EmptyStatement();
        case ExpressionStatement.Tag: return new ExpressionStatement(reader);
        case ForInStatement.Tag: return new ForInStatement(reader);
        case ForStatement.Tag: return new ForStatement(reader);
        case FunctionDeclaration.Tag: return new FunctionDeclaration(reader);
        case IfStatement.Tag: return new IfStatement(reader);
        case LabeledStatement.Tag: return new LabeledStatement(reader);
        case ReturnStatement.Tag: return new ReturnStatement(reader);
        case SwitchStatement.Tag: return new SwitchStatement(reader);
        case TryCatch.Tag: return new TryCatch(reader);
        case TryFinally.Tag: return new TryFinally(reader);
        case VariableDeclarationStatement.Tag: return new VariableDeclarationStatement(reader);
        case WhileStatement.Tag: return new WhileStatement(reader);
        case YieldStatement.Tag: return new YieldStatement(reader);
        default: throw new Exception($"{nameof(Statement)}: unrecognized tag ({tag})");
      }
    }
  }

  public interface Statement : Node
  {
  }

  public class ExpressionStatement : Statement, IStatementSyntax
  {
    public byte tag => Tag;
    public const byte Tag = 61;
    public readonly Expression expression;

    public ExpressionStatement(ComponentReader reader)
    {
      expression = reader.ReadExpression();
    }

    public ExpressionStatement(Expression expression)
    {
      this.expression = expression;
    }


    public StatementSyntax ToStatementSyntax()
    {
      // todo: pretty sure this is wrong
      return SF.ExpressionStatement(expression.ToExpressionSyntax());
      // return SF.EqualsValueClause(
      //  expression.ToLiteralExpressionSyntax());
    }
  }

  public class Block : Statement, IStatementSyntax
  {
    public byte tag => Tag;
    public const byte Tag = 62;
    public readonly List<Statement> statements;

    public Block(ComponentReader reader)
    {
      statements = reader.ReadList(r => r.ReadStatement());
    }

    public Block(List<Statement> statements)
    {
      this.statements = statements;
    }

    public StatementSyntax ToStatementSyntax()
    {
      var rStatements = new List<StatementSyntax>();
      foreach (var statement in statements)
      {
        rStatements.Add(statement.ToStatementSyntax());
      }

      if (statements.Count == 0) throw new Exception("Improper call");

      if (statements.Count == 1)
      {
        // todo: is this needed?
        return SF.Block(SF.SingletonList<StatementSyntax>(rStatements.First()));
      }

      return SF.Block(rStatements);
    }
  }

  public class AssertBlock : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 81;
    public readonly List<Statement> statements;

    public AssertBlock(ComponentReader reader)
    {
      statements = reader.ReadList(r => r.ReadStatement());
    }

    public AssertBlock(List<Statement> statements)
    {
      this.statements = statements;
    }
  }

  public class EmptyStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 63;
  }

  public class AssertStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 64;
    public readonly Expression condition;
    public readonly FileOffset conditionStartOffset;
    public readonly FileOffset conditionEndOffset;
    public readonly Option<Expression> message;

    public AssertStatement(ComponentReader reader)
    {
      condition = reader.ReadExpression();
      conditionStartOffset = new FileOffset(reader);
      conditionEndOffset = new FileOffset(reader);
      message = reader.ReadOption(r => r.ReadExpression());
    }

    public AssertStatement(Expression condition, FileOffset conditionStartOffset, FileOffset conditionEndOffset, Option<Expression> message)
    {
      this.condition = condition;
      this.conditionStartOffset = conditionStartOffset;
      this.conditionEndOffset = conditionEndOffset;
      this.message = message;
    }

    public AssertStatement(Expression condition)
    {
      this.condition = condition;
      message = new Nothing<Expression>();
    }

    public AssertStatement(Expression condition, Expression message)
    {
      this.condition = condition;
      this.message = new Something<Expression>(message);
    }
  }

  public class LabeledStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 65;
    public readonly Statement body;

    public LabeledStatement(ComponentReader reader)
    {
      body = reader.ReadStatement();
    }

    public LabeledStatement(Statement body)
    {
      this.body = body;
    }
  }

  public class BreakStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 66;
    public readonly FileOffset fileOffset;

    // Reference to the Nth LabeledStatement in scope, with 0 being the
    // outermost enclosing labeled statement within the same FunctionNode.
    //
    // Labels are not in scope across function boundaries.
    public readonly uint labelIndex;

    public BreakStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      labelIndex = reader.ReadUint();
    }

    public BreakStatement(FileOffset fileOffset, uint labelIndex)
    {
      this.fileOffset = fileOffset;
      this.labelIndex = labelIndex;
    }

    [Testing]
    public BreakStatement(uint labelIndex)
    {
      this.labelIndex = labelIndex;
    }

  }

  public class WhileStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 67;
    public readonly FileOffset fileOffset;
    public readonly Expression condition;
    public readonly Statement body;

    public WhileStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      condition = reader.ReadExpression();
      body = reader.ReadStatement();
    }

    public WhileStatement(FileOffset fileOffset, Expression condition, Statement body)
    {
      this.fileOffset = fileOffset;
      this.condition = condition;
      this.body = body;
    }

    [Testing]
    public WhileStatement(Expression condition, Statement body)
    {
      this.condition = condition;
      this.body = body;
    }
  }

  public class DoStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 68;
    public readonly FileOffset fileOffset;
    public readonly Statement body;
    public readonly Expression condition;

    public DoStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      body = reader.ReadStatement();
      condition = reader.ReadExpression();
    }

    public DoStatement(FileOffset fileOffset, Statement body, Expression condition)
    {
      this.fileOffset = fileOffset;
      this.body = body;
      this.condition = condition;
    }

    [Testing]
    public DoStatement(Statement body, Expression condition)
    {
      this.body = body;
      this.condition = condition;
    }
  }

  public class ForStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 69;
    public readonly FileOffset fileOffset;
    public readonly List<VariableDeclaration> variables;
    public readonly Option<Expression> condition;
    public readonly List<Expression> updates;
    public readonly Statement body;

    public ForStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      variables = reader.ReadList(r => new VariableDeclaration(r));
      condition = reader.ReadOption(r => r.ReadExpression());
      updates = reader.ReadList(r => r.ReadExpression());
      body = reader.ReadStatement();
    }

    public ForStatement(FileOffset fileOffset, List<VariableDeclaration> variables, Option<Expression> condition, List<Expression> updates, Statement body)
    {
      this.fileOffset = fileOffset;
      this.variables = variables;
      this.condition = condition;
      this.updates = updates;
      this.body = body;
    }

    [Testing]
    public ForStatement(List<VariableDeclaration> variables, Option<Expression> condition, List<Expression> updates, Statement body)
    {
      this.variables = variables;
      this.condition = condition;
      this.updates = updates;
      this.body = body;
    }
  }

  public class ForInStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 70;
    public readonly FileOffset fileOffset;
    public readonly FileOffset bodyOffset;
    public readonly VariableDeclaration variable;
    public readonly Expression iterable;
    public readonly Statement body;

    public ForInStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      bodyOffset = new FileOffset(reader);
      variable = new VariableDeclaration(reader);
      iterable = reader.ReadExpression();
      body = reader.ReadStatement();
    }

    public ForInStatement(FileOffset fileOffset, FileOffset bodyOffset, VariableDeclaration variable, Expression iterable, Statement body)
    {
      this.fileOffset = fileOffset;
      this.bodyOffset = bodyOffset;
      this.variable = variable;
      this.iterable = iterable;
      this.body = body;
    }

    [Testing]
    public ForInStatement(FileOffset bodyOffset, VariableDeclaration variable, Expression iterable, Statement body)
    {
      this.bodyOffset = bodyOffset;
      this.variable = variable;
      this.iterable = iterable;
      this.body = body;
    }
  }

  public class AsyncForInStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 80; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly FileOffset bodyOffset;
    public readonly VariableDeclaration variable;
    public readonly Expression iterable;
    public readonly Statement body;

    public AsyncForInStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      bodyOffset = new FileOffset(reader);
      variable = new VariableDeclaration(reader);
      iterable = reader.ReadExpression();
      body = reader.ReadStatement();
    }

    public AsyncForInStatement(FileOffset fileOffset, FileOffset bodyOffset, VariableDeclaration variable, Expression iterable, Statement body)
    {
      this.fileOffset = fileOffset;
      this.bodyOffset = bodyOffset;
      this.variable = variable;
      this.iterable = iterable;
      this.body = body;
    }

    [Testing]
    public AsyncForInStatement(FileOffset bodyOffset, VariableDeclaration variable, Expression iterable, Statement body)
    {
      this.bodyOffset = bodyOffset;
      this.variable = variable;
      this.iterable = iterable;
      this.body = body;
    }
  }

  public class SwitchStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 71;
    public readonly FileOffset fileOffset;
    public readonly Expression expression;
    public readonly List<SwitchCase> cases;

    public SwitchStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      expression = reader.ReadExpression();
      cases = reader.ReadList(r => new SwitchCase(r));
    }

    public SwitchStatement(FileOffset fileOffset, Expression expression, List<SwitchCase> cases)
    {
      this.fileOffset = fileOffset;
      this.expression = expression;
      this.cases = cases;
    }

    [Testing]
    public SwitchStatement(Expression expression, List<SwitchCase> cases)
    {
      this.expression = expression;
      this.cases = cases;
    }
  }

  public class SwitchCase
  {
    // Note: there is no tag on SwitchCase
    public readonly List<Pair<FileOffset, Expression>> expressions;
    public readonly byte isDefault; // 1 if default, 0 is not default.
    public readonly Statement body;

    public SwitchCase(ComponentReader reader)
    {
      expressions = reader.ReadList(r => new Pair<FileOffset, Expression>(new FileOffset(r), r.ReadExpression()));
      isDefault = reader.ReadByte();
      body = reader.ReadStatement();
    }

    public SwitchCase(List<Pair<FileOffset, Expression>> expressions, Statement body, bool isDefault = false)
    {
      this.expressions = expressions;
      this.isDefault = isDefault ? (byte)1 : (byte)0;
      this.body = body;
    }

    [Testing]
    public SwitchCase(List<Expression> expressions, Statement body, bool isDefault = false)
    {
      this.expressions = expressions.Select(e => new Pair<FileOffset, Expression>(null, e)).ToList();
      this.isDefault = isDefault ? (byte)1 : (byte)0;
      this.body = body;
    }

  }

  public class ContinueSwitchStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 72;
    public readonly FileOffset fileOffset;

    // Reference to the Nth SwitchCase in scope.
    //
    // A SwitchCase is in scope everywhere within its enclosing switch,
    // except the scope is delimited by FunctionNodes.
    //
    // Switches are ordered from outermost to innermost, and the SwitchCases
    // within a switch are consecutively indexed from first to last, so index
    // 0 is the first SwitchCase of the outermost enclosing switch in the
    // same FunctionNode.
    public readonly uint caseIndex;

    public ContinueSwitchStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      caseIndex = reader.ReadUint();
    }

    public ContinueSwitchStatement(FileOffset fileOffset, uint caseIndex)
    {
      this.fileOffset = fileOffset;
      this.caseIndex = caseIndex;
    }

    [Testing]
    public ContinueSwitchStatement(uint caseIndex)
    {
      this.caseIndex = caseIndex;
    }
  }

  public class IfStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 73;
    public readonly FileOffset fileOffset;
    public readonly Expression condition;
    public readonly Statement then;
    public readonly Statement otherwise; // Empty statement if there was no else part.

    public IfStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      condition = reader.ReadExpression();
      then = reader.ReadStatement();
      otherwise = reader.ReadStatement();
    }

    public IfStatement(FileOffset fileOffset, Expression condition, Statement then, Statement otherwise)
    {
      this.fileOffset = fileOffset;
      this.condition = condition;
      this.then = then;
      this.otherwise = otherwise;
    }

    [Testing]
    public IfStatement(Expression condition, Statement then, Statement otherwise)
    {
      this.condition = condition;
      this.then = then;
      this.otherwise = otherwise;
    }
  }

  public class ReturnStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 74;
    public readonly FileOffset fileOffset;
    public readonly Option<Expression> expression;

    public ReturnStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      expression = reader.ReadOption(r => r.ReadExpression());
    }

    public ReturnStatement(FileOffset fileOffset, Option<Expression> expression)
    {
      this.fileOffset = fileOffset;
      this.expression = expression;
    }

    [Testing]
    public ReturnStatement(Expression expression)
    {
      this.expression = new Something<Expression>(expression);
    }

    [Testing]
    public ReturnStatement()
    {
      this.expression = new Nothing<Expression>();
    }
  }

  public class TryCatch : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 75;
    public readonly Statement body;

    // "any catch needs a stacktrace" means it has a stacktrace variable.
    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      anyCatchNeedsStackTrace = 0x1,
      isSynthesized = 0x2,
    }

    public readonly List<Catch> catches;

    public TryCatch(ComponentReader reader)
    {
      body = reader.ReadStatement();
      flags = (Flag) reader.ReadByte();
      catches = reader.ReadList(r => new Catch(r));
    }

    public TryCatch(Statement body, Flag flags, List<Catch> catches)
    {
      this.body = body;
      this.flags = flags;
      this.catches = catches;
    }
  }

  public class Catch
  {
    public readonly FileOffset fileOffset;
    public readonly DartType guard;
    public readonly Option<VariableDeclaration> exception;
    public readonly Option<VariableDeclaration> stackTrace;
    public readonly Statement body;

    public Catch(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      guard = reader.ReadDartType();
      exception = reader.ReadOption(r => new VariableDeclaration(r));
      stackTrace = reader.ReadOption(r => new VariableDeclaration(r));
      body = reader.ReadStatement();
    }

    public Catch(FileOffset fileOffset, DartType guard, Option<VariableDeclaration> exception, Option<VariableDeclaration> stackTrace, Statement body)
    {
      this.fileOffset = fileOffset;
      this.guard = guard;
      this.exception = exception;
      this.stackTrace = stackTrace;
      this.body = body;
    }

    [Testing]
    public Catch(DartType guard, Option<VariableDeclaration> exception, Option<VariableDeclaration> stackTrace, Statement body)
    {
      this.guard = guard;
      this.exception = exception;
      this.stackTrace = stackTrace;
      this.body = body;
    }
  }

  public class TryFinally : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 76;
    public readonly Statement body;
    public readonly Statement finalizer;

    public TryFinally(ComponentReader reader)
    {
      body = reader.ReadStatement();
      finalizer = reader.ReadStatement();
    }

    public TryFinally(Statement body, Statement finalizer)
    {
      this.body = body;
      this.finalizer = finalizer;
    }
  }

  public class YieldStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 77;
    public readonly FileOffset fileOffset;

    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      isYieldStar = 0x1
    }

    public readonly Expression expression;

    public YieldStatement(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      flags = (Flag) reader.ReadByte();
      expression = reader.ReadExpression();
    }

    public YieldStatement(FileOffset fileOffset, Flag flags, Expression expression)
    {
      this.fileOffset = fileOffset;
      this.flags = flags;
      this.expression = expression;
    }

    [Testing]
    public YieldStatement(Flag flags, Expression expression)
    {
      this.flags = flags;
      this.expression = expression;
    }
  }

  public class VariableDeclarationStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 78;
    public readonly VariableDeclaration variable;

    public VariableDeclarationStatement(ComponentReader reader)
    {
      variable = new VariableDeclaration(reader);
    }

    public VariableDeclarationStatement(VariableDeclaration variable)
    {
      this.variable = variable;
    }
  }

  public class FunctionDeclaration : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 79;

    public readonly FileOffset fileOffset;

    // The variable binding the function.  The variable is in scope
    // within the function for use as a self-reference.
    // Some of the fields in the variable are redundant, but its presence here
    // simplifies the rule for variable indexing.
    public readonly VariableDeclaration variable;
    public readonly FunctionNode function;

    public FunctionDeclaration(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      variable = new VariableDeclaration(reader);
      function = new FunctionNode(reader);
    }

    public FunctionDeclaration(FileOffset fileOffset, VariableDeclaration variable, FunctionNode function)
    {
      this.fileOffset = fileOffset;
      this.variable = variable;
      this.function = function;
    }

    [Testing]
    public FunctionDeclaration(VariableDeclaration variable, FunctionNode function)
    {
      this.variable = variable;
      this.function = function;
    }
  }
}