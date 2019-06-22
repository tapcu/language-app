using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace LanguageApp.src {
    class Synchronizator {
        private static readonly HttpClient client = new HttpClient();

        public static async Task sendRequestAsync(String jsonStr) {
            //var jsonStr = "{\"words\":[{\"id\": 8,  \"word\": \"tego nie spodziewałam\",  \"translation\": \"я не ожидала этого\",  \"correct_answers\": 0,  \"iteration\": 0,  \"next_show_date\": null,  \"last_update_date\": null}," +
            //    "{\"id\": 9,  \"word\": \"cios\",  \"translation\": \"удар\",  \"correct_answers\": 7,  \"iteration\": 3,  \"next_show_date\": \"2019-05-01 23:46:14\",  \"last_update_date\": \"2019-05-01 23:46:14\"}]}";

            var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");

            var response = await client.PutAsync("http://localhost:3000/sync", content);

            var responseString = await response.Content.ReadAsStringAsync();
        }
    }
}
