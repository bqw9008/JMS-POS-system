namespace POS_system_cs.Application.Navigation;

public sealed record ModuleDefinition(
    string Key,
    string Title,
    string Description,
    string[] PrimaryActions);
