using BlazorAppSecurityTutorial.Shared;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorAppSecurityTutorial.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient,
                           AuthenticationStateProvider authenticationStateProvider,
                           ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
            _localStorage = localStorage;
        }
        //The Register method posts the registerModel to the accounts controller and then returns the RegisterResult to the caller.
        public async Task<RegisterResult> Register(RegisterModel registerModel)
        {
            var result = await _httpClient.PostJsonAsync<RegisterResult>("api/accounts", registerModel);

            return result;
        }

        //method is similar to the Register method, it posts the LoginModel to the login controller. But when a successful result is returned it strips out the auth token and persists it to local storage.
        public async Task<LoginResult> Login(LoginModel loginModel)
        {
            var loginAsJson = JsonSerializer.Serialize(loginModel);
            var response = await _httpClient.PostAsync("api/Login", new StringContent(loginAsJson, Encoding.UTF8, "application/json"));
            var loginResult = JsonSerializer.Deserialize<LoginResult>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!response.IsSuccessStatusCode)
            {
                return loginResult;
            }
            //the MarkUserAsAuthenticated method we just looked at on the ApiAuthenticationStateProvider. Finally, it sets the default authorization header on the HttpClient.
            await _localStorage.SetItemAsync("authToken", loginResult.Token);
            ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(loginModel.Email);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", loginResult.Token);

            return loginResult;
        }
        //method is just doing the reverse of the Login method.
        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
