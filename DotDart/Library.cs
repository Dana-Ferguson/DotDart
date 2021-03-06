using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DotDart
{
  /*
  class Library {
    byte flags (isExternal);
    CanonicalNameReference canonicalName;
    public readonly StringReference name;
    // An absolute path URI to the .dart file from which the library was created.
    UriReference fileUri;
    List<Expression> annotations;
    List<LibraryDependency> libraryDependencies;
    List<CanonicalNameReference> additionalExports;
    List<LibraryPart> libraryParts;
    List<Typedef> typedefs;
    List<Class> classes;
    List<Field> fields;
    List<Procedure> procedures;

    // Library index. Offsets are used to get start (inclusive) and end (exclusive) byte positions for
    // a specific class or procedure. Note the "+1" to account for needing the end of the last entry.
    UInt32[classes.length + 1] classOffsets;
    UInt32 classCount = classes.length;
    UInt32[procedures.length + 1] procedureOffsets;
    UInt32 procedureCount = procedures.length;
  }
  */
  public class Library
  {
    // todo: what are these flags?
    public readonly byte flags;

    public readonly CanonicalNameReference canonicalName;

    public readonly StringReference name;

    // An absolute path URI to the .dart file from which the library was created.
    public readonly UriReference fileUri;
    public readonly List<Expression> annotations;
    public readonly List<LibraryDependency> libraryDependencies;
    public readonly List<CanonicalNameReference> additionalExports;
    public readonly List<LibraryPart> libraryParts;
    public readonly List<Typedef> typedefs;
    public readonly List<Class> classes;
    public readonly List<Field> fields;
    public readonly List<Procedure> procedures;

    // Library index. Offsets are used to get start (inclusive) and end (exclusive) byte positions for
    // a specific class or procedure. Note the "+1" to account for needing the end of the last entry.
    public readonly uint[] classOffsets;
    public readonly uint classCount;
    public readonly uint[] procedureOffsets;
    public readonly uint procedureCount;

    // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L760
    public Library(ComponentReader reader)
    {
      // Read the Offsets and Counts in a backward fashion
      var p = reader.Length - sizeof(uint);
      reader.Position = p;
      procedureCount = reader.ReadUint32();

      p -= sizeof(uint) * ((int)procedureCount + 1);
      reader.Position = p;
      procedureOffsets = reader.ReadUint32s((int)procedureCount + 1);

      p -= sizeof(uint);
      reader.Position = p;
      classCount = reader.ReadUint32();

      p -= sizeof(uint) * ((int)classCount + 1);
      reader.Position = p;
      classOffsets = reader.ReadUint32s((int)classCount + 1);

      reader.Position = 0;
      flags = reader.ReadByte();
      canonicalName = new CanonicalNameReference(reader);
      name = new StringReference(reader);

      // todo: remove!
      Console.WriteLine($"{reader.GetString(name)} :: {classOffsets.Length} :: {classCount} || {procedureOffsets.Length} :: {procedureCount};");

      fileUri = new UriReference(reader);

      //   List<String> problemsAsJson; // Described in problems.md.

      annotations = reader.ReadList(r => r.ReadExpression());
      libraryDependencies = reader.ReadList(r => new LibraryDependency(r));
      additionalExports = reader.ReadList(r => new CanonicalNameReference(r));
      libraryParts = reader.ReadList(r => new LibraryPart(r));
      typedefs = reader.ReadList(r => new Typedef(r));

      // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L831
      // classes = reader.ReadList(r => new Class(r));
      classes = new List<Class>((int)classCount);
      for (int i = 0; i < classCount; i++)
      {
        var start = classOffsets[i];
        var end = classOffsets[i + 1];
        classes.Add(new Class(reader.GetWindow(start, end - start)));
      }

      reader.Position = (int)classOffsets.Last() - reader.WindowOffset;
      fields = reader.ReadList(r => new Field(r));

      // procedures = reader.ReadList(r => new Procedure(r));
      procedures = new List<Procedure>((int)procedureCount);
      for (int i = 0; i < procedureCount; i++)
      {
        var start = procedureOffsets[i];
        var end = procedureOffsets[i + 1];
        procedures.Add(new Procedure(reader.GetWindow(start, end - start)));
      }
    }

    // We might want to combine all the libraries together into a single CompilationUnit, we could combine all the usings
    public object ToLibrary()
    {
      if (name.index != 0)
      {
        Console.WriteLine($"Warning, Library has a Name => {name.value};");
      }

      var safeLibraryName = SafeLibraryName();
      var library = SF.ClassDeclaration(safeLibraryName)
        .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.StaticKeyword)));

      // todo: annotations
      // todo: libraryDependencies ~ static usings?
      // todo: additionalExports ~ I'm unsure how to handle this ~ I may not need to
      // todo: libraryParts ~ hmm.... will this happen?
      // todo: typedefs
      // todo: classes <-- sub-classes to my static classing

      // fields
      var fieldDeclarationList = fields.Select(field => field.ToFieldDeclaration());
      // procedures
      var methodDeclarationList = procedures.Select(procedure => procedure.ToMethodDeclaration());

      library = library.WithMembers(SF.List(fieldDeclarationList.Concat<MemberDeclarationSyntax>(methodDeclarationList)));

      // I assume we can remove this during production for increased efficiency?
      return library.NormalizeWhitespace();
    }

    public string SafeLibraryName()
    {
      // We need to make sure that this is safe
      // 'file:///home/dana/RiderProjects/DartAstTest/DotDart/test_scripts/hello.dart'

      // todo: optimize -- we can probably just do an isLetter test?
      return string.Join("_",canonicalName.value.Split(new char[] {':', '/', '.'}, StringSplitOptions.RemoveEmptyEntries));
    }
  }

}