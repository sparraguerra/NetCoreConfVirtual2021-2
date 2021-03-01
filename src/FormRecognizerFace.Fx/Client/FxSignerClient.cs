using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FormRecognizerFace.Fx
{
    public interface IFxSignerClient
    {
        Task<string> GetFacturaeSigned(FacturaeSignRequest request);
    }

    public class FacturaeSignRequest
    {
        public string CertificatePassword { get; set; }
        public string CertificateBase64 { get; set; }
        public string FileContentBase64 { get; set; }
    }

    internal class FacturaeSignResponse
    {
        public string FaceSignedBase64 { get; set; }
    }

    public class FxSignerClient : IFxSignerClient
    {
        private readonly HttpClient httpClient;

        public FxSignerClient(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;
        }

        public async Task<string> GetFacturaeSigned(FacturaeSignRequest request)
        {
            string json = JsonConvert.SerializeObject(request);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            var response =  await httpClient.PostAsync("api/FaceSignFile", content);
            var responseContent = await response.Content.ReadAsAsync<FacturaeSignResponse>();

            return responseContent.FaceSignedBase64;
        }
    }
}
