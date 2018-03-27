using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Xeeny.Transports
{
    public class SecuritySettings
    {
        public bool UseSsl => X509Certificate != null;

        public X509Certificate2 X509Certificate { get; set; }
        public string CertificateName { get; set; }
    }
}
