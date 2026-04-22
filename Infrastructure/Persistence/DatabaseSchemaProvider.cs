using System.IO;
namespace POS_system_cs.Infrastructure.Persistence;

public static class DatabaseSchemaProvider
{
    public static string SchemaFilePath => Path.Combine(AppContext.BaseDirectory, "Data", "schema.sql");
}

