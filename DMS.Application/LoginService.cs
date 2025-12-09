using DMS.Application.Interface;
using DMS.Data.EF.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DMS.Application;

public class LoginService : ILoginService
{
    private readonly DmsContext _dbContext;

    public LoginService(DmsContext context)
    {
        _dbContext = context;
    }

    public (string Server, string Database) GetDbInfo()
    {
        var connectionString = _dbContext.Database.GetConnectionString();
        var builder = new SqlConnectionStringBuilder(connectionString);
        return (builder.DataSource, builder.InitialCatalog);
    }
}
