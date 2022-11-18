namespace LibrelancerAuthentication;

public interface IExpiringItem
{
    public DateTime Expiry { get; }
}