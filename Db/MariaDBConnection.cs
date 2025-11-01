using System;
using LMFS.Models;
using MySqlConnector;
using NLog;

namespace LMFS.Db
{
    public class MariaDBConnection
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static MySqlConnection connectDB()
        {
            try
            {
                string connString = $"host={DbConInfo.ip};port={DbConInfo.port};user id={DbConInfo.id};password={DbConInfo.password};database={DbConInfo.db};Allow User Variables=true;";
                var connection = new MySqlConnection(connString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }
    }
}