using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.Core.DTOs;

public class LoginResponse : BaseResponse<LoginData>
{
}

public class LoginData
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserInfoDto UserInfo { get; set; } = new();
}

