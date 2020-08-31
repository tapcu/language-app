using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows;

namespace LanguageApp.src {
    class Synchronizator {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly HttpClient client = new HttpClient();

        public static async Task sendRequestAsync(String jsonStr) {
            String serverUrl = Config.getInstance().ServerUrl;

            if (serverUrl != null && serverUrl.Length > 0) {
                logger.Info("sending data to url: " + serverUrl);

                var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(serverUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();
                logger.Info("Got response from server: " + responseString);
                //MessageBox.Show(responseString,"Server response",MessageBoxButton.OK);
            } else {
                logger.Error("failed to send data to server, url is null. Please provide serverUrl value in config file.");
            }
        }

        public static async Task<String> getJsonAsync() {
            String serverUrl = Config.getInstance().ServerUrl;

            if (serverUrl != null && serverUrl.Length > 0) {
                logger.Info("getting data from url: " + serverUrl);
                var response = await client.GetAsync(serverUrl);

                String responseString = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Got data from server", "Server response", MessageBoxButton.OK);
                return responseString;
            } else {
                logger.Error("failed to send data to server, url is null. Please provide serverUrl value in config file.");
            }
            return null;
        }
    }
}
