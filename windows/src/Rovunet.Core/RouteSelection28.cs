namespace Rovunet.Core;

public static class RouteSelection28
{
    public static string Normalize(string? mode) => mode is "failover" or "smart" ? mode : "manual";
    public static bool ShouldReplace(string mode, bool failed, long? currentMs, long candidateMs)
        => Normalize(mode) != "manual" && candidateMs > 0 &&
           (failed || mode == "smart" && currentMs is > 0 && candidateMs <= currentMs.Value / 2);
    public static TimeSpan TestBudget(Profile profile)
        => TimeSpan.FromSeconds(profile.Protocol == "Tor" ? 90 : TransportPlan.UsesSlip(profile.Raw) ? 65 :
            ProfileEditing.IsDns(profile) ? 40 : TransportPlan.UsesNaive(profile.Raw) || SshHostKey.Required(profile) ? 25 : 18);
}
