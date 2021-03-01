using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FormRecognizerFace.KeyVault
{
    public class KeyVaultService : IKeyVaultService
    {
#if DEBUG
        private readonly TokenCredential keyVaultCredential = new AzureCliCredential();
#else 
        private readonly TokenCredential keyVaultCredential = new DefaultAzureCredential();
#endif
        const string keyVaultBaseUri = "https://{0}.vault.azure.net/";

        public KeyVaultService(string keyVaultName)
        {
            _ = NotNullOrEmpty(keyVaultName, nameof(keyVaultName));
            InitializeClients(keyVaultName);
        }

        public SecretClient SecretClient { get; set; }

        public KeyClient KeyClient { get; set; }

        public CertificateClient CertificateClient { get; set; }

        public async Task<X509Certificate2> GetCertificateAsync(string certificateName)
        {
            KeyVaultCertificateWithPolicy certificate = await CertificateClient.GetCertificateAsync(certificateName);

            // Return a certificate with only the public key if the private key is not exportable.
            if (certificate.Policy?.Exportable != true)
            {
                return new X509Certificate2(certificate.Cer);
            }

            // Parse the secret ID and version to retrieve the private key.
            string[] segments = certificate.SecretId.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 3)
            {
                throw new InvalidOperationException($"Number of segments is incorrect: {segments.Length}, URI: {certificate.SecretId}");
            }

            string secretName = segments[1];
            string secretVersion = segments[2];

            KeyVaultSecret secret = await SecretClient.GetSecretAsync(secretName, secretVersion);

            // For PEM, you'll need to extract the base64-encoded message body.
            // .NET 5.0 preview introduces the System.Security.Cryptography.PemEncoding class to make this easier.
            if ("application/x-pkcs12".Equals(secret.Properties.ContentType, StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] pfx = Convert.FromBase64String(secret.Value);
                return new X509Certificate2(pfx);
            }

            throw new NotSupportedException($"Only PKCS#12 is supported. Found Content-Type: {secret.Properties.ContentType}");
        }

        private void InitializeClients(string keyVaultName)
        {
            var keyVaultUri = new Uri(string.Format(keyVaultBaseUri, keyVaultName));

            SecretClient = new SecretClient(keyVaultUri, keyVaultCredential);
            KeyClient = new KeyClient(keyVaultUri, keyVaultCredential);
            CertificateClient = new CertificateClient(keyVaultUri, keyVaultCredential);
        }

        private string NotNullOrEmpty(string value, string name)
        {
            switch (value)
            {
                case null:
                    throw new ArgumentNullException(name); 
                case string item when item.Length == 0:
                    throw new ArgumentException("Value must not be empty", name);
                default:
                    break;
            }

            return value;
        }
    }
}
