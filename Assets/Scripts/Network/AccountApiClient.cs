using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public readonly struct LoginPacket
{
    public readonly LoginResult LoginResult;
    public readonly uint AccountUid;

    public LoginPacket(LoginResult loginResult, uint accountUid)
    {
        LoginResult = loginResult;
        AccountUid = accountUid;
    }
}

[Serializable]
public class AccountRequest
{
    public string Id;
    public string Password;
}

[Serializable]
public class LoginResponse
{
    public uint UId;
}

public class AccountApiClient
{
    private const string BaseUrl = "http://localhost:5000/api/account";
    private const string LoginPath = "/login";
    private const string SignUpPath = "/signUp";

    private const int RequestTimeoutSeconds = 10;

    private const long StatusNoResponse = 0;
    private const long StatusOk = 200;
    private const long StatusUnauthorized = 401;
    private const long StatusConflict = 409;
    private const long StatusInternalServerError = 500;
    private const long StatusServiceUnavailable = 503;
    private const long StatusGatewayTimeout = 504;

    public async UniTask<LoginPacket> LoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        using UnityWebRequest request = CreatePostRequest(LoginPath, id, password);

        long responseCode = await SendAsync(request, cancellationToken);

        switch (responseCode)
        {
            case StatusOk:
                LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                return new LoginPacket(LoginResult.Success, loginResponse.UId);
            case StatusUnauthorized:
                return new LoginPacket(LoginResult.AccountNotFound, uint.MinValue);
            case StatusNoResponse:
            case StatusServiceUnavailable:
                return new LoginPacket(LoginResult.ServerUnavailable, uint.MinValue);
            case StatusGatewayTimeout:
                return new LoginPacket(LoginResult.Timeout, uint.MinValue);
            default:
                return new LoginPacket(LoginResult.UnknownError, uint.MinValue);
        }
    }

    public async UniTask<SignUpResult> SignUpAsync(string id, string password, CancellationToken cancellationToken)
    {
        using UnityWebRequest request = CreatePostRequest(SignUpPath, id, password);

        long responseCode = await SendAsync(request, cancellationToken);

        switch (responseCode)
        {
            case StatusOk:
                return SignUpResult.Success;
            case StatusConflict:
                return SignUpResult.AccountAlreadyExists;
            case StatusNoResponse:
            case StatusServiceUnavailable:
                return SignUpResult.ServerUnavailable;
            case StatusGatewayTimeout:
                return SignUpResult.Timeout;
            default:
                return SignUpResult.UnknownError;
        }
    }

    private UnityWebRequest CreatePostRequest(string path, string id, string password)
    {
        AccountRequest accountRequest = new AccountRequest { Id = id, Password = password };
        byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(accountRequest));

        UnityWebRequest request = new UnityWebRequest(BaseUrl + path, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = RequestTimeoutSeconds;

        return request;
    }

    private async UniTask<long> SendAsync(UnityWebRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
        }
        catch (UnityWebRequestException exception)
        {
            bool isServerError = exception.ResponseCode == StatusNoResponse || exception.ResponseCode >= StatusInternalServerError;

            if (isServerError)
            {
                DebugUtility.LogWarning(exception.ToString());
            }
        }

        return request.responseCode;
    }
}
