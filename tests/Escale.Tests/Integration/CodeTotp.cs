using System.Security.Cryptography;

namespace Escale.Tests.Integration
{
    // Ce que fait une application d'authentification : RFC 6238, un code à six
    // chiffres dérivé de la clé partagée et de la tranche de trente secondes en
    // cours. Sans cela, aucun test ne peut franchir la double authentification.
    public static class CodeTotp
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Calculer(string cleBase32, long decalageDeTranche = 0)
        {
            byte[] cle = DecoderBase32(cleBase32);
            long tranche = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30 + decalageDeTranche;

            byte[] compteur = BitConverter.GetBytes(tranche);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(compteur);
            }

            using HMACSHA1 hmac = new HMACSHA1(cle);
            byte[] empreinte = hmac.ComputeHash(compteur);

            int position = empreinte[^1] & 0x0F;
            int binaire = ((empreinte[position] & 0x7F) << 24)
                | ((empreinte[position + 1] & 0xFF) << 16)
                | ((empreinte[position + 2] & 0xFF) << 8)
                | (empreinte[position + 3] & 0xFF);

            return (binaire % 1_000_000).ToString("D6");
        }

        private static byte[] DecoderBase32(string saisie)
        {
            string propre = saisie.Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
            List<byte> octets = new List<byte>();
            int tampon = 0;
            int bits = 0;

            foreach (char c in propre)
            {
                int valeur = Alphabet.IndexOf(c);
                if (valeur < 0)
                {
                    continue;
                }

                tampon = (tampon << 5) | valeur;
                bits += 5;

                if (bits >= 8)
                {
                    bits -= 8;
                    octets.Add((byte)(tampon >> bits));
                }
            }

            return octets.ToArray();
        }
    }
}
