using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotDart
{
  // todo: make this internal? (and by proxy make A LOT of constructors internal)
  public class ComponentReader
  {
    private readonly byte[] _bytes;
    private readonly int _offset;
    private int _position = 0;

    private string[] _stringTable;
    // overly complicated, :'(
    private ComponentReader _parent;

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

    public ComponentReader(byte[] bytes)
    {
      _parent = this;
      _bytes = bytes;
      _offset = 0;
      Length = _bytes.Length;
    }

    public ComponentReader(ComponentReader reader, int offset)
    {
      _bytes = reader._bytes;
      _stringTable = reader._stringTable;
      _parent = reader._parent;
      _offset = offset;
      Length = _bytes.Length - offset;

      if (offset < 0) throw new Exception("Offset is too early.");
    }

    public ComponentReader(ComponentReader reader, int offset, int length)
    {
      _bytes = reader._bytes;
      _stringTable = reader._stringTable;
      _parent = reader._parent;
      _offset = offset;
      Length = length;

      if (offset < 0) throw new Exception("Offset is too early.");
      if (length > _bytes.Length - offset) throw new Exception("Window Length is too long.");
    }

    public ComponentReader GetWindow(uint position) => GetWindow((int)position);
    public ComponentReader GetWindow(uint position, uint length) => GetWindow((int)position, (int)length);

    public ComponentReader GetWindow(int offset)
    {
      return new ComponentReader(this, offset);
    }

    public ComponentReader GetWindow(int offset, int length)
    {
      return new ComponentReader(this, offset, length);
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

    public List<T> ReadList<T>(Func<ComponentReader, T> reader)
    {
      var length = ReadUint();
      return ReadList(reader, length);
    }

    public List<T> ReadList<T>(Func<ComponentReader, T> reader, uint length)
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
        if (value.endOffsets.FirstOrDefault() != 0) throw new Exception($"{nameof(StringTable)} first offset was not 0!");
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

    public string GetString(StringReference stringReference) => GetString(stringReference.index);


    public string GetString() => GetString((int)ReadUint());
    private string GetString(uint i) => i == 0 ? null : GetString((int) i);
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

    public static ComponentFile Load(string filename)
    {
      var bytes = System.IO.File.ReadAllBytes(filename);
      return new ComponentFile(new ComponentReader(bytes));
    }

    public ComponentFile(ComponentReader reader)
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

      var backData = reader.GetWindow(reader.Length - 8);
      uint libraryCount = backData.ReadUint32();
      uint componentFileSizeInBytes = backData.ReadUint32();

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

    private List<MetadataMapping> _readMetadataMappings(ComponentReader reader)
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

    public ComponentIndex(ComponentReader reader, uint libraryCount, uint componentFileSizeInBytes)
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
}