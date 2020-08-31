using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

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
            if (dbConnection != null)
                dbConnection.Close();
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
                //string alter_sql = "ALTER TABLE `words` ADD COLUMN last_update_date TEXT";
                //try {
                //    SQLiteCommand alterSQL = new SQLiteCommand(alter_sql, dbConnection);
                //    alterSQL.ExecuteNonQuery();
                //} catch (SQLiteException ignored) {
                //    logger.Debug("column LAST_UPDATE_DATE already exists in table WORDS");
                //}

            } finally {
                closeConnection();
            }
        }


        /*------------------------------------------------------S-E-L-E-C-T--------------------------------------*/


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

        public String getAllDataAsJson() {
            logger.Trace("get all data from database as JSON");

            string sql = "select * from words";
            String resultJson;

            try {
                openConnection();
                SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();
                var r = Serialize(reader);

                resultJson = JsonConvert.SerializeObject(r, Formatting.Indented);
            } catch (Exception ex) {
                logger.Error(ex, "Error while getting all words from database as DataSet");
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }

            return resultJson;
        }

        private IEnumerable<Dictionary<string, object>> Serialize(SQLiteDataReader reader) {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            while (reader.Read())
                results.Add(SerializeRow(cols, reader));

            return results;
        }
        private Dictionary<string, object> SerializeRow(IEnumerable<string> cols,
                                                        SQLiteDataReader reader) {
            var result = new Dictionary<string, object>();
            foreach (var col in cols)
                result.Add(col, reader[col]);
            return result;
        }


        /*------------------------------------------------------U-P-D-A-T-E-------------------------------------------------*/


        /*
         * insert new word to database
         */
        public void AddNewWord(String word, String translation) {
            logger.Debug("adding new word:" + word + ", with translation:" + translation);
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
                    logger.Debug("executing query:" + insertSQL.CommandText);
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

        public int CheckTranslationExistence(String translation) {
            logger.Debug("Check occurence in database for translation " + translation);
            //string sql = "SELECT count(*) as num FROM words WHERE translation like '%"+translation+"%'";
            string sql = "SELECT translation FROM words WHERE translation like '%" + translation + "%'";

            int dublicatesNumber = 0;
            String regexPattern = @"^" + translation + "[0-9]*$";
            try {
                openConnection();
                SQLiteCommand command = new SQLiteCommand(sql, dbConnection);
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read()) {
                    //translationsNumber = Convert.ToInt32(reader["num"]);
                    String otherTranslation = reader["translation"].ToString();
                    logger.Debug("got possible duplicate: " + otherTranslation);
                    //replace all brackets and commas to whitespaces
                    otherTranslation = otherTranslation.Replace(',', ' ').Replace('(', ' ').Replace(')', ' ');
                    //split string by whitespaces
                    String[] words = otherTranslation.Split(' ');
                    foreach(String word in words) {
                        logger.Debug("checking translate duplicate for word:" + word);
                        //check every part of the string to be like translation (+ optional number)
                        if (Regex.IsMatch(word, regexPattern)) {
                            dublicatesNumber++;
                            logger.Debug("increase duplicates number:" + dublicatesNumber);
                        }
                    }
                }

                return dublicatesNumber;
            } finally {
                closeConnection();
                logger.Debug("connection for checking translations closed");
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

        /*
         * UPSERTS the world in database, if not exists - insert it, if exists - update, but only if last_update_date is bigger
         */
        public void upsertWord(DictionaryItem item) {
            logger.Trace("upserting one word, id: " + item.Id);

            try {
                openConnection();
                SQLiteCommand updateSQL = new SQLiteCommand(
                    "INSERT INTO words(id, word, translation, correct_answers, iteration, next_show_date, last_update_date) " +
                    "VALUES(@id,@word,@tran,@answ,@iter,@nextDate,@updateDate) " +
                    "ON CONFLICT(id) DO UPDATE SET " +
                    "word = excluded.word, " +
                    "translation = excluded.translation, " +
                    "correct_answers = excluded.correct_answers, " +
                    "iteration = excluded.iteration, " +
                    "next_show_date = excluded.next_show_date " +
                    "last_update_date = excluded.last_update_date " +
                    "WHERE (id = @id) and (last_update_date <= excluded.last_update_date) ", dbConnection);

                updateSQL.Parameters.AddWithValue("@word", item.Word);
                updateSQL.Parameters.AddWithValue("@tran", item.Translation);
                updateSQL.Parameters.AddWithValue("@answ", item.CorrectAnswers);
                updateSQL.Parameters.AddWithValue("@iter", item.Iteration);
                updateSQL.Parameters.AddWithValue("@id", item.Id);

                DateTime nextDate = item.NextShowDate;
                if (!Object.Equals(nextDate, default(DateTime))) { //if date != null
                    string nextDateStr = nextDate.ToString("yyyy-MM-dd HH:mm:ss");
                    updateSQL.Parameters.AddWithValue("@nextDate", nextDateStr);
                } else {
                    updateSQL.Parameters.AddWithValue("@nextDate", null);
                }

                DateTime lastUpdateDate = item.LastUpdateDate;
                if (!Object.Equals(lastUpdateDate, default(DateTime))) { //if date != null
                    string lastUpdateDateStr = lastUpdateDate.ToString("yyyy-MM-dd HH:mm:ss");
                    updateSQL.Parameters.AddWithValue("@updateDate", lastUpdateDateStr);
                } else {
                    updateSQL.Parameters.AddWithValue("@updateDate", null);
                }

                updateSQL.ExecuteNonQuery();

            } catch (Exception ex) {
                logger.Error(ex, "Error while upserting word with ID " + item.Id);
                throw new Exception(ex.Message);
            } finally {
                closeConnection();
            }
        }



        /*--------------------------------------------S-P-E-C-I-F-I-C---P-A-T-H-------------------------------*/


        private SQLiteConnection openConnection(String path) {
            try {
                if (path == null || path.Length == 0)
                    throw new Exception("Database name cannot be empty!");
                SQLiteConnection connection = new SQLiteConnection("Data Source=" + path + ";Version=3;");
                connection.Open();
                SQLiteCommand createSQL = new SQLiteCommand("pragma synchronous = off;", connection);
                createSQL.ExecuteNonQuery();
                logger.Trace("Database connection opened");
                return connection;
            } catch (Exception ex) {
                logger.Error("Error while open database connection");
                logger.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        private void closeConnection(SQLiteConnection connection) {
            logger.Trace("Database connection closed");
            if (connection != null)
                connection.Close();
        }

        //drop table if exists
        private void dropWordsTableIfExists(SQLiteConnection connection) {
            string sql = "DROP TABLE IF EXISTS `words`";

            SQLiteCommand createSQL = new SQLiteCommand(sql, connection);
            try {
                createSQL.ExecuteNonQuery();
            } catch (Exception ex) {
                logger.Error("Error while drop WORDS table in database");
                logger.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        //check if table "words" exists, if not, create it
        //if table already exists, try to add new column to it (last_update_date) - for backward compatibility
        private void createWordsTableIfNotExists(SQLiteConnection connection) {
            string sql =
                "CREATE TABLE IF NOT EXISTS `words` (" +
                "`id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT," +
                "`word`	TEXT NOT NULL UNIQUE," +
                "`translation`	TEXT," +
                "`correct_answers`	INTEGER DEFAULT 0," +
                "`iteration`	INTEGER DEFAULT -1," +
                "`next_show_date`	TEXT," +
                "`last_update_date`	TEXT); ";

            SQLiteCommand createSQL = new SQLiteCommand(sql, connection);
            try {
                createSQL.ExecuteNonQuery();
            } catch (Exception ex) {
                logger.Error("Error while creating WORDS table in database");
                logger.Error(ex);
                throw new Exception(ex.Message);
            }
        }

        /*
         * insert new word into table
         */
        private void insertWord(SQLiteConnection connection, DictionaryItem item) {
            //logger.Debug("inserting one word, id: " + item.Id);
            DateTime dt = DateTime.Now;
            try {
                SQLiteCommand updateSQL = new SQLiteCommand(
                    "INSERT INTO words(id, word, translation, correct_answers, iteration, next_show_date, last_update_date) " +
                    "VALUES(@id,@word,@tran,@answ,@iter,@nextDate,@updateDate) ", connection);

                updateSQL.Parameters.AddWithValue("@id", item.Id);
                updateSQL.Parameters.AddWithValue("@word", item.Word);
                updateSQL.Parameters.AddWithValue("@tran", item.Translation);
                updateSQL.Parameters.AddWithValue("@answ", item.CorrectAnswers);
                updateSQL.Parameters.AddWithValue("@iter", item.Iteration);

                DateTime nextDate = item.NextShowDate;
                if (!Object.Equals(nextDate, default(DateTime))) { //if date != null
                    string nextDateStr = nextDate.ToString("yyyy-MM-dd HH:mm:ss");
                    updateSQL.Parameters.AddWithValue("@nextDate", nextDateStr);
                } else {
                    updateSQL.Parameters.AddWithValue("@nextDate", null);
                }

                DateTime lastUpdateDate = item.LastUpdateDate;
                if (!Object.Equals(lastUpdateDate, default(DateTime))) { //if date != null
                    string lastUpdateDateStr = lastUpdateDate.ToString("yyyy-MM-dd HH:mm:ss");
                    updateSQL.Parameters.AddWithValue("@updateDate", lastUpdateDateStr);
                } else {
                    updateSQL.Parameters.AddWithValue("@updateDate", null);
                }

                updateSQL.ExecuteNonQuery();
                TimeSpan ts = DateTime.Now - dt;
                Console.WriteLine("inserting took: " + ts);

            } catch (Exception ex) {
                logger.Error(ex, "Error while inserting word with ID " + item.Id);
                throw new Exception(ex.Message);
            }
        }

        /*
         * insert new word into table
         */
        private void insertWord2(SQLiteConnection connection, string jsonStr) {
            DateTime dt = DateTime.Now;
            try {
                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "INSERT INTO words(id, word, translation, correct_answers, iteration, next_show_date, last_update_date) " +
                        "VALUES($id,$word,$tran,$answ,$iter,$nextDate,$updateDate);";

                    var idParameter = command.CreateParameter();
                    idParameter.ParameterName = "$id";
                    command.Parameters.Add(idParameter);

                    var wordParameter = command.CreateParameter();
                    wordParameter.ParameterName = "$word";
                    command.Parameters.Add(wordParameter);

                    var tranParameter = command.CreateParameter();
                    tranParameter.ParameterName = "$tran";
                    command.Parameters.Add(tranParameter);

                    var answParameter = command.CreateParameter();
                    answParameter.ParameterName = "$answ";
                    command.Parameters.Add(answParameter);

                    var iterParameter = command.CreateParameter();
                    iterParameter.ParameterName = "$iter";
                    command.Parameters.Add(iterParameter);

                    var nextDateParameter = command.CreateParameter();
                    nextDateParameter.ParameterName = "$nextDate";
                    command.Parameters.Add(nextDateParameter);

                    var updateDateParameter = command.CreateParameter();
                    updateDateParameter.ParameterName = "$updateDate";
                    command.Parameters.Add(updateDateParameter);

                    JArray parserdJsonArr = JArray.Parse(jsonStr);
                    foreach (JObject wordItem in parserdJsonArr.Children<JObject>()) {
                        idParameter.Value = (int)wordItem["id"];
                        wordParameter.Value = (String)wordItem["word"];
                        tranParameter.Value = (String)wordItem["translation"];
                        answParameter.Value = (int)wordItem["correct_answers"];
                        iterParameter.Value = (int)wordItem["iteration"];

                        if (wordItem["next_show_date"] != null)
                            nextDateParameter.Value = (String)wordItem["next_show_date"];
                        else nextDateParameter.Value = DBNull.Value;

                        if (wordItem["last_update_date"] != null)
                            updateDateParameter.Value = (String)wordItem["last_update_date"];
                        else updateDateParameter.Value = DBNull.Value;

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    TimeSpan ts = DateTime.Now - dt;
                    Console.WriteLine("inserting took: " + ts);
                }
            } catch (Exception ex) {
                logger.Error(ex, "Error while inserting words from json");
                throw new Exception(ex.Message);
            }
        }


        public void createDatabaseFileBasedOnJson(String jsonStr, String path) {
            DateTime dt = DateTime.Now;
            logger.Info("Start to create database file " + path + " based on json");

            SQLiteConnection connection = openConnection(path);
            try {
                dropWordsTableIfExists(connection);
                createWordsTableIfNotExists(connection);
                insertWord2(connection, jsonStr);
                //JArray parserdJsonArr = JArray.Parse(jsonStr);
                //foreach (JObject wordItem in parserdJsonArr.Children<JObject>()) {
                //    int id = (int)wordItem["id"];
                //    string word = (string)wordItem["word"];
                //    string translation = (string)wordItem["translation"];
                //    int correct_answers = (int)wordItem["correct_answers"];
                //    int iteration = (int)wordItem["iteration"];
                //    string next_show_date = (string)wordItem["next_show_date"];
                //    string last_update_date = (string)wordItem["last_update_date"];
                //    DictionaryItem item = new DictionaryItem(id, word, translation, correct_answers, iteration, next_show_date, last_update_date);
                //    insertWord(connection, item);
                //}
                logger.Info("Finish creation of database file at path: " + path);
            } finally {
                closeConnection(connection);
                TimeSpan ts = DateTime.Now - dt;
                Console.WriteLine("database creation took: " + ts);
            }
        }

        /*
         * temporary function? I just need to be sure, that database path is actual. It can be changed in config.
         */
        public void updateDatabasePath(String databasePath) {
            if (!databasePath.Equals(DatabasePath)) {
                logger.Trace("update database path for db handler to " + databasePath);
                DatabasePath = databasePath;
            }
        }
    }
}
