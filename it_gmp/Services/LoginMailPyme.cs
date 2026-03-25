using it.Areas.Admin.Models;
using System.Text.Json;

namespace it.Services
{
    public class LoginMailPyme
    {

        public LoginMailPyme()
        {

        }
        public bool is_pyme(string email)
        {
            string[] words = email.Split('@');
            var is_pyme = false;
            if (words.Length > 1)
            {
                is_pyme = words[1] == "astahealthcare.com" ? true : false;
            }
            return is_pyme;
        }
        public async Task<LoginResponse> login(string email, string password)
        {
            try
            {


                var client = new HttpClient();

                var values = new Dictionary<string, string>
                {
                    { "email", email },
                    { "password", password }
                };
                var content = new FormUrlEncodedContent(values);
                var url = "https://esign.astahealthcare.com/api/CheckLogin";
                var response = await client.PostAsync(url, content);

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine(json);
                if (response.IsSuccessStatusCode)
                {
                    LoginResponse responseJson1 = await response.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (responseJson1.authed == true)
                    {

                        return new LoginResponse() { authed = true };
                    }
                    else
                    {
                        return new LoginResponse() { authed = false };
                    }
                }
                else
                {
                    return new LoginResponse() { authed = false };
                }
            }
            catch
            {
                return new LoginResponse() { authed = false };
            }
        }
    }

}