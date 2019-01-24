using System;
using System.Collections.Generic;

namespace DotDart
{
  public interface Member : Node
  {
  }

  public class Field : Member
  {
    public byte tag => Tag;
    public const byte Tag = 4;

    public readonly CanonicalNameReference canonicalName;

    // An absolute path URI to the .dart file from which the field was created.
    public readonly UriReference fileUri;
    public readonly FileOffset fileOffset;
    public readonly FileOffset fileEndOffset;


    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      isFinal = 0x1,
      isConst = 0x2,
      isStatic = 0x4,
      hasImplicitGetter = 0x8,
      hasImplicitSetter = 0x10,
      isCovariant = 0x20,
      isGenericCovariantImpl = 0x40
    }

    public readonly Name name;
    public readonly List<Expression> annotations;
    public readonly DartType type;
    public readonly Option<Expression> initializer;

    public Field(DReader reader)
    {
      reader.CheckTag(Tag);

      canonicalName = new CanonicalNameReference(reader);
      fileUri = new UriReference(reader);
      fileOffset = new FileOffset(reader);
      fileEndOffset = new FileOffset(reader);
      flags = (Flag) reader.ReadByte();
      name = new Name(reader);
      annotations = reader.ReadList(r => r.ReadExpression());
      type = reader.ReadDartType();
      initializer = reader.ReadOption(r => r.ReadExpression());
    }
  }

  public class Constructor : Member
  {
    public byte tag => Tag;
    public const byte Tag = 5;
    public readonly CanonicalNameReference canonicalName;
    public readonly UriReference fileUri;
    public readonly FileOffset startFileOffset; // Offset of the start of the constructor including any annotations.
    public readonly FileOffset fileOffset; // Offset of the constructor name.
    public readonly FileOffset fileEndOffset;

    public readonly Flag flags;

    // Where level is index into ClassLevel
    [Flags]
    public enum Flag : byte
    {
      isConst = 0x1,
      isExternal = 0x2,
      isSynthetic = 0x4
    }


    public readonly Name name;
    public readonly List<Expression> annotations;
    public readonly FunctionNode function;
    public readonly List<Initializer> initializers;

    public Constructor(DReader reader)
    {
      reader.CheckTag(Tag);

      canonicalName = new CanonicalNameReference(reader);
      fileUri = new UriReference(reader);
      startFileOffset = new FileOffset(reader);
      fileOffset = new FileOffset(reader);
      fileEndOffset = new FileOffset(reader);
      flags = (Flag) reader.ReadByte();
      name = new Name(reader);
      annotations = reader.ReadList(r => r.ReadExpression());
      function = new FunctionNode(reader);
      initializers = reader.ReadList(r => r.ReadInitializer());
    }
  }

/*
enum ProcedureKind {
  Method,
  Getter,
  Setter,
  Operator,
  Factory,
}
*/

  public class Procedure : Member
  {
    public byte tag => Tag;
    public const byte Tag = 6;

    public readonly CanonicalNameReference canonicalName;

    // An absolute path URI to the .dart file from which the class was created.
    public readonly UriReference fileUri;
    public readonly FileOffset startFileOffset; // Offset of the start of the procedure including any annotations.
    public readonly FileOffset fileOffset; // Offset of the procedure name.
    public readonly FileOffset fileEndOffset;

    public enum ProcedureKind : byte {
      Method,
      Getter,
      Setter,
      Operator,
      Factory,
    }

    public readonly ProcedureKind kind; // Index into the ProcedureKind enum above.

    public readonly Flag flags;

    // Index into the ProcedureKind enum above.
    [Flags]
    public enum Flag : byte
    {
      isStatic = 0x1,
      isAbstract = 0x2,
      isExternal = 0x4,
      isConst = 0x8,
      isForwardingStub = 0x10,
      isForwardingSemiStub = 0x20,
      isRedirectingFactoryConstructor = 0x40,
      isNoSuchMethodForwarder = 0x80
    }

    public readonly Name name;
    public readonly List<Expression> annotations;

    // Only present if the 'isForwardingStub' flag is set.
    //public readonly MemberReference forwardingStubSuperTarget; // May be NullReference.
    //public readonly MemberReference forwardingStubInterfaceTarget; // May be NullReference.
    public readonly Option<MemberReference> forwardingStubSuperTarget;
    public readonly Option<MemberReference> forwardingStubInterfaceTarget;

    // Can only be absent if abstract, but tag is there anyway.
    public readonly Option<FunctionNode> function;

    public Procedure(DReader reader)
    {
      var s = reader.Position;
      reader.CheckTag(Tag);

      canonicalName = new CanonicalNameReference(reader);
      fileUri = new UriReference(reader);
      startFileOffset = new FileOffset(reader);

      fileOffset = new FileOffset(reader);
      fileEndOffset = new FileOffset(reader);
      kind = (ProcedureKind)reader.ReadByte();
      flags = (Flag) reader.ReadByte();
      name = new Name(reader);
      annotations = reader.ReadList(r => r.ReadExpression());

      // #V12
      forwardingStubSuperTarget = reader.ReadOption(r => new MemberReference(r));
      forwardingStubInterfaceTarget = reader.ReadOption(r => new MemberReference(r));

      // #V18?
      //forwardingStubSuperTarget = new MemberReference(reader);
      //forwardingStubInterfaceTarget = new MemberReference(reader);

      function = reader.ReadOption(r => new FunctionNode(r));
    }
  }

  public class RedirectingFactoryConstructor : Member
  {
    public byte tag => Tag;
    public const byte Tag = 107;
    public readonly CanonicalNameReference canonicalName;
    public readonly UriReference fileUri;
    public readonly FileOffset fileOffset;
    public readonly FileOffset fileEndOffset;

    // todo: what are these flags?
    public readonly byte flags;


    public readonly Name name;
    public readonly List<Expression> annotations;
    public readonly MemberReference targetReference;
    public readonly List<DartType> typeArguments;
    public readonly List<TypeParameter> typeParameters;
    public readonly uint parameterCount; // positionalParameters.length + namedParameters.length.
    public readonly uint requiredParameterCount;
    public readonly List<VariableDeclaration> positionalParameters;
    public readonly List<VariableDeclaration> namedParameters;

    public RedirectingFactoryConstructor(DReader reader)
    {
      reader.CheckTag(Tag);

      canonicalName = new CanonicalNameReference(reader);
      fileUri = new UriReference(reader);
      fileOffset = new FileOffset(reader);
      fileEndOffset = new FileOffset(reader);

      flags = reader.ReadByte();

      name = new Name(reader);
      annotations = reader.ReadList(r => r.ReadExpression());
      targetReference = new MemberReference(reader);
      typeArguments = reader.ReadList(r => r.ReadDartType());
      typeParameters = reader.ReadList(r => new TypeParameter(r));
      parameterCount = reader.ReadUint();
      requiredParameterCount = reader.ReadUint();
      positionalParameters = reader.ReadList(r => new VariableDeclaration(r));
      namedParameters = reader.ReadList(r => new VariableDeclaration(r));
    }
  }

}