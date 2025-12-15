namespace FufuLauncher.Models;
public class SignInResult
{
    public string Code { get; set; } = string.Empty;
    public int RiskCode { get; set; }
    public bool IsRisk { get; set; }
}