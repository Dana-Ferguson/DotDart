using System;
using System.Collections.Generic;

namespace DotDart
{
  public static class DartTypeExtensions
  {
    // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L1852
    // we have much different concepts of what DartType is? ~ Will this lead to different functionality?
    // Is this just because of changes in binary.md?
    // TypedefType (we have, but not a type)
    // BottomType (not in binary.md)
    public static DartType ReadDartType(this ComponentReader reader)
    {
      var tag = reader.ReadByte();
      switch (tag)
      {
        case DynamicType.Tag: return new DynamicType();
        case FunctionType.Tag: return new FunctionType(reader);
        case InterfaceType.Tag: return new InterfaceType(reader);
        case InvalidType.Tag: return new InvalidType();
        case SimpleFunctionType.Tag: return new SimpleFunctionType(reader);
        case SimpleInterfaceType.Tag: return new SimpleInterfaceType(reader);
        case TypeParameterType.Tag: return new TypeParameterType(reader);
        case VoidType.Tag: return new VoidType();
        default: throw new Exception($"{nameof(DartType)}: unrecognized tag ({tag})");
      }
    }
  }

  public interface DartType : Node
  {
  }

  public class InvalidType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 90;
  }

  public class DynamicType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 91;
  }

  public class VoidType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 92;
  }

  public class InterfaceType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 93;
    public readonly ClassReference classReference;
    public readonly List<DartType> typeArguments;

    public InterfaceType(ComponentReader reader)
    {
      classReference = new ClassReference(reader);
      typeArguments = reader.ReadList(r => r.ReadDartType());
    }
  }

  public class SimpleInterfaceType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 96; // Note: tag is out of order.

    public readonly ClassReference classReference;
    // Equivalent to InterfaceType with empty list of type arguments.

    public SimpleInterfaceType(ComponentReader reader)
    {
      classReference = new ClassReference(reader);
    }
  }

  public class FunctionType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 94;
    public readonly List<TypeParameter> typeParameters;

    public readonly uint requiredParameterCount;

    // positionalParameters.length + namedParameters.length
    public readonly uint totalParameterCount;
    public readonly List<DartType> positionalParameters;
    public readonly List<NamedDartType> namedParameters;

    public readonly CanonicalNameReference typedefReference;

    // public readonly Option<TypedefType> typedef;
    public readonly DartType returnType;

    public FunctionType(ComponentReader reader)
    {
      typeParameters = reader.ReadList(r => new TypeParameter(r));
      requiredParameterCount = reader.ReadUint();
      totalParameterCount = reader.ReadUint();
      positionalParameters = reader.ReadList(r => r.ReadDartType());
      namedParameters = reader.ReadList(r => new NamedDartType(r));

      // #v12:   CanonicalNameReference typedefReference;
      // #v12+:  Option<TypedefType> typedef;
      typedefReference = new CanonicalNameReference(reader);
      //typedef = reader.ReadOption(r => new TypedefType(r));

      returnType = reader.ReadDartType();
    }
  }

  public class SimpleFunctionType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 97; // Note: tag is out of order.
    public readonly List<DartType> positionalParameters;

    // This is in Binary.MD but not in the source
    // public readonly List<StringReference> positionalParameterNames;

    public readonly DartType returnType;
    // Equivalent to a FunctionType with no type parameters or named parameters,
    // and where all positional parameters are required.

    public SimpleFunctionType(ComponentReader reader)
    {
      positionalParameters = reader.ReadList(r => r.ReadDartType());

      // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L1889
      // MISSING!
      // positionalParameterNames = reader.ReadList(r => new StringReference(r));
      // Console.WriteLine(string.Join(", ", positionalParameterNames.Select(n => reader.GetString(n.index))));
      returnType = reader.ReadDartType();
    }
  }

  public class NamedDartType
  {
    public readonly StringReference name;
    public readonly DartType type;

    public NamedDartType(ComponentReader reader)
    {
      name = new StringReference(reader);
      type = reader.ReadDartType();
    }
  }

  public class TypeParameterType : DartType
  {
    public byte tag => Tag;
    public const byte Tag = 95;

    // Reference to the Nth type parameter in scope (with some caveats about
    // type parameter bounds).
    //
    // As with the other indexing schemes, outermost nodes have lower
    // indices, and a type parameter list is consecutively indexed from
    // left to right.
    //
    // In the case of type parameter bounds, this indexing diverges slightly
    // from the definition of scoping, since type parameter N+1 is not "in scope"
    // in the bound of type parameter N, but it takes up an index as if it was in
    // scope there.
    //
    // The type parameter can be bound by a Class, FunctionNode, or FunctionType.
    //
    // Note that constructors currently do not declare type parameters.  Uses of
    // the class type parameters in a constructor refer to those declared on the
    // class.
    public readonly uint index;
    public readonly Option<DartType> bound;

    public TypeParameterType(ComponentReader reader)
    {
      index = reader.ReadUint();
      bound = reader.ReadOption(r => r.ReadDartType());
    }
  }
}