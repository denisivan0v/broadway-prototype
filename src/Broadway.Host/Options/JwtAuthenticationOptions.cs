namespace NuClear.Broadway.Host.Options
{
    public class JwtAuthenticationOptions
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public string Certificate { get; set; }
    }
}