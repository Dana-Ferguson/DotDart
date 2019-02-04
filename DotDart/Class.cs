using System;
using System.Collections.Generic;

namespace DotDart
{

  public enum ClassLevel
  {
    Type = 0,
    Hierarchy = 1,
    Mixin = 2,
    Body = 3,
  }


  // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L937

  // A class can be represented at one of three levels: type, hierarchy, or body.
  //
  // If the enclosing library is external, a class is either at type or
  // hierarchy level, depending on its isTypeLevel flag.
  // If the enclosing library is not external, a class is always at body level.
  //
  // See ClassLevel in ast.dart for the details of each loading level.
  public class Class : Node
  {
    public byte tag => Tag;
    public const byte Tag = 2;

    public readonly CanonicalNameReference canonicalName;

    // An absolute path URI to the .dart file from which the class was created.
    public readonly UriReference fileUri;
    public readonly FileOffset startFileOffset; // Offset of the start of the class including any annotations.
    public readonly FileOffset fileOffset; // Offset of the name of the class.
    public readonly FileOffset fileEndOffset;

    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      levelBit0 = 0x1,
      levelBit1 = 0x2,
      isAbstract = 0x4,
      isEnum = 0x8,
      isAnonymousMixin = 0x10,
      isEliminatedMixin = 0x20,
      isMixinDeclaration = 0x40
    }

    private ClassLevel classLevel => (ClassLevel) ((byte)flags & 3);

    public readonly StringReference name;
    public readonly List<Expression> annotations;
    public readonly List<TypeParameter> typeParameters;

    public readonly Option<DartType> superClass;

    // For transformed mixin application classes (isEliminatedMixin),
    // original mixedInType is pulled into the end of implementedClasses.
    public readonly Option<DartType> mixedInType;
    public readonly List<DartType> implementedClasses;
    public readonly List<Field> fields;
    public readonly List<Constructor> constructors;
    public readonly List<Procedure> procedures;
    public readonly List<RedirectingFactoryConstructor> redirectingFactoryConstructors;

    // Class index. Offsets are used to get start (inclusive) and end (exclusive) byte positions for
    // a specific procedure. Note the "+1" to account for needing the end of the last entry.
    // uint[procedures.length + 1]
    public readonly uint [] procedureOffsets;
    public uint procedureCount => (uint) procedures.Count;

    public Class(ComponentReader reader)
    {
      reader.CheckTag(Tag);

      canonicalName = new CanonicalNameReference(reader);

      fileUri = new UriReference(reader);
      startFileOffset = new FileOffset(reader);
      fileOffset = new FileOffset(reader);
      fileEndOffset = new FileOffset(reader);

      flags = (Flag) reader.ReadByte();

      name = new StringReference(reader);
      annotations = reader.ReadList(r => r.ReadExpression());
      typeParameters = reader.ReadList(r => new TypeParameter(r));

      superClass = reader.ReadOption(r => r.ReadDartType());

      mixedInType = reader.ReadOption(r => r.ReadDartType());
      implementedClasses = reader.ReadList(r => r.ReadDartType());
      fields = reader.ReadList(r => new Field(r));
      constructors = reader.ReadList(r => new Constructor(r));
      procedures = reader.ReadList(r => new Procedure(r));
      redirectingFactoryConstructors = reader.ReadList(r => new RedirectingFactoryConstructor(r));
    }

    // todo: this needs a builder class
    public Class(CanonicalNameReference canonicalName, UriReference fileUri, FileOffset startFileOffset, FileOffset fileOffset, FileOffset fileEndOffset,
      Flag flags, StringReference name, List<Expression> annotations, List<TypeParameter> typeParameters, Option<DartType> superClass,
      Option<DartType> mixedInType, List<DartType> implementedClasses, List<Field> fields, List<Constructor> constructors, List<Procedure> procedures,
      List<RedirectingFactoryConstructor> redirectingFactoryConstructors, uint[] procedureOffsets)
    {
      this.canonicalName = canonicalName;
      this.fileUri = fileUri;
      this.startFileOffset = startFileOffset;
      this.fileOffset = fileOffset;
      this.fileEndOffset = fileEndOffset;
      this.flags = flags;
      this.name = name;
      this.annotations = annotations;
      this.typeParameters = typeParameters;
      this.superClass = superClass;
      this.mixedInType = mixedInType;
      this.implementedClasses = implementedClasses;
      this.fields = fields;
      this.constructors = constructors;
      this.procedures = procedures;
      this.redirectingFactoryConstructors = redirectingFactoryConstructors;
      this.procedureOffsets = procedureOffsets;
    }
  }

}