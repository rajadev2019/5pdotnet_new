using _5PaisaLibrary;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.Design;

#region Tried with Rest API

//HttpResponseMessage resp = await NewMethod1(totp);
//string requestCode = await resp.Content.ReadAsStringAsync();
//
////Console.WriteLine(await response.Content.ReadAsStringAsync());
//
//await NewMethod();
//
//static async Task NewMethod()
//{
//    var client = new HttpClient();
//    var request = new HttpRequestMessage(HttpMethod.Post, "https://Openapi.5paisa.com/VendorsAPI/Service1.svc/GetAccessToken");
//    request.Headers.Add("Cookie", "5paisacookie=d01sqy4g5u4wempadmintdpq");
//    var content = new StringContent("{\n    \"head\": {\n        \"Key\": \"cXtggTed4IB4Y7zzkPRCjDKCwovTQbq4s\"\n    },\n    \"body\": {\n        \"RequestToken\": \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IjUzMTMyNTY5Iiwicm9sZSI6ImNYdGdnVGVkNElCNFk3enprUFJDakRLQ3dvdlRRYnE0IiwiU3RhdGUiOiIiLCJuYmYiOjE3Mzc2NDMwMDQsImV4cCI6MTczNzY0NjYwNCwiaWF0IjoxNzM3NjQzMDA0fQ.UOmovhKMEwPPQl9Z0HfpUVw7PqpBZGHRMSLp-OU-nmU\",\n        \"EncryKey\": \"e8CuHBkKiHzYB6N1RixN8f6w7vFrLQaQ\",\n        \"UserId\": \"9830182496\"\n    }\n}", null, "application/json");
//    request.Content = content;
//    var response = await client.SendAsync(request);
//    response.EnsureSuccessStatusCode();
//    Console.WriteLine(await response.Content.ReadAsStringAsync());
//}
//
//static async Task<HttpResponseMessage> NewMethod1(string totp)
//{
//    var client = new HttpClient();
//    var request = new HttpRequestMessage(HttpMethod.Post, "https://Openapi.5paisa.com/VendorsAPI/Service1.svc/TOTPLogin");
//    var content = new StringContent("{\n    \"head\": {\n        \"Key\": \"cXtggTed4IB4Y7zzkPRCjDKCwovTQbq4\"\n    },\n    \"body\": {\n        \"Email_ID\": \"9830182496\",\n        \"TOTP\": \"" + totp + "\",\n        \"PIN\": \"110888\"\n    }\n}", null, "application/json");
//    request.Content = content;
//    var response = await client.SendAsync(request);
//    response.EnsureSuccessStatusCode();
//    return response;
//}
#endregion

var apiKey = "cXtggTed4IB4Y7zzkPRCjDKCwovTQbq4";
try
{
    _5PaisaAPI paisaAPI = new _5PaisaAPI(apiKey: apiKey, encryptionKey: "e8CuHBkKiHzYB6N1RixN8f6w7vFrLQaQ", encryptUserId: "fdcomFzjStL");

    paisaAPI.SetAccessToken(GetAccessToken(paisaAPI)); // Set Access Token should be an extension method and has to check null

    var histResponse = paisaAPI.historical("N", "C", 2885, "1d", DateTime.Now.AddDays(-5), DateTime.Now.AddDays(-1));
    Console.WriteLine(histResponse.HistoricalData.ToString());
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

static Token GetAccessToken(_5PaisaAPI paisaAPI)
{
    string filePath = "token.json"; // Path to your JSON file

    if (!File.Exists(filePath))
    {
        Console.WriteLine("File not found!");
        return GenerateAccessToken(paisaAPI);
    }
    else
    {
        return ReadTokenFromFile(filePath) ?? GenerateAccessToken(paisaAPI);
    }
}

static Token ReadTokenFromFile(string filePath)
{
    string jsonString = File.ReadAllText(filePath);

    // Deserialize JSON to an object
    var data = JsonSerializer.Deserialize<Token>(jsonString);
    if ((data != null) && (GetDateTimeFromAccessToken(data.AccessToken) > DateTime.Now))
    {
        Console.WriteLine($"TimeStamp: {GetDateTimeFromAccessToken(data.AccessToken)},\nRequest Token: {data.RequestToken},\nAccessToken: {data.AccessToken},\nRefreshToken: {data.RefreshToken}");
    
        return new Token
        {
            RequestToken = data.RequestToken,
            AccessToken = data.AccessToken,
            RefreshToken = data.RefreshToken,
            Status = data.Status,
            Message = data.Message
        };
    }
    else
    {
        return null;
    }
}

static Token GenerateAccessToken(_5PaisaAPI paisaAPI)
{
    #region Authentication
    string filePath = "token.json";
    Console.WriteLine("Please Enter TOTP From Authenticator App: ");
    string totp = Console.ReadLine();
    var response = paisaAPI.TOTPLogin(_EmailId: "9830182496", _TOTP: totp, _Pin: "110888");

    if (response.status == "0")
    {
        string requestTokenOTP = response.TokenResponse.RequestToken;
        Console.WriteLine("Req Token: " + requestTokenOTP);
        var loginResponse = paisaAPI.GetOuthLogin(requestTokenOTP);
        var accessToken = loginResponse.TokenResponse.AccessToken;

        Console.WriteLine("Access Token: " + accessToken);
        Console.WriteLine("Refresh Token: " + loginResponse.TokenResponse.RefreshToken);

        #region AuthData save

        var tokenData = new Token
        {
            //TimeStamp = DateTime.UtcNow,
            RequestToken = requestTokenOTP,
            AccessToken = accessToken,
            RefreshToken = loginResponse.TokenResponse.RefreshToken,
            Status = loginResponse.TokenResponse.Status,
            Message = loginResponse.TokenResponse.Message
        };

        // Serialize object to JSON and write to file
        var options = new JsonSerializerOptions { WriteIndented = true }; // Pretty print JSON
        string tokenDataJson = JsonSerializer.Serialize(tokenData, options);
        File.WriteAllText(filePath, tokenDataJson);

        Console.WriteLine("Auth data saved successfully!");
        return ReadTokenFromFile(filePath);
        #endregion
    }
    else
    {
        return null;
    }
    #endregion
}

static DateTime GetDateTimeFromAccessToken(string accessToken)
{
    try
    {
        // Decode and extract payload from JWT
        var payload = DecodeJwtPayload(accessToken);

        if (payload != null)
        {
            // Extract 'iat' and 'exp' values
            DateTime iat = payload.IssuedAt;
            long exp = payload.Expiration.Value;

            // Convert to DateTime (UTC)
            //DateTime iatDateTime = DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime;
            DateTime expDateTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

            Console.WriteLine($"Issued At (iat): {iat}");
            Console.WriteLine($"Expires At (exp): {expDateTime}");
            return expDateTime;
        }
        else
        {
            return DateTime.Now;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error decoding token: {ex.Message}");
        return DateTime.Now;
    }
}

static JwtPayload DecodeJwtPayload(string token)
{
    try
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        return jwtToken.Payload;
    }
    catch
    {
        return null;
    }
}