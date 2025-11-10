using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EdoSign.Lab_3.Services
{
    public class CryptoService
    {
        // === 1. Генерація RSA-пари ключів ===
        public (string privatePem, string publicPem) GenerateRsaKeyPair()
        {
            using var rsa = RSA.Create(2048);
            var privateKey = ExportPrivateKeyPem(rsa);
            var publicKey = ExportPublicKeyPem(rsa);
            return (privateKey, publicKey);
        }

        // === 2. Підписування даних (у Base64) ===
        public string SignToBase64(byte[] data, string privateKeyPem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem.ToCharArray());

            var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }

        // === 3. Перевірка підпису ===
        public bool VerifySignature(byte[] data, string signatureBase64, string publicKeyPem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem.ToCharArray());

            var signature = Convert.FromBase64String(signatureBase64);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        // === Допоміжні методи для експорту PEM ===
        private string ExportPrivateKeyPem(RSA rsa)
        {
            var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
            var b64 = Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN PRIVATE KEY-----\n{b64}\n-----END PRIVATE KEY-----";
        }

        private string ExportPublicKeyPem(RSA rsa)
        {
            var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            var b64 = Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN PUBLIC KEY-----\n{b64}\n-----END PUBLIC KEY-----";
        }
    }
}
