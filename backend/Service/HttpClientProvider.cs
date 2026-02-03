namespace backend.Service
{
    public static class HttpClientProvider
    {
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, SslPolicyErrors) => true
        };
        public static readonly HttpClient Client = new HttpClient(handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromMinutes(120)
        };


    }
}
