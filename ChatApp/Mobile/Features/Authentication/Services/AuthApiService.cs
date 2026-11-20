using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApp.Mobile.Features.Authentication.DTOs;

namespace ChatApp.Mobile.Features.Authentication.Services;

public class AuthApiService : IAuthApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _storageService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiService(HttpClient httpClient, ISecureStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<ApiResult<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/register", request, _jsonOptions);
            return await HandleResponseAsync<AuthResponseDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<AuthResponseDto>.Failure("تعذر الاتصال بالسيرفر. يرجى التأكد من تشغيل الباك إند والاتصال بالشبكة.", 503);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<AuthResponseDto>.Failure("انتهت مهلة الاتصال بالسيرفر.", 408);
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponseDto>.Failure($"حدث خطأ أثناء التسجيل: {ex.Message}", 500);
        }
    }

    public async Task<ApiResult<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", request, _jsonOptions);
            return await HandleResponseAsync<AuthResponseDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<AuthResponseDto>.Failure("تعذر الاتصال بالسيرفر. يرجى التأكد من تشغيل الباك إند والاتصال بالشبكة.", 503);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<AuthResponseDto>.Failure("انتهت مهلة الاتصال بالسيرفر.", 408);
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponseDto>.Failure($"حدث خطأ أثناء تسجيل الدخول: {ex.Message}", 500);
        }
    }

    public async Task<ApiResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var request = new RefreshTokenRequestDto(refreshToken);
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/refresh", request, _jsonOptions);
            return await HandleResponseAsync<AuthResponseDto>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponseDto>.Failure($"فشل تجديد الجلسة: {ex.Message}", 500);
        }
    }

    public async Task<ApiResult<bool>> LogoutAsync()
    {
        try
        {
            var token = await _storageService.GetAccessTokenAsync();
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(req);
            await _storageService.ClearAllAsync();
            return ApiResult<bool>.Success(true, (int)response.StatusCode);
        }
        catch
        {
            await _storageService.ClearAllAsync();
            return ApiResult<bool>.Success(true, 200);
        }
    }

    public async Task<ApiResult<UserProfileDto>> GetCurrentUserAsync(string? accessToken = null)
    {
        try
        {
            var token = accessToken ?? await _storageService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return ApiResult<UserProfileDto>.Failure("لا يوجد رمز دخول محفوظ.", 401);
            }

            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<UserProfileDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<UserProfileDto>.Failure("تعذر الاتصال بالسيرفر.", 503);
        }
        catch (Exception ex)
        {
            return ApiResult<UserProfileDto>.Failure($"حدث خطأ أثناء جلب الملف الشخصي: {ex.Message}", 500);
        }
    }

    public async Task<ApiResult<UserProfileDto>> UpdateProfileAsync(UpdateProfileRequestDto request)
    {
        try
        {
            var token = await _storageService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return ApiResult<UserProfileDto>.Failure("لا يوجد رمز دخول محفوظ.", 401);
            }

            using var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me")
            {
                Content = JsonContent.Create(request, options: _jsonOptions)
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(req);
            return await HandleResponseAsync<UserProfileDto>(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<UserProfileDto>.Failure("تعذر الاتصال بالسيرفر.", 503);
        }
        catch (Exception ex)
        {
            return ApiResult<UserProfileDto>.Failure($"حدث خطأ أثناء حفظ الملف الشخصي: {ex.Message}", 500);
        }
    }

    private async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return ApiResult<T>.Success(apiResponse.Data, (int)response.StatusCode);
                }
            }
            catch
            {
                // Fallback direct deserialization if not wrapped
                var directData = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                if (directData != null)
                {
                    return ApiResult<T>.Success(directData, (int)response.StatusCode);
                }
            }
        }

        // Handle error responses
        string errorMessage = "فشلت العملية. يرجى المحاولة مرة أخرى.";

        try
        {
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);
            if (errorResponse != null)
            {
                errorMessage = errorResponse.GetFormattedErrors();
            }
        }
        catch
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                errorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة.";
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                errorMessage = "البريد الإلكتروني أو اسم المستخدم مستخدم بالفعل.";
            }
            else if (!string.IsNullOrWhiteSpace(content))
            {
                errorMessage = content;
            }
        }

        return ApiResult<T>.Failure(errorMessage, (int)response.StatusCode);
    }
}
