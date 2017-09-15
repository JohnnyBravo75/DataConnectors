using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.IO;

namespace DataConnectors.Adapter.DbAdapter
{
    public class TNSNamesReader
    {
        public List<string> GetOracleHomes()
        {
            List<string> oracleHomes = new List<string>();
            RegistryKey rgkLM = Registry.LocalMachine;
            RegistryKey rgkAllHome = rgkLM.OpenSubKey(@"SOFTWARE\ORACLE");
            if (rgkAllHome != null)
            {
                foreach (string subkey in rgkAllHome.GetSubKeyNames())
                {
                    if (subkey.StartsWith("KEY_"))
                    {
                        oracleHomes.Add(subkey);
                    }
                }
            }
            return oracleHomes;
        }

        private string GetOracleHomePath(string OracleHomeRegistryKey)
        {
            RegistryKey rgkLM = Registry.LocalMachine;
            RegistryKey rgkOracleHome = rgkLM.OpenSubKey(@"SOFTWARE\ORACLE\" + OracleHomeRegistryKey);

            if (!rgkOracleHome.Equals(""))
            {
                return rgkOracleHome.GetValue("ORACLE_HOME").ToString();
            }
            return "";
        }

        private string GetTNSNAMESORAFilePath(string OracleHomeRegistryKey)
        {
            string oracleHomePath = this.GetOracleHomePath(OracleHomeRegistryKey);
            string tnsNamesOraFilePath = "";
            if (!oracleHomePath.Equals(""))
            {
                tnsNamesOraFilePath = oracleHomePath + @"\NETWORK\ADMIN\TNSNAMES.ORA";
                if (!(File.Exists(tnsNamesOraFilePath)))
                {
                    tnsNamesOraFilePath = oracleHomePath + @"\NET80\ADMIN\TNSNAMES.ORA";
                }
            }
            return tnsNamesOraFilePath;
        }

        public List<string> LoadTNSNames(string OracleHomeRegistryKey)
        {
            var DBNamesCollection = new List<string>();
            string RegExPattern = @"[\n][\s]*[^\(][a-zA-Z0-9_.]+[\s]*=[\s]*\(";
            string strTNSNAMESORAFilePath = GetTNSNAMESORAFilePath(OracleHomeRegistryKey);

            if (!strTNSNAMESORAFilePath.Equals(""))
            {
                //check out that file does physically exists
                var fiTNS = new FileInfo(strTNSNAMESORAFilePath);
                if (fiTNS.Exists)
                {
                    if (fiTNS.Length > 0)
                    {
                        //read tnsnames.ora file
                        int iCount;
                        for (iCount = 0; iCount < Regex.Matches(
                            File.ReadAllText(fiTNS.FullName),
                            RegExPattern).Count; iCount++)
                        {
                            DBNamesCollection.Add(Regex.Matches(
                                File.ReadAllText(fiTNS.FullName),
                                RegExPattern)[iCount].Value.Trim().Substring(0,
                                Regex.Matches(File.ReadAllText(fiTNS.FullName),
                                RegExPattern)[iCount].Value.Trim().IndexOf(" ")));
                        }
                    }
                }
            }
            return DBNamesCollection;
        }
    }
}