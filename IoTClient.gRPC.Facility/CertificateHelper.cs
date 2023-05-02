using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IoTClient.gRPC.Facility
{
    internal class CertificateHelper
    {
        private readonly IConfiguration _configuration;

        public CertificateHelper(IConfiguration configuration)
        {
            // Set up configuration
           _configuration = configuration;
        }

        public X509Certificate2 getAuthenticationCertificate()
        {
            var authSection = _configuration.GetSection("Authentication");
            if (authSection != null)
            {
                var certFilePath = authSection.GetSection("CertPath").Value;
                var certKeyPath = authSection.GetSection("CertKeyPath").Value;
                return   new X509Certificate2(certFilePath, certKeyPath);
            }
            // return null if cert is not available
            return null;
        }
    }
}
