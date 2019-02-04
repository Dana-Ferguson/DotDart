using System;

namespace DotDart
{
  public static class InitializerExtensions
  {
    public static Initializer ReadInitializer(this ComponentReader reader)
    {
      var tag = reader.ReadByte();
      switch (tag)
      {
        case AssertInitializer.Tag: return new AssertInitializer(reader);
        case FieldInitializer.Tag: return new FieldInitializer(reader);
        case InvalidInitializer.Tag: return new InvalidInitializer(reader);
        case LocalInitializer.Tag: return new LocalInitializer(reader);
        case RedirectingInitializer.Tag: return new RedirectingInitializer(reader);
        case SuperInitializer.Tag: return new SuperInitializer(reader);
        default: throw new Exception($"{nameof(Initializer)}: unrecognized tag ({tag})");
      }
    }
  }

  public interface Initializer : Node
  {
  }

  public class InvalidInitializer : Initializer
  {
    public byte tag => Tag;
    public const byte Tag = 7;
    public readonly byte isSynthetic;

    public InvalidInitializer(ComponentReader reader)
    {
      isSynthetic = reader.ReadByte();
    }

    public InvalidInitializer(bool isSynthetic)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
    }
  }

  public class FieldInitializer : Initializer
  {
    public byte tag => Tag;
    public const byte Tag = 8;
    public readonly byte isSynthetic;
    public readonly FieldReference field;
    public readonly Expression value;

    public FieldInitializer(ComponentReader reader)
    {
      isSynthetic = reader.ReadByte();
      field = new FieldReference(reader);
      value = reader.ReadExpression();
    }

    public FieldInitializer(FieldReference field, Expression value, bool isSynthetic)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
      this.field = field;
      this.value = value;
    }

    public FieldInitializer(FieldReference field, Expression value)
    {
      this.field = field;
      this.value = value;
    }
  }

  public class SuperInitializer : Initializer
  {
    public byte tag => Tag;
    public const byte Tag = 9;
    public readonly byte isSynthetic;
    public readonly FileOffset fileOffset;
    public readonly ConstructorReference target;
    public readonly Arguments arguments;

    public SuperInitializer(ComponentReader reader)
    {
      isSynthetic = reader.ReadByte();
      fileOffset = new FileOffset(reader);
      target = new ConstructorReference(reader);
      arguments = new Arguments(reader);
    }

    public SuperInitializer(FileOffset fileOffset, ConstructorReference target, Arguments arguments, bool isSynthetic = false)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
      this.fileOffset = fileOffset;
      this.target = target;
      this.arguments = arguments;
    }

    [Testing]
    public SuperInitializer(ConstructorReference target, Arguments arguments, bool isSynthetic = false)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
      this.target = target;
      this.arguments = arguments;
    }
  }

  public class RedirectingInitializer : Initializer
  {
    public byte tag => Tag;
    public const byte Tag = 10;
    public readonly byte isSynthetic;
    public readonly FileOffset fileOffset;
    public readonly ConstructorReference target;
    public readonly Arguments arguments;

    public RedirectingInitializer(ComponentReader reader)
    {
      isSynthetic = reader.ReadByte();
      fileOffset = new FileOffset(reader);
      target = new ConstructorReference(reader);
      arguments = new Arguments(reader);
    }

    public RedirectingInitializer(FileOffset fileOffset, ConstructorReference target, Arguments arguments, bool isSynthetic = false)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
      this.fileOffset = fileOffset;
      this.target = target;
      this.arguments = arguments;
    }

    [Testing]
    public RedirectingInitializer(ConstructorReference target, Arguments arguments, bool isSynthetic = false)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
      this.target = target;
      this.arguments = arguments;
    }

  }

  public class LocalInitializer : Initializer
  {
    public byte tag => Tag;
    public const byte Tag = 11;
    public readonly byte isSynthetic;
    public readonly VariableDeclaration variable;

    public LocalInitializer(ComponentReader reader)
    {
      isSynthetic = reader.ReadByte();
      variable = new VariableDeclaration(reader);
    }

    public LocalInitializer(VariableDeclaration variable, bool isSynthetic = false)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
      this.variable = variable;
    }
  }

  public class AssertInitializer : Initializer
  {
    public byte tag => Tag;
    public const byte Tag = 12;
    public readonly byte isSynthetic;
    public readonly AssertStatement statement;

    public AssertInitializer(ComponentReader reader)
    {
      // #V12 - This is in the Binary.MD but not in the actual code.
      isSynthetic = reader.ReadByte();
      // statement = new AssertStatement(reader);
      statement = reader.ReadStatement() as AssertStatement;
    }

    public AssertInitializer(AssertStatement statement, bool isSynthetic = false)
    {
      this.isSynthetic = isSynthetic ? (byte)0 : (byte)1;
      this.statement = statement;
    }
  }
}