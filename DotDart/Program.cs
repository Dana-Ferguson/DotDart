using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming

// https://sharplab.io

namespace DotDart
{
  public class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Hello World!");

      var bytes = System.IO.File.ReadAllBytes("ddc_sdk.dill");
      Console.WriteLine(bytes.Length);

      /*foreach (var b in bytes.Take(16))
      {
          Console.WriteLine(b.ToString("X2"));
      }*/

      var reader = new DReader(bytes);
      // todo: handle this better
      // if we want to try and increase efficiency, we'd want a MMF
      // there is a lot of random (or reversed) access to DILL files (weird design decision?)
      // best just to load all bytes and conquer
      uint libraryCount = DReader.ToUint32(bytes.AsSpan(bytes.Length-8, 4));
      uint componentFileSizeInBytes = DReader.ToUint32(bytes.AsSpan(bytes.Length-4, 4));;
      var componentFile = new ComponentFile(reader, libraryCount, componentFileSizeInBytes);
      int x = 3;
    }
  }

  public class TagException : Exception
  {
    public TagException(byte actual, byte expected) :
      base($"Improper Tag: actual = {actual}; expected = {expected};")
    {
    }
  }

  public class DReader
  {
    private readonly byte[] _bytes;
    private readonly int _offset;
    private int _position = 0;

    private string[] _stringTable;
    // overly complicated, :'(
    private DReader _parent;

    public int Position
    {
      get => _position;
      set
      {
        _position = value;
        if (_position < 0) throw new Exception("Moved to before stream.");
        if (_position > Length) throw new Exception("Moved to after stream.");
      }
    }
    // direction for RLists?

    public int Length { get; private set; }

    public DReader(byte[] bytes)
    {
      _parent = this;
      _bytes = bytes;
      _offset = 0;
      Length = _bytes.Length;
    }

    public DReader(DReader reader, int offset)
    {
      _bytes = reader._bytes;
      _stringTable = reader._stringTable;
      _parent = reader._parent;
      _offset = offset;
      Length = _bytes.Length - offset;

      if (offset < 0) throw new Exception("Offset is too early.");
    }

    public DReader(DReader reader, int offset, int length)
    {
      _bytes = reader._bytes;
      _stringTable = reader._stringTable;
      _parent = reader._parent;
      _offset = offset;
      Length = length;

      if (offset < 0) throw new Exception("Offset is too early.");
      if (length > _bytes.Length - offset) throw new Exception("Window Length is too long.");
    }

    public DReader GetWindow(uint position) => GetWindow((int)position);
    public DReader GetWindow(uint position, uint length) => GetWindow((int)position, (int)length);

    public DReader GetWindow(int offset)
    {
      return new DReader(this, offset);
    }

    public DReader GetWindow(int offset, int length)
    {
      return new DReader(this, offset, length);
    }

    private void _Advance(int count = 1)
    {
      Position += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte _curr(int i = 0)
    {
      var j = _index(i);
      if (i + Position >= Length) throw  new Exception("Moved to after stream.");
      return _bytes[j];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int _index(int i = 0) => Position + _offset + i;

    public byte ReadByte()
    {
      var b = _curr(0);
      _Advance();
      return b;
    }

    // todo: evaluate usages and convert to Spans
    public byte[] ReadBytes(int count)
    {
      var bs = new byte[count];
      Array.Copy(_bytes, _index(), bs, 0, count);
      _Advance(count);
      return bs;
    }

    public uint ReadUint32() // => BitConverter.ToUInt32(_reader.ReadBytes(sizeof(uint)).Reverse().ToArray());
    {
      // var value = (uint) (_bytes[Position] | _bytes[Position+1] << 8 | _bytes[Position+2] << 16 | _bytes[Position+3] << 24);
      var value = (uint) (_curr(3) | _curr(2) << 8 | _curr(1) << 16 | _curr() << 24);
      _Advance(sizeof(uint));
      return value;
    }

    public void CheckTag(byte tag)
    {
      var actual = ReadByte();
      if (tag != actual) throw new TagException(actual, tag);
    }

    public static uint ToUint32(Span<byte> bytes) => BitConverter.ToUInt32(bytes.ToArray().Reverse().ToArray());

    public uint ReadUint()
    {
      var byte1 = ReadByte();
      if ((byte1 & 128) == 0)
      {
        // UInt7
        return byte1;
      }

      var byte2 = ReadByte();
      if ((byte1 & 64) == 0)
      {
        // UInt14
        return BitConverter.ToUInt32(new byte[] {byte2, (byte) (byte1 & 63), 0, 0});
      }

      // 26989
      var byte3 = ReadByte();
      var byte4 = ReadByte();

      /*
      var x = BitConverter.ToUInt32(new[] {byte4, byte3, byte2, (byte) (byte1 & 63)});
      if (x == 26989)
      {
        int y = 3;
      }

      return x;*/

      return BitConverter.ToUInt32(new[] {byte4, byte3, byte2, (byte) (byte1 & 63)});
    }

    public uint[] ReadUInts(int count)
    {
      var uints = new uint[count];

      for (int i = 0; i < count; i++)
      {
        uints[i] = ReadUint();
      }

      return uints;
    }

    public uint[] ReadUint32s(int count)
    {
      var uints = new uint[count];

      for (int i = 0; i < count; i++)
      {
        uints[i] = ReadUint32();
      }

      return uints;
    }


    public double ReadDouble() // => _reader.ReadDouble();
    {
      var v =  BitConverter.ToDouble(ReadBytes(8));
      return v;
    }

    public List<T> ReadList<T>(Func<DReader, T> reader)
    {
      var length = ReadUint();
      return ReadList(reader, length);
    }

    public List<T> ReadList<T>(Func<DReader, T> reader, uint length)
    {
      var list = new List<T>((int) length);
      for (int i = 0; i < length; i++)
      {
        list.Add(reader(this));
      }

      return list;
    }

    public StringTable StringTable
    {
      set
      {
        if (_stringTable != null) throw new Exception($"Attempted to overwrite a {nameof(StringTable)}.");
        var strings = new string[value.endOffsets.Count-1];

        // Console.WriteLine($"endOffsets :: {string.Join(", ", endOffsets)}");
        // Console.WriteLine($"utf8Bytes :: {Encoding.UTF8.GetString(utf8Bytes)}");
        int start = 0;
        for(int i = 1; i < value.endOffsets.Count; i++)
        {
          var end = value.endOffsets[i];
          var text = Encoding.UTF8.GetString(value.utf8Bytes, start, end - start);
          // Console.WriteLine($"{i} :: {text}");
          strings[i-1] = text;
          start = end;
        }

        _parent._stringTable = strings;
      }
    }

    public int WindowOffset => _offset;

    private string GetString(int i)
    {
      // if (i < 0 || i > _stringTable.Length) throw new Exception("String not found. Index = {i}; Length = {_stringTable.Length};");
      if (i < 0 || i > _stringTable.Length) return $"Index = {i}; Length = {_stringTable.Length};";
      return i == 0 ? null : _stringTable[i - 1];
    }

    public string GetString(CanonicalNameReference name)
    {
      return GetString(name.biasedIndex);
    }

    public string GetString(Name name)
    {
      if (name.library != null)
      {
        return GetString(name.library.canonicalName) + '.' + GetString(name.name.index);
      }
      return GetString(name.name.index);
    }


    public string GetString() => GetString((int)ReadUint());
    public string GetString(uint i) => i == 0 ? null : GetString((int) i);
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

  /*
      class ComponentFile {
        UInt32 magic = 0x90ABCDEF;
        UInt32 formatVersion = 16;
        Library[] libraries;
        UriSource sourceMap;
        List<CanonicalName> canonicalNames;
        MetadataPayload[] metadataPayloads;
        RList<MetadataMapping> metadataMappings;
        StringTable strings;
        List<Constant> constants;
        ComponentIndex componentIndex;
      }
   */

  public class ComponentFile
  {
    const uint Magic = 0x90ABCDEF;

    // formatVersion = 16;

    // formatVersion = 18;
    // https://github.com/dart-lang/sdk/blob/master/pkg/kernel/problems.md
    // https://github.com/dart-lang/sdk/blob/master/pkg/front_end/lib/src/fasta/fasta_codes.dart
    // List<String> problemsAsJson; // Described in problems.md.

    public readonly List<Library> libraries;
    public readonly UriSource sourceMap;
    public readonly List<CanonicalName> canonicalNames;
    public readonly List<MetadataPayload> metadataPayloads;
    public readonly List<MetadataMapping> metadataMappings;
    public readonly StringTable strings;
    public readonly List<Constant> constants;
    public readonly ComponentIndex componentIndex;

    public ComponentFile(DReader reader, uint libraryCount, uint componentFileSizeInBytes)
    {
      var magic = reader.ReadUint32();
      if (magic != Magic) throw new Exception($"{nameof(ComponentFile)} Magic should be {Magic} but was {magic}.");

      var formatVersion = reader.ReadUint32();
      if (formatVersion == 16)
      {
        //
      }
      else if (formatVersion >= 17)
      {
        // readSetOfStrings() in pkg/kernel/lib/binary/ast_from_binary.dart
        var problemsAsJson = reader.ReadList(r =>
          Encoding.UTF8.GetString(r.ReadList(s => s.ReadByte()).ToArray()));

        Console.WriteLine("We found these strings!");
        foreach (var json in problemsAsJson)
        {
          Console.WriteLine(json);
        }
      }
      else
      {
        Console.WriteLine($"Unknown ComponentFile formatVersion {formatVersion}");
      }

      var componentIndexOffset = componentFileSizeInBytes - (libraryCount + 10) * sizeof(uint);
      var componentIndexReader = reader.GetWindow(componentIndexOffset);
      componentIndex = new ComponentIndex(componentIndexReader, libraryCount, componentFileSizeInBytes);

      // this is called _stringTable in Kernel Source
      strings = new StringTable(reader.GetWindow(componentIndex.binaryOffsetForStringTable));

      // this is called _linkTable in Kernel Source
      canonicalNames = reader.GetWindow(componentIndex.binaryOffsetForCanonicalNames).ReadList(r => new CanonicalName(r));

      // TODO(alexmarkov): reverse metadata mappings and read forwards <-- yes, alex, this should be read forward
      metadataMappings = _readMetadataMappings(reader.GetWindow(componentIndex.binaryOffsetForMetadataMappings,
        componentIndex.binaryOffsetForStringTable - componentIndex.binaryOffsetForMetadataMappings));

      // _associateMetadata(component, _componentStartOffset);

      sourceMap = new UriSource(reader.GetWindow(componentIndex.binaryOffsetForSourceTable));
      constants = reader.GetWindow(componentIndex.binaryOffsetForConstantTable).ReadList(r => r.ReadConstant());

      // reader.Position = (int)componentIndex.libraryOffsets[1];
      // libraries = reader.ReadList(r => new Library(r), libraryCount);
      libraries = new List<Library>((int)componentIndex.libraryCount);
      for (int i = 0; i < libraryCount; i++)
      {
        var start = componentIndex.libraryOffsets[i];
        var end = componentIndex.libraryOffsets[i+1];
        libraries.Add(new Library(reader.GetWindow(start, end - start)));
      }

      // sourceMap = new UriSource(reader);
      // canonicalNames = reader.ReadList(r => new CanonicalName(r));
      int count = (int)(componentIndex.binaryOffsetForMetadataMappings - componentIndex.binaryOffsetForMetadataPayloads);
      // todo: this is all wrong!!!!
      metadataPayloads = reader.ReadList(r => new MetadataPayload(r, count)/*, componentIndex.*/);

      // *RLIST*
      // metadataMappings = reader.ReadList(r => new MetadataMapping(r));
      // strings = new StringTable(reader);
      // constants = reader.ReadList(r => r.ReadConstant());

      // componentIndex = new ComponentIndex(reader, componentIndexLibraryCount, componentIndexFileSizeInBytes);
    }

    private List<MetadataMapping> _readMetadataMappings(DReader reader)
    {
      var mappings = new List<MetadataMapping>();
      var uints = reader.ReadUint32s(reader.Length / sizeof(uint));

      var p = uints.Length - 1;
      var metadataMappings_Count = uints[p--];

      for (int i = 0; i < metadataMappings_Count; i++)
      {
        var nodeOffsetToMetadataOffset = new List<Pair<uint, uint>>();
        var rListCount = uints[p--];
        p -= (int)rListCount * 2;

        for (int j = 0; j < rListCount; j++)
        {
          // var nodeOffset = reader.ReadUint32();
          // var metadataOffset = binaryOffsetForMetadataPayloads + reader.ReadUint32();
          // mapping[(int)nodeOffset] = (int)metadataOffset;
          var pair = new Pair<uint, uint>(uints[p+j*2], uints[p+j*2+1]);
          nodeOffsetToMetadataOffset.Add(pair);
        }

        var tag = uints[p--];
        var mapping = new MetadataMapping(tag, nodeOffsetToMetadataOffset);
        mappings.Add(mapping);
      }

      return mappings;
    }

    /*
    private void _readMetadataMappings(DReader reader, int binaryOffsetForMetadataPayloads) {
      // At the beginning of this function _byteOffset points right past
      // metadataMappings to string table.

      // Read the length of metadataMappings.
      reader.Position -= 4;
      var subSectionCount = reader.ReadUint32();

      int endOffset = reader.Position - 4; // End offset of the current subsection.
      for (var i = 0; i < subSectionCount; i++) {
        // RList<Pair<UInt32, UInt32>> nodeOffsetToMetadataOffset
        reader.Position = endOffset - 4;
        var mappingLength = reader.ReadUint32();
        var mappingStart = (int)((endOffset - 4) - 4 * 2 * mappingLength);
        reader.Position = mappingStart - 4;

        // UInt32 tag (fixed size StringReference)
        var tag = _stringTable[reader.ReadUint32()];

        var repository = component.metadata[tag];
        if (repository != null) {
          // Read nodeOffsetToMetadataOffset mapping.
          var mapping = new Dictionary<int, int>();
          reader.Position = mappingStart;
          for (var j = 0; j < mappingLength; j++) {
            var nodeOffset = reader.ReadUint32();
            var metadataOffset = binaryOffsetForMetadataPayloads + reader.ReadUint32();
            mapping[(int)nodeOffset] = (int)metadataOffset;
          }

          _subsections = _subsections ?? new List<_MetadataSubsection>();
          _subsections.add(new _MetadataSubsection(repository, mapping));
        }

        // Start of the subsection and the end of the previous one.
        endOffset = mappingStart - 4;
      }
    }

    /// Deserialized MetadataMapping corresponding to the given metadata repository.
    class _MetadataSubsection {
      /// [MetadataRepository] that can read this subsection.
      readonly MetadataRepository repository;

      /// Deserialized mapping from node offsets to metadata offsets.
      readonly Dictionary<int, int> mapping;

      _MetadataSubsection(MetadataRepository repository, Dictionary<int, int> mapping)
      {
        this.repository = repository;
        this.mapping = mapping;
      }
    }*/
  }


  // Backend specific metadata section.
  public class MetadataPayload
  {
    public readonly byte[] opaquePayload;

    public MetadataPayload(DReader reader, int count)
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

    public MetadataMapping(DReader reader)
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
    public Library(DReader reader)
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
      Console.WriteLine($"{reader.GetString(name.index)} :: {classOffsets.Length} :: {classCount} || {procedureOffsets.Length} :: {procedureCount};");

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
      for (int i = 0; i < procedures.Count; i++)
      {
        var start = procedureOffsets[i];
        var end = procedureOffsets[i + 1];
        // procedures.Add(new Procedure(reader.GetWindow(start, end - start)));
      }
    }
  }

  /*
  class CanonicalNameReference {
    UInt biasedIndex; // 0 if null, otherwise N+1 where N is index of parent
  }
  */
  public class CanonicalNameReference
  {
    public readonly uint biasedIndex;

    public CanonicalNameReference(DReader reader)
    {
      biasedIndex = reader.ReadUint();
      // todo: remove ~ this is for checking
      Console.WriteLine($"* '{reader.GetString(this)}'");
    }
  }

  /*
  class UriReference {
    UInt index; // Index into the UriSource uris.
  }
  */
  public class UriReference
  {
    public readonly uint index;

    public UriReference(DReader reader)
    {
      index = reader.ReadUint();
    }
  }

  /*
  class StringReference {
    UInt index; // Index into the Component's strings.
  }
  */
  public class StringReference
  {
    public readonly uint index;

    public StringReference(DReader reader)
    {
      index = reader.ReadUint();
    }
  }

  /*
  class StringTable {
    List<UInt> endOffsets;
    Byte[endOffsets.last] utf8Bytes;
  }
  */
  public class StringTable
  {
    public readonly List<int> endOffsets;
    public readonly byte[] utf8Bytes;

    public StringTable(DReader reader)
    {
      // todo: optimize!
      endOffsets = reader.ReadList((r) => (int)r.ReadUint());
      utf8Bytes = reader.ReadBytes(endOffsets.LastOrDefault());
      reader.StringTable = this;
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

    public ConstantReference(DReader reader)
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

    public SourceInfo(DReader reader)
    {
      // todo: micro-optimize
      uriUtf8Bytes = reader.ReadList(r => r.ReadByte());
      sourceUtf8Bytes = reader.ReadList(r => r.ReadByte());
      lineStarts = reader.ReadList(r => r.ReadUint());
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

    public UriSource(DReader reader)
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

    public FileOffset(DReader reader)
    {
      fileOffset = reader.ReadUint() - 1;
    }
  }

  public static class OptionExtensions
  {
    public static Option<T> ReadOption<T>(this DReader reader, Func<DReader, T> valueReader)
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
  }

  // was Nothing : Option<T>, but that doesn't work in C#
  public class Nothing<T> : Option<T>
  {
    public byte tag => Tag;
    public const byte Tag = 0;
  }

  public class Something<T> : Option<T>
  {
    public byte tag => Tag;
    public const byte Tag = 1;
    public readonly T value;

    public Something(DReader reader, Func<DReader, T> valueReader)
    {
      value = valueReader(reader);
    }
  }

  public class CanonicalName
  {
    public readonly CanonicalNameReference parent;
    public readonly StringReference name;

    public CanonicalName(DReader reader)
    {
      parent = new CanonicalNameReference(reader);
      name = new StringReference(reader);
    }
  }

  // Component index with all fixed-size-32-bit integers.
  // This gives "semi-random-access" to certain parts of the binary.
  // By reading the last 4 bytes one knows the number of libaries,
  // which allows to skip to any other field in this component index,
  // which again allows to skip to what it points to.
  public class ComponentIndex
  {
    public readonly uint binaryOffsetForSourceTable;
    public readonly uint binaryOffsetForCanonicalNames;
    public readonly uint binaryOffsetForMetadataPayloads;
    public readonly uint binaryOffsetForMetadataMappings;
    public readonly uint binaryOffsetForStringTable;
    public readonly uint binaryOffsetForConstantTable;

    public readonly uint mainMethodReference; // This is a ProcedureReference with a fixed-size integer.

    // uint[libraryCount + 1] libraryOffsets;
    public readonly uint [] libraryOffsets;
    public readonly uint libraryCount;
    public readonly uint componentFileSizeInBytes;

    public ComponentIndex(DReader reader, uint libraryCount, uint componentFileSizeInBytes)
    {
      binaryOffsetForSourceTable = reader.ReadUint32();
      binaryOffsetForCanonicalNames = reader.ReadUint32();
      binaryOffsetForMetadataPayloads = reader.ReadUint32();
      binaryOffsetForMetadataMappings = reader.ReadUint32();
      binaryOffsetForStringTable = reader.ReadUint32();
      binaryOffsetForConstantTable = reader.ReadUint32();

      mainMethodReference = reader.ReadUint32();

      libraryOffsets = reader.ReadUint32s((int)libraryCount + 1);

      //Console.WriteLine($"- before: {libraryCount}");
      var checkCount = reader.ReadUint32();
      if (libraryCount != checkCount)
      {
        throw new Exception($"{nameof(libraryCount)} not as expected: {libraryCount} != {checkCount}");
      }
      //Console.WriteLine($"- after: {libraryCount}");

      //Console.WriteLine($"* before: {componentFileSizeInBytes}");
      var checkSize = reader.ReadUint32();
      if (componentFileSizeInBytes != checkSize)
      {
        throw new Exception($"{nameof(componentFileSizeInBytes)} not as expected: {componentFileSizeInBytes} != {checkSize}");
      }
      //Console.WriteLine($"* after: {componentFileSizeInBytes}");
    }
  }

  public class LibraryReference
  {
    // Must be populated by a library (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public LibraryReference(DReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class ClassReference
  {
    // Must be populated by a class (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public ClassReference(DReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class MemberReference
  {
    // Must be populated by a member (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    // todo, annotate with allowNull
    public MemberReference(DReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class FieldReference
  {
    // Must be populated by a field (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public FieldReference(DReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class ConstructorReference
  {
    // Must be populated by a constructor (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public ConstructorReference(DReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }

  }

  public class ProcedureReference
  {
    // Must be populated by a procedure (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public ProcedureReference(DReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class TypedefReference
  {
    // Must be populated by a typedef (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public TypedefReference(DReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

/*
type Name {
  StringReference name;
  if name begins with '_' {
    LibraryReference library;
  }
}
*/
  public class Name
  {
    public readonly StringReference name;

    // if name begins with '_'
    public readonly LibraryReference library;

    public Name(DReader reader)
    {
      name = new StringReference(reader);
      // library = name.GetString().StartsWith('_') ? new LibraryReference(reader) : null;
      library = reader.GetString(name.index)?.StartsWith('_') ?? false ? new LibraryReference(reader) : null;
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

    public LibraryDependency(DReader reader)
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

    public LibraryPart(DReader reader)
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

    public Typedef(DReader reader)
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

    public Combinator(DReader reader)
    {
      flags = (Flag) reader.ReadByte();
      names = reader.ReadList(r => new StringReference(r));
    }
  }

  public class LibraryDependencyReference
  {
    // Index into libraryDependencies in the enclosing Library.
    public readonly uint index;

    public LibraryDependencyReference(DReader reader)
    {
      index = reader.ReadUint();
    }
  }

  enum ClassLevel
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

    public Class(DReader reader)
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
  }

  public static class MemberExtensions
  {
    // todo: is this ever used ~ how are members injected?
    public static Member ReadMember(this DReader reader)
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

    public FunctionNode(DReader reader)
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

    public VariableReference(DReader reader)
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

    public Arguments(DReader reader)
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
  }

  public class NamedExpression
  {
    // Note: there is no tag on NamedExpression.
    public readonly StringReference name;
    public readonly Expression value;

    public NamedExpression(DReader reader)
    {
      name = new StringReference(reader);
      value = reader.ReadExpression();
    }
  }


  public class MapEntry
  {
    // Note: there is no tag on MapEntry
    public readonly Expression key;
    public readonly Expression value;

    public MapEntry(DReader reader)
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

    public VariableDeclaration(DReader reader)
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

  public class TypedefType
  {
    public byte tag => Tag;
    public const byte Tag = 87;
    public readonly TypedefReference typedefReference;
    public readonly List<DartType> typeArguments;

    public TypedefType(DReader reader)
    {
      typedefReference = new TypedefReference(reader);
      typeArguments = reader.ReadList(r => r.ReadDartType());
    }
  }

  public class TypeParameter
  {
    // Note: there is no tag on TypeParameter
    public readonly Flag flags;

    [Flags]
    public enum Flag : byte
    {
      isGenericCovariantImpl = 0x1,
    }

    public readonly List<Expression> annotations;
    public readonly StringReference name; // Cosmetic, may be empty, not unique.
    public readonly DartType bound; // 'dynamic' if no explicit bound was given.
    public readonly Option<DartType> defaultType; // type used when the parameter is not passed

    public TypeParameter(DReader reader)
    {
      flags = (Flag)reader.ReadByte();
      annotations =  reader.ReadList(r => r.ReadExpression());
      name = new StringReference(reader);
      bound = reader.ReadDartType();
      defaultType = reader.ReadOption(r => r.ReadDartType());
    }
  }
}