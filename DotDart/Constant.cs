using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace DotDart
{
  public static class ConstantExtensions
  {
    public static Constant ReadConstant(this ComponentReader reader)
    {
      var tag = reader.ReadByte();
      switch (tag)
      {
        case BoolConstant.Tag: return new BoolConstant(reader);
        case DoubleConstant.Tag: return new DoubleConstant(reader);
        case EnvironmentBoolConstant.Tag: return new EnvironmentBoolConstant(reader);
        case EnvironmentIntConstant.Tag: return new EnvironmentIntConstant(reader);
        case EnvironmentStringConstant.Tag: return new EnvironmentStringConstant(reader);
        case InstanceConstant.Tag: return new InstanceConstant(reader);
        case IntConstant.Tag: return new IntConstant(reader);
        case ListConstant.Tag: return new ListConstant(reader);
        case MapConstant.Tag: return new MapConstant(reader);
        case NullConstant.Tag: return new NullConstant();
        case PartialInstantiationConstant.Tag: return new PartialInstantiationConstant(reader);
        case StringConstant.Tag: return new StringConstant(reader);
        case SymbolConstant.Tag: return new SymbolConstant(reader);
        case TearOffConstant.Tag: return new TearOffConstant(reader);
        case TypeLiteralConstant.Tag: return new TypeLiteralConstant(reader);
        case UnevaluatedConstant.Tag: return new UnevaluatedConstant(reader);
        default: throw new Exception($"{nameof(Constant)}: unrecognized tag ({tag})");
      }
    }
  }

  public interface Constant : Node
  {
  }

  public class NullConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 0;
  }

  public class BoolConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 1;
    public readonly byte value;

    public BoolConstant(ComponentReader reader)
    {
      value = reader.ReadByte();
    }

    public BoolConstant(bool value)
    {
      this.value = value ? (byte)1 : (byte)0;
    }
  }


  public class IntConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 2;

    // PositiveIntLiteral | NegativeIntLiteral | SpecializedIntLiteral | BigIntLiteral value;
    public readonly Expression value;

    public IntConstant(ComponentReader reader)
    {
      value = reader.ReadExpression();

      switch (value.tag)
      {
        case PositiveIntLiteral.Tag:
        case NegativeIntLiteral.Tag:
        // case SpecializedIntLiteral.Tag:
        // public byte tag => (byte) (144 + N); // Where 0 <= N < 8.
        case 144:
        case 145:
        case 146:
        case 147:
        case 148:
        case 149:
        case 150:
        case 151:
        case BigIntLiteral.Tag:
          return;
        default:
          throw new Exception($"{nameof(IntConstant)}: improper tag ({tag})");
      }
    }

    public IntConstant(int value)
    {
      if (value < 0)
      {
        this.value = new NegativeIntLiteral(value);
      }
      else
      {
        this.value = new PositiveIntLiteral((uint)value);
      }
    }

    public IntConstant(BigIntLiteral value)
    {
      this.value = value;
    }
  }

  public class DoubleConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 3;
    public readonly double value;

    public DoubleConstant(ComponentReader reader)
    {
      value = reader.ReadDouble();
    }

    public DoubleConstant(double value)
    {
      this.value = value;
    }
  }

  public class StringConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 4;
    public readonly StringReference value;

    public StringConstant(ComponentReader reader)
    {
      value = new StringReference(reader);
    }

    public StringConstant(StringReference value)
    {
      this.value = value;
    }

    public StringConstant(string value)
    {
      this.value = new StringReference(value);
    }
  }

  public class SymbolConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 5;
    public readonly LibraryReference library; // May be NullReference.
    public readonly StringReference name;

    public SymbolConstant(ComponentReader reader)
    {
      library = new LibraryReference(reader);
      name = new StringReference(reader);
    }

    public SymbolConstant(LibraryReference library, StringReference name)
    {
      this.library = library;
      this.name = name;
    }

    public SymbolConstant(StringReference name)
    {
      this.name = name;
    }

    public SymbolConstant(string name)
    {
      this.name = new StringReference(name);
    }
  }

  /*
    type MapConstant extends Constant {
      Byte tag = 6;
      DartType keyType;
      DartType valueType;
      List<[ConstantReference, ConstantReference]> keyValueList;
    }
  */
  public class MapConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 6;
    public readonly DartType keyType;
    public readonly DartType valueType;
    public readonly List<(ConstantReference, ConstantReference)> keyValueList;

    public MapConstant(ComponentReader reader)
    {
      keyType = reader.ReadDartType();
      valueType = reader.ReadDartType();
      keyValueList = reader.ReadList(r => (new ConstantReference(r), new ConstantReference(r)));
    }

    public MapConstant(DartType keyType, DartType valueType, List<(ConstantReference, ConstantReference)> keyValueList)
    {
      this.keyType = keyType;
      this.valueType = valueType;
      this.keyValueList = keyValueList;
    }
  }

  public class ListConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 7;
    public readonly DartType type;
    public readonly List<ConstantReference> values;

    public ListConstant(ComponentReader reader)
    {
      type = reader.ReadDartType();
      values = reader.ReadList(r => new ConstantReference(r));
    }

    public ListConstant(DartType type, List<ConstantReference> values)
    {
      this.type = type;
      this.values = values;
    }
  }

  public class InstanceConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 8;
    public readonly CanonicalNameReference classNameReference;
    public readonly List<DartType> typeArguments;
    public readonly List<(FieldReference, ConstantReference)> values;

    public InstanceConstant(ComponentReader reader)
    {
      classNameReference = new CanonicalNameReference(reader);
      typeArguments = reader.ReadList(r => r.ReadDartType());
      values = reader.ReadList(r => (new FieldReference(r), new ConstantReference(r)));
    }

    public InstanceConstant(CanonicalNameReference classNameReference, List<DartType> typeArguments, List<(FieldReference, ConstantReference)> values)
    {
      this.classNameReference = classNameReference;
      this.typeArguments = typeArguments;
      this.values = values;
    }
  }

  public class PartialInstantiationConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 9;
    public readonly CanonicalNameReference tearOffConstant;
    public readonly List<DartType> typeArguments;

    public PartialInstantiationConstant(ComponentReader reader)
    {
      tearOffConstant = new CanonicalNameReference(reader);
      typeArguments = reader.ReadList(r => r.ReadDartType());
    }

    public PartialInstantiationConstant(CanonicalNameReference tearOffConstant, List<DartType> typeArguments)
    {
      this.tearOffConstant = tearOffConstant;
      this.typeArguments = typeArguments;
    }
  }

  public class TearOffConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 10;
    public readonly CanonicalNameReference staticProcedureReference;

    public TearOffConstant(ComponentReader reader)
    {
      staticProcedureReference = new CanonicalNameReference(reader);
    }

    public TearOffConstant(CanonicalNameReference staticProcedureReference)
    {
      this.staticProcedureReference = staticProcedureReference;
    }
  }

  public class TypeLiteralConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 11;
    public readonly DartType type;

    public TypeLiteralConstant(ComponentReader reader)
    {
      type = reader.ReadDartType();
    }

    public TypeLiteralConstant(DartType type)
    {
      this.type = type;
    }
  }

  [Obsolete("Eliminated in v18, Tag reassigned to UnevaluatedConstant")]
  public class EnvironmentBoolConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 12;
    public readonly StringReference name;
    public readonly CanonicalNameReference defaultValue;

    public EnvironmentBoolConstant(ComponentReader reader)
    {
      name = new StringReference(reader);
      defaultValue = new CanonicalNameReference(reader);
      Console.WriteLine("OBSOLETE");
    }
  }

  [Obsolete("Eliminated in v18")]
  public class EnvironmentIntConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 13;
    public readonly StringReference name;
    public readonly CanonicalNameReference defaultValue;

    public EnvironmentIntConstant(ComponentReader reader)
    {
      name = new StringReference(reader);
      defaultValue = new CanonicalNameReference(reader);
      Console.WriteLine("OBSOLETE");
    }
  }

  [Obsolete("Eliminated in v18")]
  public class EnvironmentStringConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 14;
    public readonly StringReference name;
    public readonly CanonicalNameReference defaultValue;

    public EnvironmentStringConstant(ComponentReader reader)
    {
      name = new StringReference(reader);
      defaultValue = new CanonicalNameReference(reader);
      Console.WriteLine("OBSOLETE");
    }
  }

  public class UnevaluatedConstant : Constant
  {
    public byte tag => Tag;

    // v16
    public const byte Tag = 15;

    // todo: is this produced in current DILL files?
    // v18
    // public const byte Tag = 12;

    public readonly Expression expression;

    public UnevaluatedConstant(ComponentReader reader)
    {
      expression = reader.ReadExpression();
    }

    public UnevaluatedConstant(Expression expression)
    {
      this.expression = expression;
    }
  }
}