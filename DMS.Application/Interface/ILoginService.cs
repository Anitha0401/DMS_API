namespace DMS.Application.Interface;

public interface ILoginService
{
    public (string Server, string Database) GetDbInfo();
}