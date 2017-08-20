namespace DataConnectors.Common.Helper
{
    public static class EncryptionHelper
    {
        private const string encryptionPrefix = "@@";
        private const string encryptionSuffix = "@@";
        private const string password = ".-*+";

        private static bool useDapi = true;

        public static bool UseDapi
        {
            get { return useDapi; }
            set { useDapi = value; }
        }

        public static bool IsEncrypted(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            return str.StartsWith(encryptionPrefix) && str.EndsWith(encryptionSuffix);
        }

        public static string GetEncryptedString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            if (IsEncrypted(str))
            {
                return str;
            }

            if (UseDapi)
            {
                str = DataProtectionUtil.Encrypt(str);
            }
            else
            {
                str = CryptoUtil.Encrypt(str, password, CryptoUtil.CryptoProviderDES);
            }
            return encryptionPrefix + str + encryptionSuffix;
        }

        public static string GetDecrptedString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            if (IsEncrypted(str))
            {
                str = str.Substring(encryptionPrefix.Length, str.Length - encryptionPrefix.Length - encryptionSuffix.Length);

                if (UseDapi)
                {
                    str = DataProtectionUtil.Decrypt(str);
                }
                else
                {
                    str = CryptoUtil.Decrypt(str, password, CryptoUtil.CryptoProviderDES);
                }
                return str;
            }

            return str;
        }
    }
}