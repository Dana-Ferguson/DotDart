using System;
using System.Collections.Generic;

namespace DotDart
{

  public static class StatementExtensions
  {
    public static Statement ReadStatement(this DReader reader)
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

  public class ExpressionStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 61;
    public readonly Expression expression;

    public ExpressionStatement(DReader reader)
    {
      expression = reader.ReadExpression();
    }
  }

  public class Block : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 62;
    public readonly List<Statement> statements;

    public Block(DReader reader)
    {
      statements = reader.ReadList(r => r.ReadStatement());
    }
  }

  public class AssertBlock : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 81;
    public readonly List<Statement> statements;

    public AssertBlock(DReader reader)
    {
      statements = reader.ReadList(r => r.ReadStatement());
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

    public AssertStatement(DReader reader)
    {
      condition = reader.ReadExpression();
      conditionStartOffset = new FileOffset(reader);
      conditionEndOffset = new FileOffset(reader);
      message = reader.ReadOption(r => r.ReadExpression());
    }
  }

  public class LabeledStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 65;
    public readonly Statement body;

    public LabeledStatement(DReader reader)
    {
      body = reader.ReadStatement();
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

    public BreakStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      labelIndex = reader.ReadUint();
    }
  }

  public class WhileStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 67;
    public readonly FileOffset fileOffset;
    public readonly Expression condition;
    public readonly Statement body;

    public WhileStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      condition = reader.ReadExpression();
      body = reader.ReadStatement();
    }
  }

  public class DoStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 68;
    public readonly FileOffset fileOffset;
    public readonly Statement body;
    public readonly Expression condition;

    public DoStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      body = reader.ReadStatement();
      condition = reader.ReadExpression();
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

    public ForStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      variables = reader.ReadList(r => new VariableDeclaration(r));
      condition = reader.ReadOption(r => r.ReadExpression());
      updates = reader.ReadList(r => r.ReadExpression());
      body = reader.ReadStatement();
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

    public ForInStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      bodyOffset = new FileOffset(reader);
      variable = new VariableDeclaration(reader);
      iterable = reader.ReadExpression();
      body = reader.ReadStatement();
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

    public AsyncForInStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      bodyOffset = new FileOffset(reader);
      variable = new VariableDeclaration(reader);
      iterable = reader.ReadExpression();
      body = reader.ReadStatement();
    }
  }

  public class SwitchStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 71;
    public readonly FileOffset fileOffset;
    public readonly Expression expression;
    public readonly List<SwitchCase> cases;

    public SwitchStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      expression = reader.ReadExpression();
      cases = reader.ReadList(r => new SwitchCase(r));
    }
  }

  public class SwitchCase
  {
    // Note: there is no tag on SwitchCase
    public readonly List<Pair<FileOffset, Expression>> expressions;
    public readonly byte isDefault; // 1 if default, 0 is not default.
    public readonly Statement body;

    public SwitchCase(DReader reader)
    {
      expressions = reader.ReadList(r => new Pair<FileOffset, Expression>(new FileOffset(r), r.ReadExpression()));
      isDefault = reader.ReadByte();
      body = reader.ReadStatement();
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

    public ContinueSwitchStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      caseIndex = reader.ReadUint();
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

    public IfStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      condition = reader.ReadExpression();
      then = reader.ReadStatement();
      otherwise = reader.ReadStatement();
    }
  }

  public class ReturnStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 74;
    public readonly FileOffset fileOffset;
    public readonly Option<Expression> expression;

    public ReturnStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      expression = reader.ReadOption(r => r.ReadExpression());
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

    public TryCatch(DReader reader)
    {
      body = reader.ReadStatement();
      flags = (Flag) reader.ReadByte();
      catches = reader.ReadList(r => new Catch(r));
    }
  }

  public class Catch
  {
    public readonly FileOffset fileOffset;
    public readonly DartType guard;
    public readonly Option<VariableDeclaration> exception;
    public readonly Option<VariableDeclaration> stackTrace;
    public readonly Statement body;

    public Catch(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      guard = reader.ReadDartType();
      exception = reader.ReadOption(r => new VariableDeclaration(r));
      stackTrace = reader.ReadOption(r => new VariableDeclaration(r));
      body = reader.ReadStatement();
    }
  }

  public class TryFinally : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 76;
    public readonly Statement body;
    public readonly Statement finalizer;

    public TryFinally(DReader reader)
    {
      body = reader.ReadStatement();
      finalizer = reader.ReadStatement();
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

    public YieldStatement(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      flags = (Flag) reader.ReadByte();
      expression = reader.ReadExpression();
    }
  }

  public class VariableDeclarationStatement : Statement
  {
    public byte tag => Tag;
    public const byte Tag = 78;
    public readonly VariableDeclaration variable;

    public VariableDeclarationStatement(DReader reader)
    {
      variable = new VariableDeclaration(reader);
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

    public FunctionDeclaration(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      variable = new VariableDeclaration(reader);
      function = new FunctionNode(reader);
    }
  }
}