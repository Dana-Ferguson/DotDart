using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

    public Field(ComponentReader reader)
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

    public Field(CanonicalNameReference canonicalName, UriReference fileUri, FileOffset fileOffset, FileOffset fileEndOffset,
      Flag flags, Name name, List<Expression> annotations, DartType type, Option<Expression> initializer)
    {
      this.canonicalName = canonicalName;
      this.fileUri = fileUri;
      this.fileOffset = fileOffset;
      this.fileEndOffset = fileEndOffset;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.type = type;
      this.initializer = initializer;
    }

    [Testing]
    public Field(CanonicalNameReference canonicalName, Flag flags, Name name, List<Expression> annotations, DartType type, Option<Expression> initializer)
    {
      this.canonicalName = canonicalName;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.type = type;
      this.initializer = initializer;
    }

    public FieldDeclarationSyntax ToFieldDeclaration()
    {
      if (canonicalName.value != name.name.value)
      {
        Console.WriteLine($"Warning!!! '{canonicalName.value}' != '{name.name.value}';");
      }

      if (annotations.Count != 0) Console.WriteLine($"Warning!!! Annotations!;");

      var declaratorSyntax = SF.VariableDeclarator(SF.Identifier(name.name.value));
      if (initializer.TryGetValue(out var initializerExpression))
      {
        declaratorSyntax = declaratorSyntax.WithInitializer(
          // todo: I think this might not work ... a lot!
          SF.EqualsValueClause(initializerExpression.ToLiteralExpressionSyntax()));
      }

      var fieldDeclaration = SF.FieldDeclaration(
        SF.VariableDeclaration(type.ToTypeSyntax())
          .WithVariables(SF.SingletonSeparatedList<VariableDeclaratorSyntax>(declaratorSyntax)));

      if (flags.HasFlag(Flag.isConst)) fieldDeclaration = fieldDeclaration.WithModifiers(SF.TokenList(SF.Token(SyntaxKind.ConstKeyword)));
      if (flags.HasFlag(Flag.isFinal)) fieldDeclaration = fieldDeclaration.WithModifiers(SF.TokenList(SF.Token(SyntaxKind.ReadOnlyKeyword)));
      if (flags.HasFlag(Flag.isStatic)) fieldDeclaration = fieldDeclaration.WithModifiers(SF.TokenList(SF.Token(SyntaxKind.StaticKeyword)));

      if (flags.HasFlag(Flag.isCovariant)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.isGenericCovariantImpl)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.hasImplicitSetter)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.hasImplicitGetter)) throw new NotImplementedException();

      return fieldDeclaration;
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

    public Constructor(ComponentReader reader)
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

    public Constructor(CanonicalNameReference canonicalName, UriReference fileUri, FileOffset startFileOffset, FileOffset fileOffset, FileOffset fileEndOffset,
      Flag flags, Name name, List<Expression> annotations, FunctionNode function, List<Initializer> initializers)
    {
      this.canonicalName = canonicalName;
      this.fileUri = fileUri;
      this.startFileOffset = startFileOffset;
      this.fileOffset = fileOffset;
      this.fileEndOffset = fileEndOffset;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.function = function;
      this.initializers = initializers;
    }

    [Testing]
    public Constructor(CanonicalNameReference canonicalName, Flag flags, Name name,
      List<Expression> annotations, FunctionNode function, List<Initializer> initializers)
    {
      this.canonicalName = canonicalName;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.function = function;
      this.initializers = initializers;
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

    public Procedure(ComponentReader reader)
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

    public Procedure(CanonicalNameReference canonicalName, UriReference fileUri, FileOffset startFileOffset, FileOffset fileOffset, FileOffset fileEndOffset,
      ProcedureKind kind, Flag flags, Name name, List<Expression> annotations, Option<MemberReference> forwardingStubSuperTarget,
      Option<MemberReference> forwardingStubInterfaceTarget, Option<FunctionNode> function)
    {
      this.canonicalName = canonicalName;
      this.fileUri = fileUri;
      this.startFileOffset = startFileOffset;
      this.fileOffset = fileOffset;
      this.fileEndOffset = fileEndOffset;
      this.kind = kind;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.forwardingStubSuperTarget = forwardingStubSuperTarget;
      this.forwardingStubInterfaceTarget = forwardingStubInterfaceTarget;
      this.function = function;
    }

    [Testing]
    public Procedure(CanonicalNameReference canonicalName, ProcedureKind kind, Flag flags, Name name, List<Expression> annotations,
      Option<MemberReference> forwardingStubSuperTarget, Option<MemberReference> forwardingStubInterfaceTarget, Option<FunctionNode> function)
    {
      this.canonicalName = canonicalName;
      this.kind = kind;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.forwardingStubSuperTarget = forwardingStubSuperTarget;
      this.forwardingStubInterfaceTarget = forwardingStubInterfaceTarget;
      this.function = function;
    }

    public MethodDeclarationSyntax ToMethodDeclaration()
    {
      bool isMain = false;
      TypeSyntax mainReturnSyntax = null;

      TypeSyntax returnType = SF.PredefinedType(
        SF.Token(SyntaxKind.VoidKeyword));

      var procedureName = canonicalName.value;
      if (procedureName == "main")
      {
        isMain = true;
      }
      if (procedureName != name.name.value || name.library != null) Console.WriteLine($"Take a look at this! {name.library?.canonicalName?.value}.{name.name.value}");

      if (function.TryGetValue(out var functionNode))
      {
          returnType = functionNode.returnType.ToTypeSyntax();
      }

      var method = SF.MethodDeclaration(returnType, SF.Identifier(procedureName));

      var modifiers = new List<SyntaxToken>();

      if (flags.HasFlag(Flag.isConst)) modifiers.Add(SF.Token(SyntaxKind.ConstKeyword));
      if (flags.HasFlag(Flag.isStatic)) modifiers.Add(SF.Token(SyntaxKind.StaticKeyword));
      // if (isMain) modifiers.Add(SF.Token(SyntaxKind.PublicKeyword));
      if (!procedureName.StartsWith('_')) modifiers.Add(SF.Token(SyntaxKind.PublicKeyword));

      if (flags.HasFlag(Flag.isAbstract)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.isExternal)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.isForwardingStub)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.isForwardingSemiStub)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.isRedirectingFactoryConstructor)) throw new NotImplementedException();
      if (flags.HasFlag(Flag.isNoSuchMethodForwarder)) throw new NotImplementedException();

      if (modifiers.Count == 1)
      {
        method = method.WithModifiers(SF.TokenList(modifiers.First()));
      }
      else if (modifiers.Count != 0)
      {
        method = method.WithModifiers(SF.TokenList(modifiers.ToArray()));
      }

      // todo: in our test cases this is a dart Block which goes to BlockSyntax ~ is this always the case?
      // --> I'm guessing it's not for arrow functions???
      if (functionNode.body.TryGetValue(out var body))
      {
        // if 'isMain' we need to rewrite all return blocks
        var blockSyntax = body.ToStatementSyntax() as BlockSyntax;
        blockSyntax = SF.Block(blockSyntax.Statements.Add(SF.ReturnStatement(SF.LiteralExpression(
          SyntaxKind.DefaultLiteralExpression,
          SF.Token(SyntaxKind.DefaultKeyword)))));
        method = method.WithBody(blockSyntax);
      }

      return method;
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

    public RedirectingFactoryConstructor(ComponentReader reader)
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

    public RedirectingFactoryConstructor(CanonicalNameReference canonicalName, UriReference fileUri, FileOffset fileOffset, FileOffset fileEndOffset,
      byte flags, Name name, List<Expression> annotations, MemberReference targetReference, List<DartType> typeArguments, List<TypeParameter> typeParameters,
      uint parameterCount, uint requiredParameterCount, List<VariableDeclaration> positionalParameters, List<VariableDeclaration> namedParameters)
    {
      this.canonicalName = canonicalName;
      this.fileUri = fileUri;
      this.fileOffset = fileOffset;
      this.fileEndOffset = fileEndOffset;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.targetReference = targetReference;
      this.typeArguments = typeArguments;
      this.typeParameters = typeParameters;
      this.parameterCount = parameterCount;
      this.requiredParameterCount = requiredParameterCount;
      this.positionalParameters = positionalParameters;
      this.namedParameters = namedParameters;
    }

    [Testing]
    public RedirectingFactoryConstructor(CanonicalNameReference canonicalName,
      byte flags, Name name, List<Expression> annotations, MemberReference targetReference, List<DartType> typeArguments, List<TypeParameter> typeParameters,
      uint parameterCount, uint requiredParameterCount, List<VariableDeclaration> positionalParameters, List<VariableDeclaration> namedParameters)
    {
      this.canonicalName = canonicalName;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.targetReference = targetReference;
      this.typeArguments = typeArguments;
      this.typeParameters = typeParameters;
      this.parameterCount = parameterCount;
      this.requiredParameterCount = requiredParameterCount;
      this.positionalParameters = positionalParameters;
      this.namedParameters = namedParameters;
    }
  }
}