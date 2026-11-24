using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public readonly struct DBLoginPacket
{
    public readonly DBLoginResult DBLoginResult;
    public readonly uint AccountUid;

    public DBLoginPacket(DBLoginResult dbLoginResult, uint accountUid)
    {
        DBLoginResult = dbLoginResult;
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

    public async UniTask<DBLoginPacket> LoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        using UnityWebRequest request = CreatePostRequest(LoginPath, id, password);

        long responseCode = await SendAsync(request, cancellationToken);

        switch (responseCode)
        {
            case StatusOk:
                LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                return new DBLoginPacket(DBLoginResult.Success, loginResponse.UId);
            case StatusUnauthorized:
                return new DBLoginPacket(DBLoginResult.AccountNotFound, uint.MinValue);
            case StatusNoResponse:
            case StatusServiceUnavailable:
                return new DBLoginPacket(DBLoginResult.ServerUnavailable, uint.MinValue);
            case StatusGatewayTimeout:
                return new DBLoginPacket(DBLoginResult.Timeout, uint.MinValue);
            default:
                return new DBLoginPacket(DBLoginResult.UnknownError, uint.MinValue);
        }
    }

    public async UniTask<DBSignUpResult> SignUpAsync(string id, string password, CancellationToken cancellationToken)
    {
        using UnityWebRequest request = CreatePostRequest(SignUpPath, id, password);

        long responseCode = await SendAsync(request, cancellationToken);

        switch (responseCode)
        {
            case StatusOk:
                return DBSignUpResult.Success;
            case StatusConflict:
                return DBSignUpResult.AccountAlreadyExists;
            case StatusNoResponse:
            case StatusServiceUnavailable:
                return DBSignUpResult.ServerUnavailable;
            case StatusGatewayTimeout:
                return DBSignUpResult.Timeout;
            default:
                return DBSignUpResult.UnknownError;
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
