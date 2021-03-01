using FirmaXadesNetCore;
using FirmaXadesNetCore.Crypto;
using FirmaXadesNetCore.Signature.Parameters;
using FormRecognizerFace.KeyVault;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace FormRecognizerFace.Fx
{
    public class GetFacturaeSignedActivity
    {
        private readonly IKeyVaultService keyvaultService;
        private readonly IConfiguration configuration; 
        private readonly XadesService xadesService;
 
        public GetFacturaeSignedActivity(IKeyVaultService keyvaultService, 
                                        XadesService xadesService,
                                        IConfiguration configuration)
        {
            this.keyvaultService = keyvaultService; 
            this.xadesService = xadesService;
            this.configuration = configuration;
        }
        
        [FunctionName(nameof(GetFacturaeSignedActivity))]
        public async Task<string> RunGetFacturaeSignedActivity([ActivityTrigger] string facturae)
        {
            var certificate = await keyvaultService.GetCertificateAsync(configuration["AppSettings:KeyVault:Certificate:Name"]);

            var signedDocument = new MemoryStream();
            SignatureParameters parameters = new SignatureParameters
            {
                // Política de firma de factura-e 3.1
                SignaturePolicyInfo = new SignaturePolicyInfo
                {
                    PolicyIdentifier = "http://www.facturae.es/politica_de_firma_formato_facturae/politica_de_firma_formato_facturae_v3_1.pdf",
                    PolicyHash = "Ohixl6upD6av8N7pEvDABhEL6hM="
                },
                SignaturePackaging = SignaturePackaging.ENVELOPED,
                DataFormat = new DataFormat
                {
                    MimeType = "text/xml"
                },
                SignerRole = new SignerRole()
            };
            parameters.SignerRole.ClaimedRoles.Add("emisor");

            using (parameters.Signer = new Signer(certificate))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(facturae);               
                var signed = xadesService.Sign(xmlDocument, parameters);
                signed.Save(signedDocument);
            }

            return System.Convert.ToBase64String(signedDocument.ToArray());
        }
    }
}
