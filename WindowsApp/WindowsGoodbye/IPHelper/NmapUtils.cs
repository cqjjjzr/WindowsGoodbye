using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace IPHelper
{
    static class NmapUtils
    {
        public const int NmapTimeoutMillis = 4 * 1000;

        public static string Scan(string addr, string mac)
        {
            try
            {
                var tempFile = Path.GetTempFileName();
                string path = Assembly.GetExecutingAssembly().CodeBase;
                string directory = Path.GetDirectoryName(path);
                if (directory.StartsWith("file:\\")) directory = directory.Replace("file:\\", "");
                var info = new ProcessStartInfo(directory + "\\" + "nmap.exe", $"-sP -n -oX \"{tempFile}\" {addr}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var process = Process.Start(info);
                if (process != null) process.WaitForExit(NmapTimeoutMillis);
                else return null;
                if (!process.HasExited) process.Kill();

                if (!File.Exists(tempFile)) return null;
                var content = File.ReadAllText(tempFile);
                File.Delete(tempFile);
                if (!content.Contains("</nmaprun>")) content += "\n</nmaprun>"; // process terminates early so no end for root element, we need to add it manually

                return ParseXml(content, mac);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string ParseXml(string content, string mac)
        {
            mac = mac.ToUpper().Replace("-", ":");
            /*var doc = new XmlDocument();
            doc.LoadXml(content);

            if (doc.DocumentElement == null) return null;

            var hosts = doc.DocumentElement.GetElementsByTagName("host");
            foreach (XmlNode host in hosts)
            {
                var addresses = new List<XmlNode>(host.ChildNodes);
            }*/

            var doc = XDocument.Parse(content);
            var addr = doc.Descendants("host")
                .Where(host => host.Descendants("address")
                                   .FirstOrDefault(element => element.Attribute("addrtype")?.Value == "mac")
                                   ?.Attribute("addr")
                                   ?.Value == mac)
                .Descendants("address").FirstOrDefault(element => element.Attribute("addrtype")?.Value == "ipv4")
                ?.Attribute("addr")
                ?.Value;

            return addr;
        }
    }
}
