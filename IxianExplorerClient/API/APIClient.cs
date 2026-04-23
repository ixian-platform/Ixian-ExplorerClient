using IxianExplorerClient.Meta;
using IXICore;
using IXICore.Activity;
using IXICore.Meta;
using IXICore.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static IXICore.Transaction;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace IxianExplorerClient.API
{
    public class APIClient
    {

        public static string? mapUrlToExplorerAPI(Uri? requestUrl)
        {
            if (requestUrl == null) return null;

            string relativePath = requestUrl.AbsolutePath.TrimStart('/'); // "blocks/1234"
            string query = requestUrl.Query; // "?param=value"

            return $"{Config.explorerAPIBaseUrl}/{relativePath}{query}";
        }

        public static async Task<JsonResponse> forwardRequest(HttpListenerRequest originalRequest, string targetUrl)
        {
            JsonResponse response = new JsonResponse();

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpRequestMessage proxyRequest = new HttpRequestMessage
                    {
                        Method = new HttpMethod(originalRequest.HttpMethod),
                        RequestUri = new Uri(targetUrl)
                    };

                    proxyRequest.Headers.Add("API-KEY", Config.explorerAPIKey);
                    if (originalRequest.HasEntityBody)
                    {
                        using (StreamReader reader = new StreamReader(originalRequest.InputStream))
                        {
                            string content = await reader.ReadToEndAsync();
                            proxyRequest.Content = new StringContent(content, Encoding.UTF8, "application/json");
                        }
                    }

                    // Send the request to the Explorer API
                    HttpResponseMessage proxyResponse = await httpClient.SendAsync(proxyRequest);
                    string responseContent = await proxyResponse.Content.ReadAsStringAsync();

                    if (proxyResponse.IsSuccessStatusCode)
                    {
                        response.result = JsonConvert.DeserializeObject(responseContent)!;
                    }
                    else
                    {
                        response.error = new JsonError
                        {
                            code = (int)proxyResponse.StatusCode,
                            message = $"API Error: {responseContent}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                response.error = new JsonError
                {
                    code = 500,
                    message = $"Unexpected error: {ex.Message}"
                };
            }

            return response;
        }


        public static IxiNumber getAmountByAddressAsync(string address_string)
        {
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{address_string}");
                HttpResponseMessage response = httpClient.Send(request);

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    string amountString = root.GetProperty("amount").GetString()!;
                    IxiNumber amount = new(amountString);
                    return amount;
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching wallet balance for {address_string}: {ex.Message}");
                return 0;
            }
        }

        public static int getTransactionCountByAddressAsync(string address_string)
        {
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{address_string}");
                HttpResponseMessage response = httpClient.Send(request);

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    int txcount = root.GetProperty("txcount").GetInt32();
                    return txcount;
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching transaction count for {address_string}: {ex.Message}");
                return 0;
            }
        }

        private static void processTransaction(JsonElement transactionElement, string addressString)
        {
            try
            {
                ActivityType activity_type = ActivityType.None;

                string txid = transactionElement.GetProperty("txid").GetString()!;
                int type = int.Parse(transactionElement.GetProperty("type").GetString()!);
                string data = transactionElement.GetProperty("data").GetString()!;
                byte[] dataBytes = Crypto.stringToHash(data);
                string amount = transactionElement.GetProperty("amount").GetString()!;
                long timestamp = long.Parse(transactionElement.GetProperty("timestamp").GetString()!);
                ulong applied = transactionElement.GetProperty("applied").GetUInt64();
                int version = transactionElement.GetProperty("version").GetInt32();

                // Deserialize 'from'
                string fromRaw = transactionElement.GetProperty("from").GetString()!;
                IDictionary<byte[], IxiNumber> fromList = JsonSerializer.Deserialize<Dictionary<string, string>>(fromRaw)!
                    .ToDictionary(
                        kv => new Address(kv.Key).addressNoChecksum,
                        kv => new IxiNumber(kv.Value), new ByteArrayComparer()
                    );

                // Deserialize 'to'
                string toRaw = transactionElement.GetProperty("to").GetString()!;
                IDictionary<Address, ToEntry> toList = JsonSerializer.Deserialize<Dictionary<string, string>>(toRaw)!
                    .ToDictionary(
                        kv => new Address(kv.Key),
                        kv => new ToEntry(version, new IxiNumber(kv.Value)), new AddressComparer()
                    );
                string fee = transactionElement.GetProperty("fee").GetString()!;

                var tx = new Transaction(type)
                {
                    id = Transaction.txIdLegacyToV8(txid),
                    toList = toList,
                    fromList = fromList,
                    pubKey = new Address(fromList.First().Key),
                    blockHeight = ulong.Parse(txid.Split('-').First()),
                    applied = applied,
                    timeStamp = timestamp,
                    amount = amount,
                    fee = fee
                };

                IxianHandler.addTransactionToActivityStorage(Node.activityStorage, tx, applied, true);

                Logging.info($"{addressString}: {txid} added");
            }
            catch (Exception ex)
            {
                Logging.warn($"Failed to process transaction for address {addressString}: {ex.Message}");
            }
           
        }

        public static bool getTransactionsByAddressAsync(string addressString, int page = 1)
        {
            ulong upToBlockHeight = getLatestBlockHeight();
            // Wait 10 blocks in case of reorgs
            if (upToBlockHeight > 10)
            {
                upToBlockHeight -= 10;
            }
            else
            {
                return false;
            }

            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{addressString}/transactions?page={page}");
                HttpResponseMessage response = httpClient.Send(request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false; 
                }

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                Address address = new Address(addressString);

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Array)
                    {
                        Logging.warn($"Unexpected response format for address {addressString}");
                        return false;
                    }

                    foreach (JsonElement transactionElement in root.EnumerateArray())
                    {
                        if (transactionElement.GetProperty("applied").GetUInt64() >= upToBlockHeight)
                        {
                            continue;
                        }
                        processTransaction(transactionElement, addressString);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching transaction activity for {addressString}: {ex.Message}");
            }

            return false;
        }

        public static ulong getLatestBlockHeight()
        {
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/blocks/latest");
                HttpResponseMessage response = httpClient.Send(request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return 0;
                }

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        Logging.warn($"Unexpected response format for block");
                        return 0;
                    }

                    return root.GetProperty("id").GetUInt64();
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching latest block height: {ex.Message}");
            }

            return 0;
        }

        public static bool getTransactionUpdatesByAddressAsync(string addressString, string lastTxid)
        {
            ulong upToBlockHeight = getLatestBlockHeight();
            // Wait 10 blocks in case of reorgs
            if (upToBlockHeight > 10)
            {
                upToBlockHeight -= 10;
            } else
            {
                return false;
            }

            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{addressString}/updates?lastTx={lastTxid}");
                HttpResponseMessage response = httpClient.Send(request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                Address address = new Address(addressString);

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Array)
                    {
                        Logging.warn($"Unexpected response format for address {addressString}");
                        return false;
                    }

                    foreach (JsonElement transactionElement in root.EnumerateArray())
                    {
                        if (transactionElement.GetProperty("applied").GetUInt64() >= upToBlockHeight)
                        {
                            continue;
                        }
                        processTransaction(transactionElement, addressString);
                    }
                }
                Node.updateBalance(addressString);
                return true;
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching transaction activity for {addressString}: {ex.Message}");             
            }

            return false;
        }      
    }
}
