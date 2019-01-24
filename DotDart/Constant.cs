using System;
using System.Collections.Generic;

namespace DotDart
{
  public static class ConstantExtensions
  {
    public static Constant ReadConstant(this DReader reader)
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

    public BoolConstant(DReader reader)
    {
      value = reader.ReadByte();
    }
  }


  public class IntConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 2;

    // PositiveIntLiteral | NegativeIntLiteral | SpecializedIntLiteral | BigIntLiteral value;
    public readonly Expression value;

    public IntConstant(DReader reader)
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
  }

  public class DoubleConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 3;
    public readonly double value;

    public DoubleConstant(DReader reader)
    {
      value = reader.ReadDouble();
    }
  }

  public class StringConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 4;
    public readonly StringReference value;

    public StringConstant(DReader reader)
    {
      value = new StringReference(reader);
    }
  }

  public class SymbolConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 5;
    public readonly LibraryReference library; // May be NullReference.
    public readonly StringReference name;

    public SymbolConstant(DReader reader)
    {
      library = new LibraryReference(reader);
      name = new StringReference(reader);
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

    public MapConstant(DReader reader)
    {
      keyType = reader.ReadDartType();
      valueType = reader.ReadDartType();
      keyValueList = reader.ReadList(r => (new ConstantReference(r), new ConstantReference(r)));
    }
  }

  public class ListConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 7;
    public readonly DartType type;
    public readonly List<ConstantReference> values;

    public ListConstant(DReader reader)
    {
      type = reader.ReadDartType();
      values = reader.ReadList(r => new ConstantReference(r));
    }
  }

  public class InstanceConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 8;
    public readonly CanonicalNameReference classNameReference;
    public readonly List<DartType> typeArguments;
    public readonly List<(FieldReference, ConstantReference)> values;

    public InstanceConstant(DReader reader)
    {
      classNameReference = new CanonicalNameReference(reader);
      typeArguments = reader.ReadList(r => r.ReadDartType());
      values = reader.ReadList(r => (new FieldReference(r), new ConstantReference(r)));
    }
  }

  public class PartialInstantiationConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 9;
    public readonly CanonicalNameReference tearOffConstant;
    public readonly List<DartType> typeArguments;

    public PartialInstantiationConstant(DReader reader)
    {
      tearOffConstant = new CanonicalNameReference(reader);
      typeArguments = reader.ReadList(r => r.ReadDartType());
    }
  }

  public class TearOffConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 10;
    public readonly CanonicalNameReference staticProcedureReference;

    public TearOffConstant(DReader reader)
    {
      staticProcedureReference = new CanonicalNameReference(reader);
    }
  }

  public class TypeLiteralConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 11;
    public readonly DartType type;

    public TypeLiteralConstant(DReader reader)
    {
      type = reader.ReadDartType();
    }
  }

  [Obsolete("Eliminated in v18, Tag reassigned to UnevaluatedConstant")]
  public class EnvironmentBoolConstant : Constant
  {
    public byte tag => Tag;
    public const byte Tag = 12;
    public readonly StringReference name;
    public readonly CanonicalNameReference defaultValue;

    public EnvironmentBoolConstant(DReader reader)
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

    public EnvironmentIntConstant(DReader reader)
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

    public EnvironmentStringConstant(DReader reader)
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

    public UnevaluatedConstant(DReader reader)
    {
      expression = reader.ReadExpression();
    }
  }
}