namespace DMS.Data.EF;

public class DBActions : IDBActions
{
    private readonly string connectionString;

    public DBActions(string defaultConnection)
    {
        connectionString = defaultConnection;
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException(connectionString, "Unable to find connection string in the settings");
        }
    }
}
public class DBConnectionString
{  
    public DBConnectionString(string _defaultConnection)
    {
        DefaultConnection = _defaultConnection;
    }
    public string DefaultConnection { get; set; }
}
