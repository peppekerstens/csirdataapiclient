//
//Used HttpClient for making HTTP requests.
//async and await are used to handle asynchronous requests.
//Error handling is done with exceptions.
//JSON serialization and deserialization is handled by System.Text.Json.
//The Python requests.Session() behavior is mapped to HttpClient, which persists across multiple requests.
//
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class IrDataClient
{
    private bool authenticated;
    private HttpClient client;
    private string baseUrl;
    private string username;
    private string encodedPassword; //hallo

    public IrDataClient(string username, string password)
    {
        authenticated = false;
        client = new HttpClient();
        baseUrl = "https://members-ng.iracing.com";
        this.username = username;
        encodedPassword = EncodePassword(username, password);
    }

    private string EncodePassword(string username, string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + username.ToLower()));
            return Convert.ToBase64String(hashBytes);
        }
    }

    public async Task<string> LoginAsync()
    {
        var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
        var data = new { email = username, password = encodedPassword };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await client.PostAsync($"{baseUrl}/auth", content);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine("Rate limited, waiting");
                if (response.Headers.Contains("x-ratelimit-reset"))
                {
                    string ratelimitReset = response.Headers.GetValues("x-ratelimit-reset").FirstOrDefault();
                    if (long.TryParse(ratelimitReset, out long resetTimestamp))
                    {
                        DateTime resetDatetime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).DateTime;
                        TimeSpan delta = resetDatetime - DateTime.Now;
                        if (delta.TotalSeconds > 0)
                            await Task.Delay(delta);
                    }
                }
                return await LoginAsync();
            }
            else if (response.IsSuccessStatusCode)
            {
                var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
                if (responseData.ContainsKey("authcode"))
                {
                    authenticated = true;
                    return "Logged in";
                }
            }
            else
            {
                throw new Exception($"Error from iRacing: {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (TaskCanceledException)
        {
            throw new Exception("Login timed out");
        }
        catch (HttpRequestException)
        {
            throw new Exception("Connection error");
        }

        return null;
    }

    private string BuildUrl(string endpoint)
    {
        return $"{baseUrl}{endpoint}";
    }

    private async Task<Dictionary<string, object>> GetResourceOrLinkAsync(string url, Dictionary<string, string> payload = null)
    {
        if (!authenticated)
        {
            await LoginAsync();
            return await GetResourceOrLinkAsync(url, payload);
        }

        var queryString = payload != null ? $"?{string.Join("&", payload.Select(kv => $"{kv.Key}={kv.Value}"))}" : string.Empty;
        HttpResponseMessage response = await client.GetAsync(url + queryString);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            authenticated = false;
            return await GetResourceOrLinkAsync(url, payload);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            Console.WriteLine("Rate limited, waiting");
            if (response.Headers.Contains("x-ratelimit-reset"))
            {
                string ratelimitReset = response.Headers.GetValues("x-ratelimit-reset").FirstOrDefault();
                if (long.TryParse(ratelimitReset, out long resetTimestamp))
                {
                    DateTime resetDatetime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).DateTime;
                    TimeSpan delta = resetDatetime - DateTime.Now;
                    if (delta.TotalSeconds > 0)
                        await Task.Delay(delta);
                }
            }
            return await GetResourceOrLinkAsync(url, payload);
        }
        else if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Unhandled Non-200 response: {response.StatusCode}");
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());

        if (!data.ContainsKey("link"))
        {
            return data;
        }
        else
        {
            string link = data["link"].ToString();
            return new Dictionary<string, object> { { "link", link } };
        }
    }

    public async Task<Dictionary<string, object>> GetResourceAsync(string endpoint, Dictionary<string, string> payload = null)
    {
        string requestUrl = BuildUrl(endpoint);
        Dictionary<string, object> resourceObj = await GetResourceOrLinkAsync(requestUrl, payload);

        if (resourceObj.ContainsKey("link"))
        {
            HttpResponseMessage response = await client.GetAsync(resourceObj["link"].ToString());

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                authenticated = false;
                return await GetResourceAsync(endpoint, payload);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.WriteLine("Rate limited, waiting");
                if (response.Headers.Contains("x-ratelimit-reset"))
                {
                    string ratelimitReset = response.Headers.GetValues("x-ratelimit-reset").FirstOrDefault();
                    if (long.TryParse(ratelimitReset, out long resetTimestamp))
                    {
                        DateTime resetDatetime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).DateTime;
                        TimeSpan delta = resetDatetime - DateTime.Now;
                        if (delta.TotalSeconds > 0)
                        {
                            await Task.Delay(delta);
                        }
                    }
                }
                return await GetResourceAsync(endpoint, payload);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Unhandled Non-200 response: {response.StatusCode}");
            }
            return resourceObj;
        }

        return resourceObj;
    }

    public async Task<List<Dictionary<string, object>>> GetCarsAsync()
    {
        var resourceObj = await GetResourceAsync("/data/car/get");

        // Controleer of de ontvangen data correct is
        if (resourceObj == null)
        {
            throw new Exception("Invalid data format, expected a list of cars.");
        }

        List<Dictionary<string, object>> listWithDic = new List<Dictionary<string, object>>();
        listWithDic.Add(resourceObj);

        if (listWithDic.Count > 1)
        {
            // More than 1 key in the list = chaos
        }
        else if (listWithDic.Count == 0)
        {
            // List is empty
        }
        else
        {
            foreach (Dictionary<string, object> kvp in listWithDic)
            {
                await getJson(kvp["link"].ToString(), "carlist");
            }
        }

        string testURL = @"data\\carlist.json";
        string jsonString = File.ReadAllText(testURL);
        List<string> jsonData = JsonSerializer.Deserialize<List<string>>(jsonString);

        Console.WriteLine(jsonString); //string jsonString = JsonSerializer.Serialize(yourObject);

        return listWithDic;
    }


    public async Task<List<Dictionary<string, object>>> GetTracksAsync()
    {
        var resourceObj = await GetResourceAsync("/data/track/get");
        // Expliciet deserialiseren naar een lijst van dictionaries
        return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(resourceObj.ToString());
    }


    public async Task getJson(string url, string filename)
    {
        // File path to save the downloaded file
        string filePath = @$"data\{filename}.json";

        // Create HttpClient
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Download file as a byte array
                byte[] fileBytes = await client.GetByteArrayAsync(url);

                // Save the file locally
                await File.WriteAllBytesAsync(filePath, fileBytes);

                Console.WriteLine("File downloaded and saved successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }

    public async Task<Dictionary<string, object>> GetUserIRatingAsync(string custId)
    {
        // Construct the endpoint for retrieving user iRating based on custId
        //var endpoint = $"/data/track/get";
        //var endpoint = $"/data/doc";
        //var endpoint = $"/data/stats/member/{custId}/career";
        //var endpoint = $"/data/driver_stats_by_category/sports_car";
        var endpoint = $"/data/member/chart_data/929520/2/1";
        Console.WriteLine(endpoint);

        // Get the resource from the endpoint
        var resourceObj = await GetResourceAsync(endpoint);

        // Check if the returned data is in the correct format
        if (resourceObj == null || !resourceObj.ContainsKey("iRating"))
        {
            throw new Exception("Invalid data format, expected iRating information.");
        }

        foreach (KeyValuePair<string, object> ele2 in resourceObj)
        {
            Console.WriteLine("{0} and {1}", ele2.Key, ele2.Value);
        }
        await getJson(BuildUrl(endpoint), "generalinfo");

        // Log the iRating value
        Console.WriteLine($"User iRating: {resourceObj["iRating"]}");

        return resourceObj;
    }

    public async Task<Dictionary<string, object>> GetAllCommands()
    {
        var endpoint = $"/data/doc";

        // Get the resource from the endpoint
        var resourceObj = await GetResourceAsync(endpoint);

        return resourceObj;
    }
}