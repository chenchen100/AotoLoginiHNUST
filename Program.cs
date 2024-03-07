using System.Net.NetworkInformation;
using System.Text;
using ManagedNativeWifi;
using Tomlyn.Model;
using Tomlyn;
using System;
using System.Xml.Serialization;

class Program
{
    static async Task Main(string[] args)
    {
        String ip = GetWlanIpAddress();
        Boolean wlan = IsWlanEnabled();
        IEnumerable<string> ssids = EnumerateConnectedNetworkSsids();
        string firstSsid = ssids.FirstOrDefault();

        string configFilePath = "config.toml";
        if (!File.Exists(configFilePath))
        {
            
            TomlTable defaultConfig = new TomlTable
            {
                ["WIFISSID"] = "your_wifi_ssid",
                ["USERNAME"] = "your_username",
                ["USERPASSWORD"] = "your_password",
                ["telecomOpertor"] = "your_telecom_operator"
            };
            string tomlString = Toml.FromModel(defaultConfig);
            File.WriteAllText(configFilePath, tomlString);
        }
        string tomlContent = File.ReadAllText(configFilePath);
        var config = Toml.ToModel(tomlContent);

        string WIFISSID = config["WIFISSID"].ToString();
        string USERNAME = config["USERNAME"].ToString();
        string USERPASSWORD = config["USERPASSWORD"].ToString();
        string telecomOpertor = config["telecomOpertor"].ToString();

        string loginUrlTemplate = "http://login.hnust.cn:801/eportal/?c=Portal&a=login&callback=dr1004&login_method=1&user_account=,0,{0}{1}&user_password={2}&wlan_user_ip={3}";
        string loginUrl = String.Format(loginUrlTemplate, USERNAME, telecomOpertor, USERPASSWORD, ip);

        

        if (wlan)
        {
            if(ssids.Count() != 0 && firstSsid==WIFISSID) {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(loginUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        LogMessage(responseBody);
                    }
                    else
                    {
                        LogMessage("发送请求失败. 状态码: " + response.StatusCode);
                    }
                }
            }
            else
            {
                LogMessage("当前连接的wifi不是" + WIFISSID);
            }
        }
        else
        {
            LogMessage("未打开wifi");
        }

        Environment.Exit(0);
    }

    private static string GetWlanIpAddress()
    {
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface networkInterface in networkInterfaces)
        {
            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && networkInterface.OperationalStatus == OperationalStatus.Up)
            {
                IPInterfaceProperties properties = networkInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }

        return null;
    }

    private static bool IsWlanEnabled()
    {
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface networkInterface in networkInterfaces)
        {
            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && networkInterface.OperationalStatus == OperationalStatus.Up)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateConnectedNetworkSsids()
    {
        return NativeWifi.EnumerateAvailableNetworkSsids()
            .Select(x => x.ToString());
    }

    static void LogMessage(string message)
    {
        string logFilePath = "log.log";
        // 格式化日期时间信息
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // 构造要写入的日志行内容
        string logLine = $"{timestamp} - {message}";
        // 将日志行追加到 log.log 文件中
        using (StreamWriter writer = File.AppendText(logFilePath))
        {
            writer.WriteLine(logLine);
        }

    }
}
