using it.Areas.Admin.Models;

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
                    { "kerio_username", email },
                    { "kerio_password", password }
                };
                var content = new FormUrlEncodedContent(values);
                var url = "https://mail.astahealthcare.com/webmail/login/dologin";
                var response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    var AbsoluteUri = response.RequestMessage.RequestUri.AbsoluteUri;//"https://mail.astahealthcare.com/webmail/"  
                    if (AbsoluteUri == "https://mail.astahealthcare.com/webmail/")
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