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

        public static void Scan(string addr, string mac)
        {
            FileSystemWatcher watcher = null;
            string tempFile = null;
            try
            {
                tempFile = Path.GetTempFileName();
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
                watcher = new FileSystemWatcher
                {
                    Path = Path.GetTempPath(),
                    Filter = tempFile,
                };
                watcher.Changed += (sender, args) => { ProcessChanged(tempFile, mac); };
                if (process != null) process.WaitForExit(NmapTimeoutMillis);
                else return;
                if (!process.HasExited) process.Kill();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                if (watcher != null)
                    watcher.EnableRaisingEvents = false;
                if (tempFile != null && File.Exists(tempFile)) File.Delete(tempFile);
            }
        }


        public static ISet<string> Book = new HashSet<string>();
        private static void ProcessChanged(string path, string mac)
        {
            var content = File.ReadAllText(path);
            File.Delete(path);
            if (!content.Contains("</nmaprun>")) content += "\n</nmaprun>"; // process terminates early so no end for root element, we need to add it manually
            Program.SendResult(content);
            /*string addr = ParseXml(content, mac);
            if (addr == null || Book.Contains(addr)) return;
            Book.Add(addr);
            Program.SendResult(addr);*/
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
