using System;
using System.Collections.Generic;

namespace DotDart
{
  public static class ExpressionExtensions
  {
    const int SpecializedTagHighBit = 0x80; // 10000000
    const int SpecializedTagMask = 0xF8; // 11111000
    const int SpecializedPayloadMask = 0x7; // 00000111

    // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L1377
    public static Expression ReadExpression(this DReader reader)
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
          return new SpecializedIntLiteral((byte) tag);
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

  public class SpecializedIntLiteral : Expression
  {
    private readonly int N;

    public byte tag => (byte) (144 + N); // Where 0 <= N < 8.

    // Integer literal with value (N - 3), that is, an integer in range -3..4.
    public int value => N - 3;

    public SpecializedIntLiteral(byte tag)
    {
      N = tag - 144;
    }
  }

  public class PositiveIntLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 55;
    public readonly uint value;

    public PositiveIntLiteral(DReader reader)
    {
      value = reader.ReadUint();
    }
  }

  public class NegativeIntLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 56;
    private readonly uint absoluteValue;

    public int value => -(int) absoluteValue;

    public NegativeIntLiteral(DReader reader)
    {
      absoluteValue = reader.ReadUint();
    }
  }

  public class BigIntLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 57;
    public readonly StringReference valueString;

    public BigIntLiteral(DReader reader)
    {
      valueString = new StringReference(reader);
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

    public DirectPropertySet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      target = new MemberReference(reader);
      value = reader.ReadExpression();
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

    public DirectMethodInvocation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      target = new MemberReference(reader);
      arguments = new Arguments(reader);
    }
  }

  public class InvalidExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 19;
    public readonly FileOffset fileOffset;
    public readonly StringReference message;

    public InvalidExpression(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      message = new StringReference(reader);
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

    public VariableGet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
      variable = new VariableReference(reader);
      promotedType = reader.ReadOption(r => r.ReadDartType());
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

    public SpecializedVariableGet(byte tag, DReader reader)
    {
      N = tag - 128;
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
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

    public VariableSet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
      variable = new VariableReference(reader);
      value = reader.ReadExpression();
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

    public SpecializedVariableSet(byte tag, DReader reader)
    {
      N = tag - 136;
      fileOffset = new FileOffset(reader);
      variableDeclarationPosition = reader.ReadUint();
      value = reader.ReadExpression();
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

    public PropertyGet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      name = new Name(reader);
      interfaceTarget = new MemberReference(reader);
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

    public PropertySet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      name = new Name(reader);
      value = reader.ReadExpression();
      interfaceTarget = new MemberReference(reader);
    }
  }

  public class SuperPropertyGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 24;
    public readonly FileOffset fileOffset;
    public readonly Name name;
    public readonly MemberReference interfaceTarget; // May be NullReference.

    public SuperPropertyGet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      name = new Name(reader);
      interfaceTarget = new MemberReference(reader);
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

    public SuperPropertySet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      name = new Name(reader);
      value = reader.ReadExpression();
      interfaceTarget = new MemberReference(reader);
    }
  }

  public class DirectPropertyGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 15; // Note: tag is out of order
    public readonly FileOffset fileOffset;
    public readonly Expression receiver;
    public readonly MemberReference target;

    public DirectPropertyGet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      target = new MemberReference(reader);
    }
  }

  public class StaticGet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 26;
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;

    public StaticGet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
    }
  }

  public class StaticSet : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 27;
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;
    public readonly Expression value;

    public StaticSet(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
      value = reader.ReadExpression();
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

    public MethodInvocation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      receiver = reader.ReadExpression();
      name = new Name(reader);
      arguments = new Arguments(reader);
      interfaceTarget = new MemberReference(reader);
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

    public SuperMethodInvocation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      name = new Name(reader);
      arguments = new Arguments(reader);
      interfaceTarget = new MemberReference(reader);
    }
  }

  public class StaticInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 30;
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;
    public readonly Arguments arguments;

    public StaticInvocation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
      arguments = new Arguments(reader);
    }
  }

// Constant call to an external constant factory.
  public class ConstStaticInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 18; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly MemberReference target;
    public readonly Arguments arguments;

    public ConstStaticInvocation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new MemberReference(reader);
      arguments = new Arguments(reader);
    }
  }

  public class ConstructorInvocation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 31;
    public readonly FileOffset fileOffset;
    public readonly ConstructorReference target;
    public readonly Arguments arguments;

    public ConstructorInvocation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new ConstructorReference(reader);
      arguments = new Arguments(reader);
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
    public ConstConstructorInvocation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      target = new ConstructorReference(reader);
      arguments = new Arguments(reader);
    }
  }

  public class Not : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 33;
    public readonly Expression operand;

    public Not(DReader reader)
    {
      operand = reader.ReadExpression();
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

    public LogicalExpression(DReader reader)
    {
      left = reader.ReadExpression();
      logicalOperator = reader.ReadByte();
      right = reader.ReadExpression();
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

    public ConditionalExpression(DReader reader)
    {
      condition = reader.ReadExpression();
      then = reader.ReadExpression();
      otherwise = reader.ReadExpression();
      staticType = reader.ReadOption(r => r.ReadDartType());
    }
  }

  public class StringConcatenation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 36;
    public readonly FileOffset fileOffset;
    public readonly List<Expression> expressions;

    public StringConcatenation(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      expressions = reader.ReadList(r => r.ReadExpression());
    }
  }

  public class IsExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 37;
    public readonly FileOffset fileOffset;
    public readonly Expression operand;
    public readonly DartType type;

    public IsExpression(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      operand = reader.ReadExpression();
      type = reader.ReadDartType();
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

    public AsExpression(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      flags = (Flag) reader.ReadByte();
      operand = reader.ReadExpression();
      type = reader.ReadDartType();
    }
  }

  public class StringLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 39;
    public readonly StringReference value;

    public StringLiteral(DReader reader)
    {
      value = new StringReference(reader);
    }
  }

  public class DoubleLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 40;
    double value;

    public DoubleLiteral(DReader reader)
    {
      value = reader.ReadDouble();
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

    public SymbolLiteral(DReader reader)
    {
      value = new StringReference(reader);
    }
  }

  public class TypeLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 45;
    public readonly DartType type;

    public TypeLiteral(DReader reader)
    {
      type = reader.ReadDartType();
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

    public Rethrow(DReader reader)
    {
      fileOffset = new FileOffset(reader);
    }
  }

  public class Throw : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 48;
    public readonly FileOffset fileOffset;
    public readonly Expression value;

    public Throw(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      value = reader.ReadExpression();
    }
  }

  public class ListLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 49;
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public ListLiteral(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
    }
  }

  public class ConstListLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 58; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public ConstListLiteral(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
    }
  }

  public class SetLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 109; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public SetLiteral(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
    }
  }

  public class ConstSetLiteral : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 110; // Note: tag is out of order.
    public readonly FileOffset fileOffset;
    public readonly DartType typeArgument;
    public readonly List<Expression> values;

    public ConstSetLiteral(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      typeArgument = reader.ReadDartType();
      values = reader.ReadList(r => r.ReadExpression());
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

    public MapLiteral(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      keyType = reader.ReadDartType();
      valueType = reader.ReadDartType();
      entries = reader.ReadList(r => new MapEntry(r));
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

    public ConstMapLiteral(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      keyType = reader.ReadDartType();
      valueType = reader.ReadDartType();
      entries = reader.ReadList(r => new MapEntry(r));
    }
  }

  public class AwaitExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 51;
    public readonly Expression operand;

    public AwaitExpression(DReader reader)
    {
      operand = reader.ReadExpression();
    }
  }

  public class FunctionExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 52;
    public readonly FileOffset fileOffset;
    public readonly FunctionNode function;

    public FunctionExpression(DReader reader)
    {
      fileOffset = new FileOffset(reader);
      function = new FunctionNode(reader);
    }
  }

  public class Let : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 53;
    public readonly VariableDeclaration variable;
    public readonly Expression body;

    public Let(DReader reader)
    {
      variable = new VariableDeclaration(reader);
      body = reader.ReadExpression();
    }
  }

  public class Instantiation : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 54;
    public readonly Expression expression;
    public readonly List<DartType> typeArguments;

    public Instantiation(DReader reader)
    {
      expression = reader.ReadExpression();
      typeArguments = reader.ReadList(r => r.ReadDartType());
    }
  }

  public class LoadLibrary : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 14;
    public readonly LibraryDependencyReference deferredImport;

    public LoadLibrary(DReader reader)
    {
      deferredImport = new LibraryDependencyReference(reader);
    }
  }

  public class CheckLibraryIsLoaded : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 13;
    public readonly LibraryDependencyReference deferredImport;

    public CheckLibraryIsLoaded(DReader reader)
    {
      deferredImport = new LibraryDependencyReference(reader);
    }
  }

  public class ConstantExpression : Expression
  {
    public byte tag => Tag;
    public const byte Tag = 107;
    public readonly CanonicalNameReference constantReference;

    public ConstantExpression(DReader reader)
    {
      constantReference = new CanonicalNameReference(reader);
    }
  }
}