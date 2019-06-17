using System;
using System.Data.SQLite;
using System.IO;


namespace ConsoleApp4
{
    class Donnees
    {

        private object _lock = new object();

        public string _connectionString;
        public virtual string ConnectionString
        {
            set { _connectionString = value; }
            get { return _connectionString; }
        }

        public void InitializeDatabase(string connectionString)
        {
            Debug.AssertStringNotEmpty(connectionString);
            ConnectionString = connectionString;

            string dbFilePath = connectionString;

            if (File.Exists(dbFilePath))
                return;

            //
            // Make sure that we don't have multiple threads all trying to create the database
            //

            lock (_lock)
            {
                //
                // Just double check that no other thread has created the database while
                // we were waiting for the lock
                //

                if (File.Exists(dbFilePath))
                    return;

                SQLiteConnection.CreateFile(dbFilePath);

                const string sql = @"
                CREATE TABLE Fichier (
                    Idt INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    FullName TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Size INTEGER NOT NULL,
                    DteMdf TEXT NOT NULL,
                    hashCode TEXT NOT NULL
                )";

                using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + connectionString))
                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public Boolean GetRow(FileInfo pFile)
        {
            Boolean found = false;
            lock (_lock)
            {
                const string sql = @"
                select count(*) NBR from Fichier
                where FullName = '{0}'";

                string req = string.Format(sql, pFile.FullName.Replace("'", "''"));

                using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + ConnectionString))
                using (SQLiteCommand command = new SQLiteCommand(req, connection))
                {
                    connection.Open();
                    SQLiteDataReader rdr = command.ExecuteReader();
                    
                    while(rdr.Read())
                    {
                        if(Convert.ToInt32(rdr["NBR"]) == 1)
                        {
                            found = true;
                        }
                    }
                }
            }
            return found;
        }

        public void InsertRow(FileInfo pFile, string pHashCode)
        {

            lock (_lock)
            {
                //
                // Just double check that no other thread has created the database while
                // we were waiting for the lock
                //

                const string sql = @"
                INSERT INTO Fichier
                ( FullName, Name, Size, DteMdf, hashCode
                ) VALUES
                ( '{0}', '{1}', {2}, '{3}', '{4}'
                )";

                string req = string.Format(sql, pFile.FullName.Replace("'", "''"), pFile.Name.Replace("'", "''"), pFile.Length, pFile.LastWriteTimeUtc.ToString(), pHashCode);

                using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + ConnectionString))
                using (SQLiteCommand command = new SQLiteCommand(req, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
