﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data;

namespace LanguageApp.src {
    class DatabaseHandler {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        private SQLiteConnection dbConnection;
        private string DatabasePath { get; set; }

        public DatabaseHandler(String databaseName) {
            logger.Info("initializing database handler for the file: " + databaseName);

            this.DatabasePath = databaseName;
            createWordsTableIfNotExists();
        }

        //check if table "words" exists, if not, create it
        //if table already exists, try to add new column to it (last_update_date) - for backward compatibility
        private void createWordsTableIfNotExists() {
            string sql =
                "CREATE TABLE IF NOT EXISTS `words` (" +
                "`id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
                "`word`	TEXT NOT NULL UNIQUE," +
                "`translation`	TEXT," +
                "`correct_answers`	INTEGER DEFAULT 0," +
                "`iteration`	INTEGER DEFAULT -1," +
                "`next_show_date`	TEXT," +
                "`last_update_date`	TEXT); ";

            string alter_sql = "ALTER TABLE `words` ADD COLUMN last_update_date TEXT";

            try {
                openConnection();
                SQLiteCommand createSQL = new SQLiteCommand(sql, dbConnection);
                try {
                    createSQL.ExecuteNonQuery();
                } catch (Exception ex) {
                    logger.Error("Error while creating WORDS table in database");
                    logger.Error(ex);
                    throw new Exception(ex.Message);
                }

                //try to add column last_update_date, because on some databases it could be missed
                try {
                    SQLiteCommand alterSQL = new SQLiteCommand(alter_sql, dbConnection);
                    alterSQL.ExecuteNonQuery();
                } catch (SQLiteException ignored) {
                    logger.Debug("column LAST_UPDATE_DATE already exists in table WORDS");
                }

            } finally {
                closeConnection();
            }
        }

        private void openConnection() {
            try {
                if (DatabasePath == null || DatabasePath.Length == 0)
                    throw new Exception("Database name cannot be empty!");
                dbConnection = new SQLiteConnection("Data Source=" + DatabasePath + ";Version=3;");
                dbConnection.Open();
                logger.Trace("Database connection opened");
            } catch (Exception ex) {
                logger.Error("Error while open database connection");
                logger.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        private void closeConnection() {
            logger.Trace("Database connection closed");
            if (dbConnection!=null)
                dbConnection.Close();
        }

        public List<DictionaryItem> getAllWords() {
            logger.Trace("Get all awailable words from databae");

            string sql = "select * from words where iteration>=0";
            List<DictionaryItem> wordPairsList = new List<DictionaryItem>();

            try {
                openConnection();
                SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read()) {
                    int id = Convert.ToInt32(reader["id"]);
                    String word = reader["word"].ToString();
                    String translation = reader["translation"].ToString();
                    int correctAnswers = Convert.ToInt32(reader["correct_answers"]);
                    int iteration = Convert.ToInt32(reader["iteration"]);
                    String nextShowDate = reader["next_show_date"].ToString();
                    wordPairsList.Add(new DictionaryItem(id, word, translation, correctAnswers, iteration));
                }
            } finally {
                closeConnection();
            }

            return wordPairsList;
        }

        public List<DictionaryItem> getCurrentWords() {
            logger.Trace("Get current available words from database");

            string sql = "select * from words " +
                         "where next_show_date is null" +
                         " or strftime('%s',next_show_date) < strftime('%s','now', 'localtime') " +
                         "order by iteration desc, correct_answers " +
                         "limit " + Const.WORDS_NUMBER_LIMIT;

            List<DictionaryItem> wordPairsList = new List<DictionaryItem>();
            StringBuilder idsList = new StringBuilder();

            try {
                openConnection();
                SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read()) {
                    int id = Convert.ToInt32(reader["id"]);
                    String word = reader["word"].ToString();
                    String translation = reader["translation"].ToString();
                    int correctAnswers = Convert.ToInt32(reader["correct_answers"]);
                    int iteration = Convert.ToInt32(reader["iteration"]);
                    String nextShowDate = reader["next_show_date"].ToString();

                    if (iteration == -1) { //this should be updated to 0, it means we started to work on this word
                        idsList.Append(id).Append(",");
                        iteration = 0;
                    }

                    wordPairsList.Add(new DictionaryItem(id, word, translation, correctAnswers, iteration, nextShowDate));
                }

                if (idsList.Length > 0) {
                    idsList.Remove(idsList.Length - 1, 1); //remove last comma
                    UpdateIterationForIDs(idsList.ToString(), 0); //update iterations for words from list to 0
                }
            } catch (Exception ex) {
                logger.Error(ex, "Error while getting available words from database");
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }

            return wordPairsList;
        }

        /*
         * update iteration for the list of IDs
         */
        public void AddNewWord(String word, String translation) {
            string sql = "INSERT INTO words(word, translation) VALUES (@new_word, @new_translation)";

            if (word == null || translation == null)
                throw new Exception("word=" + word + ", translation=" + translation);
            if (word.Length<=0 || word.Length > 30)
                throw new Exception("incorrect word length: " + word.Length);
            if (translation.Length <= 0 || translation.Length > 30)
                throw new Exception("incorrect translation length: " + translation.Length);


            try {
                openConnection();
                SQLiteCommand insertSQL = new SQLiteCommand(sql, dbConnection);
                insertSQL.Parameters.AddWithValue("@new_word", word);
                insertSQL.Parameters.AddWithValue("@new_translation", translation);

                try {
                    insertSQL.ExecuteNonQuery();
                    logger.Debug(String.Format("Word {0} with translation {1} was added to database", word, translation));
                } catch (Exception ex) {
                    logger.Error(ex, "exception: " + ex.ToString());
                    String errMsg = ex.Message.Replace('\n', ' ').Replace('\r', ' '); //to correctly show few lines exception
                    if(errMsg.Contains("UNIQUE constraint failed")) {
                        errMsg = "Word '" + word + "' already contains in database";
                    }
                    throw new Exception(errMsg);
                }
            } finally {
                closeConnection();
            }
        }

        /*
         * update number of correct answers for word
         */
        public void UpdateCorrectAnswers(DictionaryItem dItem) {
            logger.Trace("Updating correct answers");
            
            string sql = "UPDATE words SET correct_answers = @answers WHERE id = @id";

            try {
                openConnection();

                SQLiteCommand updateSQL = new SQLiteCommand(sql, dbConnection);
                updateSQL.Parameters.AddWithValue("@answers", dItem.CorrectAnswers);
                updateSQL.Parameters.AddWithValue("@Id", dItem.Id);

                updateSQL.ExecuteNonQuery();

            } catch (Exception ex) {
                logger.Error(ex, "Error while updating correct answers in database for id " + dItem.Id);
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }
        }

        /*
         * update iteration for the list of IDs
         */
        public void UpdateIterationForIDs(String IDsList, int iteration) {
            logger.Trace("Updating iterations for list of IDs");

            string sql = "UPDATE words SET iteration = "+iteration+" WHERE id in ("+IDsList+")";
            
            try {
                openConnection();
                SQLiteCommand updateSQL = new SQLiteCommand(sql, dbConnection);
                //SQLiteCommand command = new SQLiteCommand(dbConnection);
                //command.CommandText = (sql);

                updateSQL.ExecuteNonQuery();

            } catch (Exception ex) {
                logger.Error(ex, "Error while update iterations in database for IDs list");
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }
        }

        /*
         * completely update info for given dictionary item in database 
         */
        public void updateWord(DictionaryItem item) {
            logger.Trace("updating one word");

            try {
                openConnection();
                SQLiteCommand updateSQL = new SQLiteCommand(
                    "UPDATE words SET " +
                    "word = @word, " +
                    "translation = @tran, " +
                    "correct_answers = @answ, " +
                    "iteration = @iter, " +
                    "next_show_date = @nextDate, " +
                    "last_update_date = @updateDate " +
                    "WHERE id = @id", dbConnection);

                updateSQL.Parameters.AddWithValue("@word", item.Word);
                updateSQL.Parameters.AddWithValue("@tran", item.Translation);
                updateSQL.Parameters.AddWithValue("@answ", item.CorrectAnswers);
                updateSQL.Parameters.AddWithValue("@iter", item.Iteration);
                updateSQL.Parameters.AddWithValue("@id", item.Id);

                DateTime nextDate = item.NextShowDate;
                if(!Object.Equals(nextDate, default(DateTime))) { //if date != null
                    string nextDateStr = nextDate.ToString("yyyy-MM-dd HH:mm:ss");
                    updateSQL.Parameters.AddWithValue("@nextDate", nextDateStr);
                } else {
                    updateSQL.Parameters.AddWithValue("@nextDate", null);
                }

                DateTime currentDate = DateTime.Now;
                string updateDateStr = currentDate.ToString("yyyy-MM-dd HH:mm:ss");
                updateSQL.Parameters.AddWithValue("@updateDate", updateDateStr);

                updateSQL.ExecuteNonQuery();

            } catch (Exception ex) {
                logger.Error(ex, "Error while updating one word in database with ID " + item.Id);
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }

        }

        public DataSet getAllDataAsDataSet() {
            logger.Trace("get all data from database as data set");

            string sql = "select * from words";

            DataSet allWordsDS = new DataSet();
            try {
                openConnection();
                SQLiteCommand getTables = new SQLiteCommand(sql, dbConnection);
                SQLiteDataAdapter myCountAdapter = new SQLiteDataAdapter(getTables);
                myCountAdapter.Fill(allWordsDS, "Words");
            } catch (Exception ex) {
                logger.Error(ex, "Error while getting all words from database as DataSet");
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }

            return allWordsDS;
        }

        public DataTable getAllDataAsDataTable() {
            logger.Trace("get all data from database as dataTable");

            string sql = "select * from words";

            DataTable dataTable = new DataTable();
            try {
                openConnection();
                SQLiteCommand getTables = new SQLiteCommand(sql, dbConnection);
                SQLiteDataAdapter myCountAdapter = new SQLiteDataAdapter(getTables);
                myCountAdapter.Fill(dataTable);
            } catch (Exception ex) {
                logger.Error(ex, "Error while getting all words from database as DataTable");
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }

            return dataTable;
        }

        public void UpdateTableFromDataTable(DataTable dt) {
            logger.Trace("Updating table from dataTable");

            string sql = "select * from words";

            try {
                openConnection();
                SQLiteCommand getTables = new SQLiteCommand(sql, dbConnection);
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(getTables);
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);
                adapter.Update(dt);
            } catch (Exception ex) {
                logger.Error(ex, "Error while updating database from DataTable");
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }

        }

        public void updateDatabasePath(String databasePath) {
            logger.Trace("update database path for db handler to " + databasePath);
            DatabasePath = databasePath;
        }
    }
}
