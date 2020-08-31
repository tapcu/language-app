# language-app
This program could be used for studying foreign languages.

Just run LanguageApp.exe, and if there is no database file, it will be created automatically. Otherwise, will be used database that is specified in config file.

The program is configured in the "settings" menu, or by changing the corresponding values in the config.json file.

---config variables:
ShowInterval - indicates how often (in seconds) the studied words will be displayed. The default is 3 minutes.
DaysInterval- The interval in days, after which the learned word will be shown again, to repeat it and consolidate. After the first repetition, the next time the word will be shown in DaysInterval*(iteration number) days. So the learned words will be shown less and less often. The default is 7 days.
DatabasePath - path to database file (so you could use few databases and switch between them). The default is empty, so program will use database with default name "WordsDatabase.db" located in the same folder.
IterationThreshold - not used :)
