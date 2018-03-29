using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Xeeny.Sockets
{
    public class SecuritySettings
    {
        public static SecuritySettings CreateForServer(X509Certificate2 certificate, 
            RemoteCertificateValidationCallback validationCallback = null)
        {
            return new SecuritySettings(certificate, validationCallback);
        }
        public static SecuritySettings CreateForClient(string certificateName,
            RemoteCertificateValidationCallback validationCallback = null)
        {
            return new SecuritySettings(certificateName, validationCallback);
        }

        internal readonly X509Certificate2 X509Certificate;
        internal readonly string CertificateName;
        internal readonly RemoteCertificateValidationCallback ValidationCallback;

        private SecuritySettings(X509Certificate2 x509Certificate, RemoteCertificateValidationCallback validationCallback)
        {
            X509Certificate = x509Certificate;
            ValidationCallback = validationCallback;
        }

        private SecuritySettings(string subject, RemoteCertificateValidationCallback validationCallback)
        {
            CertificateName = subject;
            ValidationCallback = validationCallback;
        }
    }
}
