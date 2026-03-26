using System.Data;

namespace SharpPortfolioBackend.Data;

public interface IDbConnectionFactory
{ 
    IDbConnection Create();
}