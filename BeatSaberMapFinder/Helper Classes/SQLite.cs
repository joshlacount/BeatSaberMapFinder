using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMapFinder
{
    public static class SQLite
    {
        private static SQLiteConnection sqlite;

        static SQLite()
        {
            string dbPath = Directory.GetCurrentDirectory() + @"\data.db";
            sqlite = new SQLiteConnection("Data Source=" + dbPath);
        }

        public static int ExecuteNonQuery(string cmdText)
        {
            int returnInt;

            try
            {
                SQLiteCommand cmd;
                sqlite.Open();
                cmd = sqlite.CreateCommand();
                cmd.CommandText = cmdText;
                int rows = cmd.ExecuteNonQuery();
                sqlite.Close();

                returnInt = rows;
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.Message + ": " + e.StackTrace);
                returnInt = 0;
            }

            return returnInt;
        }

        public static int ExecuteNonQuery(string cmdText, Dictionary<SQLiteParameter, object> paramDict)
        {
            int returnInt;

            try
            {
                SQLiteCommand cmd;
                sqlite.Open();
                cmd = sqlite.CreateCommand();
                cmd.CommandText = cmdText;
                cmd.Parameters.Add(new SQLiteParameter());
                foreach (var kvp in paramDict)
                {
                    cmd.Parameters.Add(kvp.Key);
                    cmd.Parameters[kvp.Key.ParameterName].Value = kvp.Value;
                }

                int rows = cmd.ExecuteNonQuery();
                sqlite.Close();
                returnInt = rows;
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.Message + ": " + e.StackTrace);
                returnInt = 0;
            }

            return returnInt;
        }

        public static DataTable ExecuteQuery(string cmdText)
        {
            SQLiteDataAdapter ad;
            DataTable dt = new DataTable();

            try
            {
                SQLiteCommand cmd;
                sqlite.Open();
                cmd = sqlite.CreateCommand();
                cmd.CommandText = cmdText;
                ad = new SQLiteDataAdapter(cmd);
                ad.Fill(dt);
                sqlite.Close();
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.Message + ": " + e.StackTrace);
            }

            return dt;
        }

        public static DataTable ExecuteQuery(string cmdText, Dictionary<string, object> paramDict)
        {
            SQLiteDataAdapter ad;
            DataTable dt = new DataTable();

            try
            {
                SQLiteCommand cmd;
                sqlite.Open();
                cmd = sqlite.CreateCommand();
                cmd.CommandText = cmdText;

                foreach (var kvp in paramDict)
                    cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);

                ad = new SQLiteDataAdapter(cmd);
                ad.Fill(dt);
                sqlite.Close();
            }
            catch (SQLiteException e)
            {
                Console.WriteLine(e.Message + ": " + e.StackTrace);
            }

            return dt;
        }
    }
}
