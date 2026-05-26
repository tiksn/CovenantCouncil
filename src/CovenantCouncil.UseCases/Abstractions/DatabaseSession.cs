namespace CovenantCouncil.UseCases.Abstractions;

public sealed record DatabaseSession(
  string DatabasePath,
  bool IsOpen);
