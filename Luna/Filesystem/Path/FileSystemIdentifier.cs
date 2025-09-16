using Luna.Generators;

namespace Luna;

/// <summary> An internal identifier </summary>
[StrongType<uint>(Flags: StrongTypeFlag.ComparableSelf
  | StrongTypeFlag.EquatableSelf
  | StrongTypeFlag.Incrementable
  | StrongTypeFlag.HasZero
  | StrongTypeFlag.AdditionBase)]
public readonly partial struct FileSystemIdentifier;
