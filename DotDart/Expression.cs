using System;
using System.Collections.Generic;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DotDart
{
  public static class ExpressionExtensions
  {
    const int SpecializedTagHighBit = 0x80; // 10000000
    const int SpecializedTagMask = 0xF8; // 11111000
    const int SpecializedPayloadMask = 0x7; // 00000111

    // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L1377
    public static Expression ReadExpression(this ComponentReader reader)
    {
      var _tag = reader.ReadByte();
      var tag = ((_tag & SpecializedTagHighBit) == 0)
        ? _tag
        : _tag & SpecializedTagMask;

      switch (tag)
      {
        case AsExpression.Tag: return new AsExpression(reader);
        case AwaitExpression.Tag: return new AwaitExpression(reader);
        case BigIntLiteral.Tag: return new BigIntLiteral(reader);
        case CheckLibraryIsLoaded.Tag: return new CheckLibraryIsLoaded(reader);
        case ConditionalExpression.Tag: return new ConditionalExpression(reader);
        case ConstantExpression.Tag: return new ConstantExpression(reader);
        case ConstConstructorInvocation.Tag: return new ConstConstructorInvocation(reader);
        case ConstListLiteral.Tag: return new ConstListLiteral(reader);
        case ConstMapLiteral.Tag: return new ConstMapLiteral(reader);
        case ConstructorInvocation.Tag: return new ConstructorInvocation(reader);
        case ConstSetLiteral.Tag: return new ConstSetLiteral(reader);
        case ConstStaticInvocation.Tag: return new ConstStaticInvocation(reader);
        case DirectMethodInvocation.Tag: return new DirectMethodInvocation(reader);
        case DirectPropertyGet.Tag: return new DirectPropertyGet(reader);
        case DirectPropertySet.Tag: return new DirectPropertySet(reader);
        case DoubleLiteral.Tag: return new DoubleLiteral(reader);
        case FalseLiteral.Tag: return new FalseLiteral();
        case FunctionExpression.Tag: return new FunctionExpression(reader);
        case Instantiation.Tag: return new Instantiation(reader);
        case InvalidExpression.Tag: return new InvalidExpression(reader);
        case IsExpression.Tag: return new IsExpression(reader);
        case Let.Tag: return new Let(reader);
        case ListLiteral.Tag: return new ListLiteral(reader);
        case LoadLibrary.Tag: return new LoadLibrary(reader);
        case LogicalExpression.Tag: return new LogicalExpression(reader);
        case MapLiteral.Tag: return new MapLiteral(reader);
        case MethodInvocation.Tag: return new MethodInvocation(reader);
        case NegativeIntLiteral.Tag: return new NegativeIntLiteral(reader);
        case Not.Tag: return new Not(reader);
        case NullLiteral.Tag: return new NullLiteral();
        case PositiveIntLiteral.Tag: return new PositiveIntLiteral(reader);
        case PropertyGet.Tag: return new PropertyGet(reader);
        case PropertySet.Tag: return new PropertySet(reader);
        case Rethrow.Tag: return new Rethrow(reader);
        case SetLiteral.Tag: return new SetLiteral(reader);
        // public byte tag => (byte) (144 + N); // Where 0 <= N < 8.
        case 144:
        case 145:
        case 146:
        case 147:
        case 148:
        case 149:
        case 150:
        case 151:
          return SpecializedIntLiteral.FromTag((byte) tag);
        // public byte tag => (byte) (128 + N); // Where 0 <= N < 8.
        case 128:
        case 129:
        case 130:
        case 131:
        case 132:
        case 133:
        case 134:
        case 135:
          return new SpecializedVariableGet((byte) tag, reader);
        // public byte tag => (byte) (136 + N); // Where 0 <= N < 8.
        case 136:
        case 137:
        case 138:
        case 139:
        case 140:
        case 141:
        case 142:
        case 143:
          return new SpecializedVariableSet((byte) tag, reader);
        case StaticGet.Tag: return new StaticGet(reader);
        case StaticInvocation.Tag: return new StaticInvocation(reader);
        case StaticSet.Tag: return new StaticSet(reader);
        case StringConcatenation.Tag: return new StringConcatenation(reader);
        case StringLiteral.Tag: return new StringLiteral(reader);
        case SuperMethodInvocation.Tag: return new SuperMethodInvocation(reader);
        case SuperPropertyGet.Tag: return new SuperPropertyGet(reader);
        case SuperPropertySet.Tag: return new SuperPropertySet(reader);
        case SymbolLiteral.Tag: return new SymbolLiteral(reader);
        case ThisExpression.Tag: return new ThisExpression();
        case Throw.Tag: return new Throw(reader);
        case TrueLiteral.Tag: return new TrueLiteral();
        case TypeLiteral.Tag: return new TypeLiteral(reader);
        case VariableGet.Tag: return new VariableGet(reader);
        case VariableSet.Tag: return new VariableSet(reader);
        default: throw new Exception($"{nameof(Expression)}: unrecognized tag ({tag})");
      }
    }
  }

  public interface Expression : Node
  {
  }

  public class NamedExpression
  {
    // Note: there is no tag on NamedExpression.
    public readonly StringReference name;
    public readonly Expression value;

    public NamedExpression(ComponentReader reader)
    {
      name = new StringReference(reader);
      value = reader.ReadExpression();
    }

    public NamedExpression(StringReference name, Expression value)
    {
      this.name = name;
      this.value = value;
    }
  }

  public class SpecializedIntLiteral : Expression
  {
    private int N => value + 3;

    public byte tag => (byte) (144 + N); // Where 0 <= N < 8.

    // Integer literal with value (N - 3), that is, an integer in range -3..4.
    public readonly int value; // => N - 3;

    public static SpecializedIntLiteral FromTag(byte tag)
    {
      var n = tag - 144 - 3;
      return new SpecializedIntLiteral(n);
    }

    private SpecializedIntLiteral(int value)
    {
      // N = tag - 144;
      this.value = value;
    }

    // todo: emitting a Roslyn AST or... is text compiling faster?
    // e.g. how much speedup would I get out of Roslyn AST?
    // Would text compiling be much faster to build out.. or just a lot harder to get right?
    public SyntaxToken Compile()
    {
      return SF.Literal(value);
    }

    public void Serialize(DartStringBuilder sb)
    {
      sb.Append(nameof(SpecializedIntLiteral)).Append($": {value}");
    }
  }

  public class PositiveIntLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 55;
    public readonly uint value;

    public PositiveIntLiteral(ComponentReader reader)
    {
      value = reader.ReadUint();
    }

    public PositiveIntLiteral(uint value)
    {
      this.value = value;
    }

    public void Serialize(DartStringBuilder sb)
    {
      sb.Append(nameof(PositiveIntLiteral)).Append($": {value}");
    }
  }

  public class NegativeIntLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 56;
    private readonly uint absoluteValue;

    public int value => -(int) absoluteValue;

    public NegativeIntLiteral(ComponentReader reader)
    {
      absoluteValue = reader.ReadUint();
    }

    public NegativeIntLiteral(uint absoluteValue)
    {
      this.absoluteValue = absoluteValue;
    }

    public NegativeIntLiteral(int negativeValue)
    {
      if (negativeValue > 0) throw new Exception("Value must be negative");
      this.absoluteValue = (uint)(-negativeValue);
    }

    public void Serialize(DartStringBuilder sb)
    {
      sb.Append(nameof(NegativeIntLiteral)).Append($": {value}");
    }
  }

  public class BigIntLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 57;
    public readonly StringReference valueString;

    public BigIntLiteral(ComponentReader reader)
    {
      valueString = new StringReference(reader);
    }

    public BigIntLiteral(StringReference valueString)
    {
      this.valueString = valueString;
    }

    [Testing]
    public BigIntLiteral(string valueString)
    {
      this.valueString = new StringReference(valueString);
    }

    public void Serialize(DartStringBuilder sb)
    {
      sb.AppendHeader(nameof(BigIntLiteral)).Append($"{valueString}");
    }
  }

  public class DirectPropertySet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 16; // Note: tag is out of order
    public readonly FileOffset fileOffset;
    public readonly Expression receiver;
    public readonly MemberReference target;
    public readonly Expression value;

    public DirectPropertySet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      target = new MemberReference(reader);
      value = reader.ReadExpression();
    }

    public DirectPropertySet(FileOffset fileOffset, Expression receiver, MemberReference target, Expression value)
    {
      this.fileOffset = fileOffset;
      // todo: what is a receiver?
      this.receiver = receiver;
      this.target = target;
      this.value = value;
    }

    [Testing]
    public DirectPropertySet(Expression receiver, MemberReference target, Expression value)
    {
      this.receiver = receiver;
      this.target = target;
      this.value = value;
    }
  }

  public class DirectMethodInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 17; // Note: tag is out of order
    public readonly FileOffset fileOffset;
    public readonly Expression receiver;
    public readonly MemberReference target;
    public readonly Arguments arguments;

    public DirectMethodInvocation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      target = new MemberReference(reader);
      arguments = new Arguments(reader);
    }

    public DirectMethodInvocation(FileOffset fileOffset, Expression receiver, MemberReference target, Arguments arguments)
    {
      this.fileOffset = fileOffset;
      this.receiver = receiver;
      this.target = target;
      this.arguments = arguments;
    }

    [Testing]
    public DirectMethodInvocation(Expression receiver, MemberReference target, Arguments arguments)
    {
      this.receiver = receiver;
      this.target = target;
      this.arguments = arguments;
    }
  }

  public class InvalidExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 19;
    public readonly FileOffset fileOffset;
    public readonly StringReference message;

    public InvalidExpression(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      message = new StringReference(reader);
    }

    public InvalidExpression(FileOffset fileOffset, StringReference message)
    {
      this.fileOffset = fileOffset;
      this.message = message;
    }

    [Testing]
    public InvalidExpression(StringReference message)
    {
      this.message = message;
    }

    [Testing]
    public InvalidExpression(string message)
    {
      this.message = new StringReference(message);
    }

  }

  public class VariableGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 20;

    public readonly FileOffset fileOffset;

    // Byte offset in the binary for the variable declaration (without tag).
    public readonly uint variableDeclarationPosition;
    public readonly VariableReference variable;
    public readonly Option<DartType> promotedType;

    public VariableGet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
      variable = new VariableReference(reader);
      promotedType = reader.ReadOption(r => r.ReadDartType());
    }

    public VariableGet(FileOffset fileOffset, uint variableDeclarationPosition, VariableReference variable, Option<DartType> promotedType)
    {
      this.fileOffset = fileOffset;
      this.variableDeclarationPosition = variableDeclarationPosition;
      this.variable = variable;
      this.promotedType = promotedType;
    }

    [Testing]
    public VariableGet(uint variableDeclarationPosition, VariableReference variable, Option<DartType> promotedType)
    {
      this.variableDeclarationPosition = variableDeclarationPosition;
      this.variable = variable;
      this.promotedType = promotedType;
    }
  }

  // todo: I think N is an index? like list[N].. but I am unsure?
  public class SpecializedVariableGet : Expression
  {
    private readonly int N;
    public byte tag => (byte) (128 + N); // Where 0 <= N < 8.

    // Equivalent to a VariableGet with index N.
    public readonly FileOffset fileOffset;

    // Byte offset in the binary for the variable declaration (without tag).
    public readonly uint variableDeclarationPosition;

    public SpecializedVariableGet(byte tag, ComponentReader reader)
    {
      N = tag - 128;
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
    }

    // todo: what is this?
    public SpecializedVariableGet(FileOffset fileOffset, int n, uint variableDeclarationPosition)
    {
      N = n;
      this.fileOffset = fileOffset;
      this.variableDeclarationPosition = variableDeclarationPosition;
    }

    [Testing]
    public SpecializedVariableGet(int n, uint variableDeclarationPosition)
    {
      N = n;
      this.variableDeclarationPosition = variableDeclarationPosition;
    }
  }

  public class VariableSet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 21;

    public readonly FileOffset fileOffset;

    // Byte offset in the binary for the variable declaration (without tag).
    public readonly uint variableDeclarationPosition;
    public readonly VariableReference variable;
    public readonly Expression value;

    public VariableSet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
      variable = new VariableReference(reader);
      value = reader.ReadExpression();
    }

    public VariableSet(FileOffset fileOffset, uint variableDeclarationPosition, VariableReference variable, Expression value)
    {
      this.fileOffset = fileOffset;
      this.variableDeclarationPosition = variableDeclarationPosition;
      this.variable = variable;
      this.value = value;
    }

    [Testing]
    public VariableSet(uint variableDeclarationPosition, VariableReference variable, Expression value)
    {
      this.variableDeclarationPosition = variableDeclarationPosition;
      this.variable = variable;
      this.value = value;
    }
  }

  // todo: I think N is an index? like list[N].. but I am unsure?
  public class SpecializedVariableSet : Expression
  {
    public readonly int N;
    public byte tag => (byte) (136 + N); // Where 0 <= N < 8.

    public readonly FileOffset fileOffset;

    // Byte offset in the binary for the variable declaration (without tag).
    public readonly uint variableDeclarationPosition;

    public readonly Expression value;
    // Equivalent to VariableSet with index N.

    public SpecializedVariableSet(byte tag, ComponentReader reader)
    {
      N = tag - 136;
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
      value = reader.ReadExpression();
    }

    public SpecializedVariableSet(FileOffset fileOffset, int n, uint variableDeclarationPosition, Expression value)
    {
      N = n;
      this.fileOffset = fileOffset;
      this.variableDeclarationPosition = variableDeclarationPosition;
      this.value = value;
    }

    [Testing]
    public SpecializedVariableSet(int n, uint variableDeclarationPosition, Expression value)
    {
      N = n;
      this.variableDeclarationPosition = variableDeclarationPosition;
      this.value = value;
    }
  }

  public class PropertyGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 22;
    public readonly FileOffset fileOffset;
    public readonly Expression receiver;
    public readonly Name name;
    public readonly MemberReference interfaceTarget; // May be NullReference.

    public PropertyGet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      name = new Name(reader);
      interfaceTarget = new MemberReference(reader);
    }

    public PropertyGet(FileOffset fileOffset, Expression receiver, Name name, MemberReference interfaceTarget)
    {
      this.fileOffset = fileOffset;
      this.receiver = receiver;
      this.name = name;
      this.interfaceTarget = interfaceTarget;
    }

    [Testing]
    public PropertyGet(Expression receiver, Name name, MemberReference interfaceTarget)
    {
      this.receiver = receiver;
      this.name = name;
      this.interfaceTarget = interfaceTarget;
    }
  }

  public class PropertySet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 23;
    public readonly FileOffset fileOffset;
    public readonly Expression receiver;
    public readonly Name name;
    public readonly Expression value;
    public readonly MemberReference interfaceTarget; // May be NullReference.

    public PropertySet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      name = new Name(reader);
      value = reader.ReadExpression();
      interfaceTarget = new MemberReference(reader);
    }

    public PropertySet(FileOffset fileOffset, Expression receiver, Name name, Expression value, MemberReference interfaceTarget)
    {
      this.fileOffset = fileOffset;
      this.receiver = receiver;
      this.name = name;
      this.value = value;
      this.interfaceTarget = interfaceTarget;
    }

    [Testing]
    public PropertySet(Expression receiver, Name name, Expression value, MemberReference interfaceTarget)
    {
      this.receiver = receiver;
      this.name = name;
      this.value = value;
      this.interfaceTarget = interfaceTarget;
    }
  }

  public class SuperPropertyGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 24;
    public readonly FileOffset fileOffset;
    public readonly Name name;
    public readonly MemberReference interfaceTarget; // May be NullReference.

    public SuperPropertyGet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      name = new Name(reader);
      interfaceTarget = new MemberReference(reader);
    }

    public SuperPropertyGet(FileOffset fileOffset, Name name, MemberReference interfaceTarget)
    {
      this.fileOffset = fileOffset;
      this.name = name;
      this.interfaceTarget = interfaceTarget;
    }

    [Testing]
    public SuperPropertyGet(Name name, MemberReference interfaceTarget)
    {
      this.name = name;
      this.interfaceTarget = interfaceTarget;
    }
  }

  public class SuperPropertySet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 25;
    public readonly FileOffset fileOffset;
    public readonly Name name;
    public readonly Expression value;
    public readonly MemberReference interfaceTarget; // May be NullReference.

    public SuperPropertySet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      name = new Name(reader);
      value = reader.ReadExpression();
      interfaceTarget = new MemberReference(reader);
    }

    public SuperPropertySet(FileOffset fileOffset, Name name, Expression value, MemberReference interfaceTarget)
    {
      this.fileOffset = fileOffset;
      this.name = name;
      this.value = value;
      this.interfaceTarget = interfaceTarget;
    }

    [Testing]
    public SuperPropertySet(Name name, Expression value, MemberReference interfaceTarget)
    {
      this.name = name;
      this.value = value;
      this.interfaceTarget = interfaceTarget;
    }
  }

  public class DirectPropertyGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 15; // Note: tag is out of order
    public readonly FileOffset fileOffset;
    public readonly Expression receiver;
    public readonly MemberReference target;

    public DirectPropertyGet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      target = new MemberReference(reader);
    }

    public DirectPropertyGet(FileOffset fileOffset, Expression receiver, MemberReference target)
    {
      this.fileOffset = fileOffset;
      this.receiver = receiver;
      this.target = target;
    }

    [Testing]
    public DirectPropertyGet(Expression receiver, MemberReference target)
    {
      this.receiver = receiver;
      this.target = target;
    }
  }

  public class StaticGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 26;
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;

    public StaticGet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
    }

    // this needs to work with StringConcatenation
    public IdentifierNameSyntax Compile()
    {
      return SF.IdentifierName(target.canonicalName.value);
    }

    public StaticGet(FileOffset fileOffset, MemberReference target)
    {
      this.fileOffset = fileOffset;
      this.target = target;
    }

    [Testing]
    public StaticGet(string target)
    {
      fileOffset = null;
      this.target = new MemberReference(target);
    }
  }

  public class StaticSet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 27;
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;
    public readonly Expression value;

    public StaticSet(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
      value = reader.ReadExpression();
    }

    public StaticSet(FileOffset fileOffset, MemberReference target, Expression value)
    {
      this.fileOffset = fileOffset;
      this.target = target;
      this.value = value;
    }

    [Testing]
    public StaticSet(MemberReference target, Expression value)
    {
      this.target = target;
      this.value = value;
    }
  }

  public class MethodInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 28;
    public readonly FileOffset fileOffset;
    public readonly Expression receiver;
    public readonly Name name;
    public readonly Arguments arguments;
    public readonly MemberReference interfaceTarget; // May be NullReference.

    public MethodInvocation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      name = new Name(reader);
      arguments = new Arguments(reader);
      interfaceTarget = new MemberReference(reader);
    }

    public MethodInvocation(FileOffset fileOffset, Expression receiver, Name name, Arguments arguments, MemberReference interfaceTarget)
    {
      this.fileOffset = fileOffset;
      this.receiver = receiver;
      this.name = name;
      this.arguments = arguments;
      this.interfaceTarget = interfaceTarget;
    }

    [Testing]
    public MethodInvocation(Expression receiver, Name name, Arguments arguments, MemberReference interfaceTarget)
    {
      this.receiver = receiver;
      this.name = name;
      this.arguments = arguments;
      this.interfaceTarget = interfaceTarget;
    }
  }

  public class SuperMethodInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 29;
    public readonly FileOffset fileOffset;
    public readonly Name name;
    public readonly Arguments arguments;
    public readonly MemberReference interfaceTarget; // May be NullReference.

    public SuperMethodInvocation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      name = new Name(reader);
      arguments = new Arguments(reader);
      interfaceTarget = new MemberReference(reader);
    }

    public SuperMethodInvocation(FileOffset fileOffset, Name name, Arguments arguments, MemberReference interfaceTarget)
    {
      this.fileOffset = fileOffset;
      this.name = name;
      this.arguments = arguments;
      this.interfaceTarget = interfaceTarget;
    }

    [Testing]
    public SuperMethodInvocation(Name name, Arguments arguments, MemberReference interfaceTarget)
    {
      this.name = name;
      this.arguments = arguments;
      this.interfaceTarget = interfaceTarget;
    }
  }

  public class StaticInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 30;
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;
    public readonly Arguments arguments;

    public StaticInvocation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
      arguments = new Arguments(reader);
    }

    public StaticInvocation(FileOffset fileOffset, MemberReference target, Arguments arguments)
    {
      this.fileOffset = fileOffset;
      this.target = target;
      this.arguments = arguments;
    }

    [Testing]
    public StaticInvocation(MemberReference target, Arguments arguments)
    {
      this.target = target;
      this.arguments = arguments;
    }

    object Compile()
    {
      throw new NotImplementedException();
      // SeparatedSyntaxList<ArgumentSyntax>
      dynamic arguments = null; // this.arguments;

      return SF.InvocationExpression(
        SF.MemberAccessExpression(
          SyntaxKind.SimpleMemberAccessExpression,
          SF.IdentifierName("dart_core"), // todo: I think canonicalName has a library value?
          SF.IdentifierName(target.canonicalName.value)))
        .WithArgumentList(SF.ArgumentList(arguments));
    }
  }

// Constant call to an external constant factory.
  public class ConstStaticInvocation : Expression, IExpressionSyntax
  {
    public byte tag => Tag;
    public const byte Tag = 18; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;
    public readonly Arguments arguments;

    public ConstStaticInvocation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
      arguments = new Arguments(reader);
    }

    public ConstStaticInvocation(FileOffset fileOffset, MemberReference target, Arguments arguments)
    {
      this.fileOffset = fileOffset;
      this.target = target;
      this.arguments = arguments;
    }

    [Testing]
    public ConstStaticInvocation(MemberReference target, Arguments arguments)
    {
      this.target = target;
      this.arguments = arguments;
    }

    public ExpressionSyntax ToExpressionSyntax()
    {
      var invocationExpression = SF.InvocationExpression(SF.IdentifierName(target.canonicalName.value));
      if (arguments.numArguments != 0)
      {
        invocationExpression = invocationExpression.WithArgumentList(arguments.ToArgumentListSyntax());
      }

      return invocationExpression;
    }
  }

  public class ConstructorInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 31;
    public readonly FileOffset fileOffset;
    public readonly ConstructorReference target;
    public readonly Arguments arguments;

    public ConstructorInvocation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new ConstructorReference(reader);
      arguments = new Arguments(reader);
    }

    public ConstructorInvocation(FileOffset fileOffset, ConstructorReference target, Arguments arguments)
    {
      this.fileOffset = fileOffset;
      this.target = target;
      this.arguments = arguments;
    }

    [Testing]
    public ConstructorInvocation(ConstructorReference target, Arguments arguments)
    {
      this.target = target;
      this.arguments = arguments;
    }
  }

  public class ConstConstructorInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 32;
    public readonly FileOffset fileOffset;
    public readonly ConstructorReference target;
    public readonly Arguments arguments;

    // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L1487
    // will parse the same ~ but the binary.md and implementations disagree on semantics here
    public ConstConstructorInvocation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new ConstructorReference(reader);
      arguments = new Arguments(reader);
    }

    public ConstConstructorInvocation(FileOffset fileOffset, ConstructorReference target, Arguments arguments)
    {
      this.fileOffset = fileOffset;
      this.target = target;
      this.arguments = arguments;
    }

    [Testing]
    public ConstConstructorInvocation(ConstructorReference target, Arguments arguments)
    {
      this.target = target;
      this.arguments = arguments;
    }
  }

  public class Not : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 33;
    public readonly Expression operand;

    public Not(ComponentReader reader)
    {
      operand = reader.ReadExpression();
    }

    public Not(Expression operand)
    {
      this.operand = operand;
    }
  }

/*
 enum LogicalOperator { &&, || }
*/

  public class LogicalExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 34;
    public readonly Expression left;
    public readonly byte logicalOperator; // Index into LogicalOperator enum above
    public readonly Expression right;

    public LogicalExpression(ComponentReader reader)
    {
      left = reader.ReadExpression();
      logicalOperator = reader.ReadByte();
      right = reader.ReadExpression();
    }

    public LogicalExpression(Expression left, byte logicalOperator, Expression right)
    {
      this.left = left;
      this.logicalOperator = logicalOperator;
      this.right = right;
    }
  }

  public class ConditionalExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 35;
    public readonly Expression condition;
    public readonly Expression then;
    public readonly Expression otherwise;
    public readonly Option<DartType> staticType;

    public ConditionalExpression(ComponentReader reader)
    {
      condition = reader.ReadExpression();
      then = reader.ReadExpression();
      otherwise = reader.ReadExpression();
      staticType = reader.ReadOption(r => r.ReadDartType());
    }

    public ConditionalExpression(Expression condition, Expression then, Expression otherwise, Option<DartType> staticType)
    {
      this.condition = condition;
      this.then = then;
      this.otherwise = otherwise;
      this.staticType = staticType;
    }
  }

  public class StringConcatenation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 36;
    public readonly FileOffset fileOffset;
    public readonly IReadOnlyList<Expression> expressions;

    public StringConcatenation(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      expressions = reader.ReadList(r => r.ReadExpression());
    }

    public StringConcatenation(FileOffset fileOffset, IEnumerable<Expression> expressions)
    {
      this.fileOffset = fileOffset;
      this.expressions = expressions.ToList();
    }

    [Testing]
    public StringConcatenation(IEnumerable<Expression> expressions)
    {
      this.expressions = expressions.ToList();
    }

    public InterpolatedStringExpressionSyntax Compile()
    {
      var terms = new List<InterpolatedStringContentSyntax>();
      foreach (var expression in expressions)
      {
        // literals --> SF.InterpolatedStringText()
        // variables --> SF.Interpolation()
        switch (expression)
        {
          case StringLiteral literal:
            terms.Add(SF.InterpolatedStringText()
              .WithTextToken(
                SF.Token(
                  SF.TriviaList(),
                  SyntaxKind.InterpolatedStringTextToken,
                  literal.value.value,
                  literal.value.value,
                  SF.TriviaList())));
            break;
          case StaticGet staticGet:
            terms.Add(SF.Interpolation(
              staticGet.Compile()));
            break;
          default:
            throw new Exception($"{nameof(StringConcatenation)} case not handled. Type is {expression.GetType()}.");
        }
      }

      return SF.InterpolatedStringExpression(
              SF.Token(SyntaxKind.InterpolatedStringStartToken))
            .WithContents(
              SF.List<InterpolatedStringContentSyntax>(terms)
            );
    }
  }

  public class IsExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 37;
    public readonly FileOffset fileOffset;
    public readonly Expression operand;
    public readonly DartType type;

    public IsExpression(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      operand = reader.ReadExpression();
      type = reader.ReadDartType();
    }

    public IsExpression(FileOffset fileOffset, Expression operand, DartType type)
    {
      this.fileOffset = fileOffset;
      this.operand = operand;
      this.type = type;
    }

    [Testing]
    public IsExpression(Expression operand, DartType type)
    {
      this.operand = operand;
      this.type = type;
    }
  }

  public class AsExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 38;
    public readonly FileOffset fileOffset;

    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      isTypeError = 0x1
    }

    public readonly Expression operand;
    public readonly DartType type;

    public AsExpression(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      flags = (Flag) reader.ReadByte();
      operand = reader.ReadExpression();
      type = reader.ReadDartType();
    }

    public AsExpression(FileOffset fileOffset, Flag flags, Expression operand, DartType type)
    {
      this.fileOffset = fileOffset;
      this.flags = flags;
      this.operand = operand;
      this.type = type;
    }

    [Testing]
    public AsExpression(Flag flags, Expression operand, DartType type)
    {
      this.flags = flags;
      this.operand = operand;
      this.type = type;
    }
  }

  public class StringLiteral : Expression, ISyntaxToken, ILiteralExpressionSyntax
  {
    public byte tag => Tag;
    public const byte Tag = 39;
    public readonly StringReference value;

    public StringLiteral(ComponentReader reader)
    {
      value = new StringReference(reader);
    }

    public StringLiteral(string value)
    {
      this.value = new StringReference(value);
    }

    // todo: does this get used? -- should this be rolled into ToLiteralExpressionSyntax
    public SyntaxToken ToSyntaxToken()
    {
      return SF.Literal(value.value);
    }

    public LiteralExpressionSyntax ToLiteralExpressionSyntax()
    {
      return SF.LiteralExpression(
        SyntaxKind.StringLiteralExpression,
        ToSyntaxToken());
    }
  }

  public class DoubleLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 40;
    double value;

    public DoubleLiteral(ComponentReader reader)
    {
      value = reader.ReadDouble();
    }

    public DoubleLiteral(double value)
    {
      this.value = value;
    }
  }

  public class TrueLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 41;
  }

  public class FalseLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 42;
  }

  public class NullLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 43;
  }

  public class SymbolLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 44;
    public readonly StringReference value; // Everything strictly after the '#'.

    public SymbolLiteral(ComponentReader reader)
    {
      value = new StringReference(reader);
    }

    public SymbolLiteral(StringReference value)
    {
      this.value = value;
    }

    [Testing]
    public SymbolLiteral(string value)
    {
      this.value = new StringReference(value);
    }
  }

  public class TypeLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 45;
    public readonly DartType type;

    public TypeLiteral(ComponentReader reader)
    {
      type = reader.ReadDartType();
    }

    public TypeLiteral(DartType type)
    {
      this.type = type;
    }
  }

  public class ThisExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 46;
  }

  public class Rethrow : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 47;
    public readonly FileOffset fileOffset;

    public Rethrow(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
    }

    public Rethrow(FileOffset fileOffset)
    {
      this.fileOffset = fileOffset;
    }

    [Testing]
    public Rethrow()
    {
    }
  }

  public class Throw : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 48;
    public readonly FileOffset fileOffset;
    public readonly Expression value;

    public Throw(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      value = reader.ReadExpression();
    }

    public Throw(FileOffset fileOffset, Expression value)
    {
      this.fileOffset = fileOffset;
      this.value = value;
    }

    [Testing]
    public Throw(Expression value)
    {
      this.value = value;
    }
  }

  public class ListLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 49;
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public ListLiteral(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
    }

    public ListLiteral(FileOffset fileOffset, DartType typeArgument, List<Expression> values)
    {
      this.fileOffset = fileOffset;
      this.typeArgument = typeArgument;
      this.values = values;
    }

    [Testing]
    public ListLiteral(DartType typeArgument, List<Expression> values)
    {
      this.typeArgument = typeArgument;
      this.values = values;
    }
  }

  public class ConstListLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 58; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public ConstListLiteral(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
    }

    public ConstListLiteral(FileOffset fileOffset, DartType typeArgument, List<Expression> values)
    {
      this.fileOffset = fileOffset;
      this.typeArgument = typeArgument;
      this.values = values;
    }

    [Testing]
    public ConstListLiteral(DartType typeArgument, List<Expression> values)
    {
      this.typeArgument = typeArgument;
      this.values = values;
    }
  }

  public class SetLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 109; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public SetLiteral(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
    }

    public SetLiteral(FileOffset fileOffset, DartType typeArgument, List<Expression> values)
    {
      this.fileOffset = fileOffset;
      this.typeArgument = typeArgument;
      this.values = values;
    }

    [Testing]
    public SetLiteral(DartType typeArgument, List<Expression> values)
    {
      this.typeArgument = typeArgument;
      this.values = values;
    }
  }

  public class ConstSetLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 110; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public ConstSetLiteral(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
    }

    public ConstSetLiteral(FileOffset fileOffset, DartType typeArgument, List<Expression> values)
    {
      this.fileOffset = fileOffset;
      this.typeArgument = typeArgument;
      this.values = values;
    }

    [Testing]
    public ConstSetLiteral(DartType typeArgument, List<Expression> values)
    {
      this.typeArgument = typeArgument;
      this.values = values;
    }
  }

  public class MapLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 50;
    public readonly FileOffset fileOffset;
    public readonly DartType keyType;
    public readonly DartType valueType;
    public readonly List<MapEntry> entries;

    public MapLiteral(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      keyType = reader.ReadDartType();
      valueType = reader.ReadDartType();
      entries = reader.ReadList(r => new MapEntry(r));
    }

    public MapLiteral(FileOffset fileOffset, DartType keyType, DartType valueType, List<MapEntry> entries)
    {
      this.fileOffset = fileOffset;
      this.keyType = keyType;
      this.valueType = valueType;
      this.entries = entries;
    }

    [Testing]
    public MapLiteral(DartType keyType, DartType valueType, List<MapEntry> entries)
    {
      this.keyType = keyType;
      this.valueType = valueType;
      this.entries = entries;
    }
  }

  public class ConstMapLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 59; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly DartType keyType;
    public readonly DartType valueType;
    public readonly List<MapEntry> entries;

    public ConstMapLiteral(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      keyType = reader.ReadDartType();
      valueType = reader.ReadDartType();
      entries = reader.ReadList(r => new MapEntry(r));
    }

    public ConstMapLiteral(FileOffset fileOffset, DartType keyType, DartType valueType, List<MapEntry> entries)
    {
      this.fileOffset = fileOffset;
      this.keyType = keyType;
      this.valueType = valueType;
      this.entries = entries;
    }

    [Testing]
    public ConstMapLiteral(DartType keyType, DartType valueType, List<MapEntry> entries)
    {
      this.keyType = keyType;
      this.valueType = valueType;
      this.entries = entries;
    }
  }

  public class AwaitExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 51;
    public readonly Expression operand;

    public AwaitExpression(ComponentReader reader)
    {
      operand = reader.ReadExpression();
    }

    public AwaitExpression(Expression operand)
    {
      this.operand = operand;
    }
  }

  public class FunctionExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 52;
    public readonly FileOffset fileOffset;
    public readonly FunctionNode function;

    public FunctionExpression(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      function = new FunctionNode(reader);
    }

    public FunctionExpression(FileOffset fileOffset, FunctionNode function)
    {
      this.fileOffset = fileOffset;
      this.function = function;
    }

    [Testing]
    public FunctionExpression(FunctionNode function)
    {
      this.function = function;
    }
  }

  public class Let : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 53;
    public readonly VariableDeclaration variable;
    public readonly Expression body;

    public Let(ComponentReader reader)
    {
      variable = new VariableDeclaration(reader);
      body = reader.ReadExpression();
    }

    public Let(VariableDeclaration variable, Expression body)
    {
      this.variable = variable;
      this.body = body;
    }
  }

  public class Instantiation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 54;
    public readonly Expression expression;
    public readonly List<DartType> typeArguments;

    public Instantiation(ComponentReader reader)
    {
      expression = reader.ReadExpression();
      typeArguments = reader.ReadList(r => r.ReadDartType());
    }

    public Instantiation(Expression expression, List<DartType> typeArguments)
    {
      this.expression = expression;
      this.typeArguments = typeArguments;
    }
  }

  public class LoadLibrary : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 14;
    public readonly LibraryDependencyReference deferredImport;

    public LoadLibrary(ComponentReader reader)
    {
      deferredImport = new LibraryDependencyReference(reader);
    }

    public LoadLibrary(LibraryDependencyReference deferredImport)
    {
      this.deferredImport = deferredImport;
    }
  }

  public class CheckLibraryIsLoaded : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 13;
    public readonly LibraryDependencyReference deferredImport;

    public CheckLibraryIsLoaded(ComponentReader reader)
    {
      deferredImport = new LibraryDependencyReference(reader);
    }

    public CheckLibraryIsLoaded(LibraryDependencyReference deferredImport)
    {
      this.deferredImport = deferredImport;
    }
  }

  public class ConstantExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 107;
    public readonly CanonicalNameReference constantReference;

    public ConstantExpression(ComponentReader reader)
    {
      constantReference = new CanonicalNameReference(reader);
    }

    public ConstantExpression(CanonicalNameReference constantReference)
    {
      this.constantReference = constantReference;
    }
  }
}