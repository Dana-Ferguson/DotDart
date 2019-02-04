using System;
using System.Collections.Generic;
using System.Linq;

namespace DotDart
{
  // todo: I'm a bit unsure about the groupings in this file.

  /*
  class CanonicalNameReference {
    UInt biasedIndex; // 0 if null, otherwise N+1 where N is index of parent
  }
  */
  public class CanonicalNameReference
  {
    public readonly uint biasedIndex;
    public readonly string value;

    public CanonicalNameReference(ComponentReader reader)
    {
      biasedIndex = reader.ReadUint();
      value = reader.GetString(this);
      // todo: remove ~ this is for checking
      // Console.WriteLine($"CNR: '{reader.GetString(this)}'");
    }

    [Testing]
    public CanonicalNameReference(string value)
    {
      this.value = value;
      biasedIndex = uint.MaxValue;
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

    public UriReference(ComponentReader reader)
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
    public readonly string value;

    public StringReference(ComponentReader reader)
    {
      index = reader.ReadUint();
      value = reader.GetString(this);
      // todo: remove ~ this is for checking
      Console.WriteLine($"SR: '{value}'");
    }

    /// <summary>
    /// Used for stuffing values into StringReference for testing
    /// </summary>
    public StringReference(string value)
    {
      index = uint.MaxValue;
      this.value = value;
    }

    public void Serialize(DartStringBuilder sb)
    {
      sb.Append($"({index}) => '{value}'");
    }

    public override string ToString()
    {
      return value;
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

    public StringTable(ComponentReader reader)
    {
      // todo: optimize!
      endOffsets = reader.ReadList((r) => (int) r.ReadUint());
      utf8Bytes = reader.ReadBytes(endOffsets.LastOrDefault());
      reader.StringTable = this;
    }
  }

  public class CanonicalName
  {
    public readonly CanonicalNameReference parent;
    public readonly StringReference name;

    public CanonicalName(ComponentReader reader)
    {
      parent = new CanonicalNameReference(reader);
      name = new StringReference(reader);
    }
  }

  public class LibraryReference
  {
    // Must be populated by a library (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public LibraryReference(ComponentReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class ClassReference
  {
    // Must be populated by a class (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public ClassReference(ComponentReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class MemberReference
  {
    // Must be populated by a member (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    // todo, annotate with allowNull
    public MemberReference(ComponentReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }

    public MemberReference(CanonicalNameReference canonicalName)
    {
      this.canonicalName = canonicalName;
    }

    [Testing]
    public MemberReference(string value)
    {
      this.canonicalName = new CanonicalNameReference(value);
    }
  }

  public class FieldReference
  {
    // Must be populated by a field (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public FieldReference(ComponentReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class ConstructorReference
  {
    // Must be populated by a constructor (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public ConstructorReference(ComponentReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }

  }

  public class ProcedureReference
  {
    // Must be populated by a procedure (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public ProcedureReference(ComponentReader reader)
    {
      canonicalName = new CanonicalNameReference(reader);
    }
  }

  public class TypedefReference
  {
    // Must be populated by a typedef (possibly later in the file).
    public readonly CanonicalNameReference canonicalName;

    public TypedefReference(ComponentReader reader)
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

    public Name(ComponentReader reader)
    {
      name = new StringReference(reader);
      // library = name.GetString().StartsWith('_') ? new LibraryReference(reader) : null;
      library = reader.GetString(name)?.StartsWith('_') ?? false ? new LibraryReference(reader) : null;
    }
  }
}