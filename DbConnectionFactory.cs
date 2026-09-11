using Microsoft.Data.SqlClient;

namespace OutBoxPattern
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration configuration;

        public DbConnectionFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public SqlConnection CreateConnection()
        {
            var connectionString = configuration.GetConnectionString("Default");

            return new SqlConnection(connectionString);
        }
    }
}
