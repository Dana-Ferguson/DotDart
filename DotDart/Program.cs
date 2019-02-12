using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming

// https://sharplab.io

namespace DotDart
{
  using static TestDartLibrary;
  public static class TestDartLibrary
  {
    public static string dart = "dart-lang";

    public class TestClass
    {
      //
    }
  }
  public class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("DotDart says hi!");

      // CompileDill();
      Test01();

      var tc = new TestClass();
    }

    static void Test01()
    {
      var filename = "ddc_sdk.dill";
      filename = "test_scripts/hello.dill";

      var bytes = System.IO.File.ReadAllBytes(filename);
      Console.WriteLine(bytes.Length);

      // foreach (var b in bytes.Take(16)) Console.WriteLine(b.ToString("X2"));

      var componentFile = ComponentFile.Load(filename);
      Console.WriteLine(new DartStringBuilder().Serialize(componentFile).ToString());
      Console.WriteLine();

      int x = 3;
    }

    static void CompileDill()
    {
      const string target = "test_scripts/hello.dill";
      const string source = "test_scripts/hello.dart";
      Dart.CompileDill(source, target);
    }
  }

  public static class Dart
  {
    public static void CompileDill(string source, string target)
    {
      var procStartInfo = new ProcessStartInfo("dart", $"--snapshot={target} --snapshot-kind=kernel {source}")
      {
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      var proc = new Process
      {
        StartInfo = procStartInfo
      };

      proc.Start();
      var result = proc.StandardOutput.ReadToEnd();
      Console.WriteLine(result);

      // todo: test the result?
    }
  }

  public class TagException : Exception
  {
    public TagException(byte actual, byte expected) :
      base($"Improper Tag: actual = {actual}; expected = {expected};")
    {
    }
  }

  // Untagged Pairs
  public class Pair<TKey, TValue>
  {
    public readonly TKey key;
    public readonly TValue value;

    public Pair(TKey key, TValue value)
    {
      this.key = key;
      this.value = value;
    }
  }

  // Backend specific metadata section.
  public class MetadataPayload
  {
    public readonly byte[] opaquePayload;

    public MetadataPayload(ComponentReader reader, int count)
    {
      opaquePayload = reader.ReadBytes(count);
    }
  }

  /*
  type MetadataMapping {
    UInt32 tag;  // StringReference of a fixed size.
    // Node offsets are absolute, while metadata offsets are relative to metadataPayloads.
    RList<Pair<UInt32, UInt32>> nodeOffsetToMetadataOffset;
  }
  */
  // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/lib/binary/ast_from_binary.dart#L2011
  public class MetadataMapping
  {
    public readonly uint tag; // StringReference of a fixed size.

    // Node offsets are absolute, while metadata offsets are relative to metadataPayloads.
    // *RLIST*
    public readonly List<Pair<uint, uint>> nodeOffsetToMetadataOffset;

    public MetadataMapping(ComponentReader reader)
    {
      // we need to start at the end
      // todo: do we need to read the tag?
      // nodeOffsetToMetadataOffset = reader.ReadList(r => new Pair<uint, uint>(r.ReadUint32(), r.ReadUint32()));
    }

    public MetadataMapping(uint tag, List<Pair<uint, uint>> nodeOffsetToMetadataOffset)
    {
      this.tag = tag;
      this.nodeOffsetToMetadataOffset = nodeOffsetToMetadataOffset;
    }
  }


  /*
  abstract class Node { byte tag; }
  abstract class Expression extends Node {}
  abstract class Constant extends Node {}

  */
  public interface Node
  {
    byte tag { get; }
  }

  public class ConstantReference
  {
    public readonly uint index; // Index into the Component's constants.

    public ConstantReference(ComponentReader reader)
    {
      index = reader.ReadUint();
    }
  }

  public class SourceInfo
  {
    public readonly List<byte> uriUtf8Bytes;
    public readonly List<byte> sourceUtf8Bytes;

    // Line starts are delta-encoded (they are encoded as line lengths).  The list
    // [0, 10, 25, 32, 42] is encoded as [0, 10, 15, 7, 10].
    public readonly List<uint> lineStarts;

    public readonly string uri;
    public readonly string source;
    // todo: we could just store begins and ends
    public readonly string[] lines;

    public SourceInfo(ComponentReader reader)
    {
      // todo: micro-optimize
      uriUtf8Bytes = reader.ReadList(r => r.ReadByte());
      sourceUtf8Bytes = reader.ReadList(r => r.ReadByte());
      lineStarts = reader.ReadList(r => r.ReadUint());

      uri = Encoding.UTF8.GetString(uriUtf8Bytes.ToArray());
      source = Encoding.UTF8.GetString(sourceUtf8Bytes.ToArray());
      lines = new string[lineStarts.Count];

      // todo: optimize -- this is _not_ efficient
      int p = 0;
      for (int i = 0; i < lineStarts.Count; i++)
      {
        p += (int)lineStarts[i];
        var line = p >= source.Length ? $"{p} > {source.Length}" :  source.Substring(p);
        var end = line.IndexOf(Environment.NewLine, StringComparison.Ordinal);
        if (end != -1) line = line.Substring(0, end);
        Console.WriteLine($"*** {line}");
        lines[i] = line;
      }
    }
  }

/*
type UriSource {
  UInt32 length;
  SourceInfo[length] source;
  // The ith entry is byte-offset to the ith Source.
  UInt32[length] sourceIndex;
}
*/
  public class UriSource
  {
    public readonly uint length;

    SourceInfo[] source;

    // The ith entry is byte-offset to the ith Source.
    public readonly uint [] sourceIndex;

    public UriSource(ComponentReader reader)
    {
      length = reader.ReadUint32();
      source = new SourceInfo[length];
      sourceIndex = new uint[length];

      for (int i = 0; i < length; i++) source[i] = new SourceInfo(reader);
      for (int i = 0; i < length; i++) sourceIndex[i] = reader.ReadUint32();
    }
  }

  public class FileOffset
  {
    // Encoded as offset + 1 to accommodate -1 indicating no offset.
    public readonly uint fileOffset;

    public FileOffset(ComponentReader reader)
    {
      fileOffset = reader.ReadUint() - 1;
    }

    public void Serialize(DartStringBuilder sb)
    {
      sb.AppendHeader(nameof(FileOffset)).Append($"{fileOffset}");
    }
  }

  public static class OptionExtensions
  {
    public static Option<T> ReadOption<T>(this ComponentReader reader, Func<ComponentReader, T> valueReader)
    {
      var myTag = reader.ReadByte();
      if (myTag == Nothing<T>.Tag) return new Nothing<T>();
      if (myTag == Something<T>.Tag) return new Something<T>(reader, valueReader);

      throw new Exception($"Invalid Option Tag ({myTag})");
    }
  }

  // todo: Why isn't this abstract like Node? (question about Dart's design)
  public class Option<T>
  {
    public readonly byte tag;

    public T GetValue() => this is Something<T> something ? something.value : default;

    public bool TryGetValue(out T variable)
    {
      if (this is Something<T> something)
      {
        variable = something.value;
        return true;
      }

      variable = default;
      return false;
    }
  }

  // was Nothing : Option<T>, but that doesn't work in C#
  public class Nothing<T> : Option<T>
  {
    public byte tag => Tag;
    public const byte Tag = 0;

    public void Serialize(DartStringBuilder sb)
    {
      sb.Append($"Nothing<{typeof(T)}>");
    }
  }

  public class Something<T> : Option<T>
  {
    public byte tag => Tag;
    public const byte Tag = 1;
    public readonly T value;

    public Something(ComponentReader reader, Func<ComponentReader, T> valueReader)
    {
      value = valueReader(reader);
    }

    public Something(T value)
    {
      this.value = value;
    }

    public void Serialize(DartStringBuilder sb)
    {
      sb.Append($"Something<{typeof(T)}>")
      .Indent()
        .Serialize(value)
      .Dedent();
    }
  }

  public class LibraryDependency
  {
    public readonly FileOffset fileOffset;

    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      isExport = 0x1,
      isDeferred = 0x2
    }

    public readonly List<Expression> annotations;
    public readonly LibraryReference targetLibrary;
    public readonly StringReference name;
    public readonly List<Combinator> combinators;

    public LibraryDependency(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      flags = (Flag) reader.ReadByte();
      annotations = reader.ReadList(r => r.ReadExpression());
      targetLibrary = new LibraryReference(reader);
      name = new StringReference(reader);
      combinators = reader.ReadList(r => new Combinator(r));
    }
  }

  public class LibraryPart
  {
    public readonly List<Expression> annotations;
    public readonly StringReference partUri;

    public LibraryPart(ComponentReader reader)
    {
      annotations = reader.ReadList(r => r.ReadExpression());
      partUri = new StringReference(reader);
    }
  }

  public class Typedef
  {
    public readonly CanonicalNameReference canonicalName;
    public readonly UriReference fileUri;
    public readonly FileOffset fileOffset;
    public readonly StringReference name;
    public readonly List<Expression> annotations;
    public readonly List<TypeParameter> typeParameters;
    public readonly DartType type;
    public readonly List<TypeParameter> typeParametersOfFunctionType;
    public readonly List<VariableDeclaration> positionalParameters;
    public readonly List<VariableDeclaration> namedParameters;

    public Typedef(ComponentReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
      fileUri = new UriReference(reader);
      fileOffset = new FileOffset(reader);
      name = new StringReference(reader);
      annotations = reader.ReadList(r => r.ReadExpression());
      typeParameters = reader.ReadList(r => new TypeParameter(r));
      type = reader.ReadDartType();
      typeParametersOfFunctionType = reader.ReadList(r => new TypeParameter(r));
      positionalParameters = reader.ReadList(r => new VariableDeclaration(r));
      namedParameters = reader.ReadList(r => new VariableDeclaration(r));
    }
  }

  public class Combinator
  {
    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      isShow = 0x1
    }

    public readonly List<StringReference> names;

    public Combinator(ComponentReader reader)
    {
      flags = (Flag) reader.ReadByte();
      names = reader.ReadList(r => new StringReference(r));
    }
  }

  public class LibraryDependencyReference
  {
    // Index into libraryDependencies in the enclosing Library.
    public readonly uint index;

    public LibraryDependencyReference(ComponentReader reader)
    {
      index = reader.ReadUint();
    }
  }

  public static class MemberExtensions
  {
    // todo: is this ever used ~ how are members injected?
    public static Member ReadMember(this ComponentReader reader)
    {
      var tag = reader.ReadByte();
      switch (tag)
      {
        case Constructor.Tag: return new Constructor(reader);
        case Field.Tag: return new Field(reader);
        case Procedure.Tag: return new Procedure(reader);
        case RedirectingFactoryConstructor.Tag: return new RedirectingFactoryConstructor(reader);
        default: throw new Exception($"{nameof(Member)}: unrecognized tag ({tag})");
      }
    }
  }



/*
enum AsyncMarker {
  Sync,
  SyncStar,
  Async,
  AsyncStar,
  SyncYielding
}
*/

  public class FunctionNode
  {
    public byte tag => Tag;
    public const byte Tag = 3;
    public readonly FileOffset fileOffset;
    public readonly FileOffset fileEndOffset;
    public readonly byte asyncMarker; // Index into AsyncMarker above.
    public readonly byte dartAsyncMarker; // Index into AsyncMarker above.
    public readonly List<TypeParameter> typeParameters;
    public readonly uint parameterCount; // positionalParameters.length + namedParameters.length.
    public readonly uint requiredParameterCount;
    public readonly List<VariableDeclaration> positionalParameters;
    public readonly List<VariableDeclaration> namedParameters;
    public readonly DartType returnType;
    public readonly Option<Statement> body;

    public FunctionNode(ComponentReader reader)
    {
      reader.CheckTag(Tag);

      fileOffset = new FileOffset(reader);
      fileEndOffset = new FileOffset(reader);
      asyncMarker = reader.ReadByte();
      dartAsyncMarker = reader.ReadByte();
      typeParameters = reader.ReadList(r => new TypeParameter(r));
      parameterCount = reader.ReadUint();
      requiredParameterCount = reader.ReadUint();
      positionalParameters = reader.ReadList(r => new VariableDeclaration(r));
      namedParameters = reader.ReadList(r => new VariableDeclaration(r));
      returnType = reader.ReadDartType();
      body = reader.ReadOption(r => r.ReadStatement());
    }
  }

  public class VariableReference
  {
    // Reference to the Nth variable in scope, with 0 being the
    // first variable declared in the outermost scope, and larger
    // numbers being the variables declared later in a given scope,
    // or in a more deeply nested scope.
    //
    // Function parameters are indexed from left to right and make
    // up the outermost scope (enclosing the function body).
    // Variables ARE NOT in scope inside their own initializer.
    // Variables ARE NOT in scope before their declaration, in contrast
    // to how the Dart Specification defines scoping.
    // Variables ARE in scope across function boundaries.
    //
    // When declared, a variable remains in scope until the end of the
    // immediately enclosing Block, Let, FunctionNode, ForStatement,
    // ForInStatement, or Catch.
    //
    // A special exception is made for constructor parameters, which are
    // also in scope in the initializer list, even though the tree nesting
    // is inconsistent with the scoping.
    public readonly uint stackIndex;

    public VariableReference(ComponentReader reader)
    {
      stackIndex = reader.ReadUint();
    }
  }


  public class Arguments
  {
    // Note: there is no tag on Arguments.
    public readonly uint numArguments; // equals positional.length + named.length
    public readonly List<DartType> types;
    public readonly List<Expression> positional;
    public readonly List<NamedExpression> named;

    public Arguments(ComponentReader reader)
    {
      numArguments = reader.ReadUint();
      types = reader.ReadList(r => r.ReadDartType());
      positional = reader.ReadList(r => r.ReadExpression());
      named = reader.ReadList(r => new NamedExpression(r));
      if (numArguments != positional.Count + named.Count)
      {
        throw new Exception($"{nameof(Arguments)}.{nameof(numArguments)}:{numArguments} != {nameof(positional)}.Count:{positional.Count} + {nameof(named)}.Count:{named.Count}");
      }
    }

    public ArgumentListSyntax ToArgumentListSyntax()
    {
      if (named.Count != 0) throw new Exception("named expressions not yet implemented");
      var args = new List<SyntaxNodeOrToken>();

      bool first = true;
      foreach (var expression in positional)
      {
        args.Add(SF.Argument(expression.ToExpressionSyntax()));

        if (first)
        {
          first = false;
        }
        else
        {
          // todo: do we need this? (_it seems weird_)
          args.Add(SF.Token(SyntaxKind.CommaToken));
        }
      }

      if (args.Count == 0)
      {
        throw new Exception("No argument list without an argument.");
      }

      if (args.Count == 1)
      {
        // todo: micro-optimize
        return SF.ArgumentList(SF.SingletonSeparatedList((ArgumentSyntax)args.First()));
      }

      return SF.ArgumentList(SF.SeparatedList<ArgumentSyntax>(args.ToArray()));
    }
  }

  public class MapEntry
  {
    // Note: there is no tag on MapEntry
    public readonly Expression key;
    public readonly Expression value;

    public MapEntry(ComponentReader reader)
    {
      key = reader.ReadExpression();
      value = reader.ReadExpression();
    }
  }

  public class VariableDeclaration
  {
    // The offset for the variable declaration, i.e. the offset of the start of
    // the declaration.
    public readonly FileOffset fileOffset;

    // The offset for the equal sign in the declaration (if it contains one).
    // If it does not contain one this should be -1.
    public readonly FileOffset fileEqualsOffset;

    public readonly List<Expression> annotations;

    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      isFinal = 0x1,
      isConst = 0x2,
      isFieldFormal = 0x4,
      isCovariant = 0x8,
      isInScope = 0x10,
      isGenericCovariantImpl = 0x20,
    }

    // For named parameters, this is the parameter name.
    // For other variables, the name is cosmetic, may be empty,
    // and is not necessarily unique.
    public readonly StringReference name;
    public readonly DartType type;

    // For statements and for-loops, this is the initial value.
    // For optional parameters, this is the default value (if given).
    // In all other contexts, it must be Nothing.
    public readonly Option<Expression> initializer;

    public VariableDeclaration(ComponentReader reader)
    {
      fileOffset = new FileOffset(reader);
      fileEqualsOffset = new FileOffset(reader);
      annotations = reader.ReadList(r => r.ReadExpression());
      flags = (Flag) reader.ReadByte();
      name = new StringReference(reader);
      type = reader.ReadDartType();
      initializer = reader.ReadOption(r => r.ReadExpression());
    }
  }
}