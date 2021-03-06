﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageApp.src {
    public class DictionaryItem {
        private int id;
        private string word;
        private string translation;
        private int correctAnswers;
        private int iteration;
        private DateTime nextShowDate;
        private DateTime lastUpdateDate;

        public DictionaryItem(int id, string word, string translation, int correctAnswers = 0, int iteration = 0, String nextDate = null, String lastUpdateDate = null) {
            this.id = id;
            this.word = word;
            this.translation = translation;
            this.correctAnswers = correctAnswers;
            this.iteration = iteration;
            if (nextDate!= null && nextDate != "")
                try {
                    this.nextShowDate = DateTime.Parse(nextDate);
                } catch (Exception ex) {
                    Console.WriteLine("Error with parsing date: " + nextDate + ": " + ex.Message);
                }
            if (lastUpdateDate != null && lastUpdateDate != "")
                try {
                    this.lastUpdateDate = DateTime.Parse(lastUpdateDate);
                } catch (Exception ex) {
                    Console.WriteLine("Error with parsing last update date: " + lastUpdateDate + ": " + ex.Message);
                }
        }

        //getters and setters
        public int Id { get => id; set => id = value; }
        public string Word { get => word; set => word = value; }
        public string Translation { get => translation; set => translation = value; }
        public int CorrectAnswers { get => correctAnswers; set => correctAnswers = value; }
        public int Iteration { get => iteration; set => iteration = value; }
        public DateTime NextShowDate { get => nextShowDate; set => nextShowDate = value; }
        public DateTime LastUpdateDate { get => lastUpdateDate; set => lastUpdateDate = value; }

        public String toString() {
            return "id: " + this.id + ", " +
                "word: " + this.word + ", " +
                "translation: " + this.translation + ", " +
                "correct answers: " + this.correctAnswers + ", " +
                "iteration: " + this.iteration;
        }
    }
}
