namespace KodiNet.Application.Exceptions;

/// <summary>SSH credentials were rejected by the target Pi.</summary>
public sealed class SshAuthenticationException(string host)
    : Exception($"SSH connection refused on {host} — incorrect credentials.")
{
    public string Host { get; } = host;
}

/// <summary>The Pi is unreachable on the network.</summary>
public sealed class PiUnreachableException(string host)
    : Exception($"Impossible to reach {host}.")
{
    public string Host { get; } = host;
}