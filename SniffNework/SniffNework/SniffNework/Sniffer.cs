
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace SniffNework {

    public enum Protocol {
        TCP = 6,
        UDP = 17,
        Unknown = -1
    };

    class Sniffer {

        public Sniffer(){}

        private byte[] byteData = new byte[4096];
        private string targetIP = string.Empty;
        private Socket mSocket = null;
        private int targetPort = 0;

        private void close() {
            try {
                mSocket.Close();
            } catch (Exception e) { }
        }

        public List<string> getIP() {
            List<string> ips = new List<string>();
            IPHostEntry HosyEntry = Dns.GetHostEntry((Dns.GetHostName()));
            if (HosyEntry.AddressList.Length > 0) {
                foreach (IPAddress ip in HosyEntry.AddressList) {
                    if (ip.AddressFamily == AddressFamily.InterNetwork) {
                        string value = ip.ToString();
                        if (!ips.Contains(value) && !value.Equals("127.0.0.1")) ips.Add(value);
                    }
                }
            }
            return ips;
        }

        public void create(string ipv4) {
            if (ipv4 != null && ipv4.Length > 0) {
                mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                mSocket.Bind(new IPEndPoint(IPAddress.Parse(ipv4), 0));
                mSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                byte[] byTrue = new byte[4] { 1, 0, 0, 0 }, byOut = new byte[4] { 1, 0, 0, 0 };
                mSocket.IOControl(IOControlCode.ReceiveAll, byTrue, byOut);
                mSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            }
        }

        public void create(List<string> ipv4) {
            if (ipv4 != null && ipv4.Count > 0) {
                mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                for(int i=0; i<ipv4.Count; i++) mSocket.Bind(new IPEndPoint(IPAddress.Parse(ipv4[i]), 0));
                mSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                byte[] byTrue = new byte[4] { 1, 0, 0, 0 }, byOut = new byte[4] { 1, 0, 0, 0 };
                mSocket.IOControl(IOControlCode.ReceiveAll, byTrue, byOut);
                mSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            }
        }

        private void OnReceive(IAsyncResult ar) {
            try {
                int nReceived = mSocket.EndReceive(ar);
                //byteData = new byte[4096];
                ParseData(byteData, nReceived);
                mSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            } catch (Exception e) {
                int i = 0;
            }
        }

        private void ParseData(byte[] byteData, int nReceived) {
            IPHeader ipHeader = new IPHeader(byteData, nReceived);
            switch (ipHeader.ProtocolType) {
                /*case Protocol.TCP: {
                    TCPHeader tcpHeader = new TCPHeader(ipHeader.Data, ipHeader.MessageLength);
                    if (tcpHeader.Flags.Equals("0x18 (PSH, ACK)")) {
                        byte[] data = new byte[(int)ipHeader.MessageLength];
                        for (int i = 0; i < data.Length; i++) data[i] = tcpHeader.Data[i];
                        string content = Encoding.ASCII.GetString(data);
                        if (content.StartsWith("GET ")) parseData(ipHeader, tcpHeader, content, true);
                        else if (content.StartsWith("HTTP/1.1 ")) parseData(ipHeader, tcpHeader, content, false);
                    }
                    break;
                }*/
                case Protocol.UDP: {
                    UDPHeader udpHeader = new UDPHeader(ipHeader.Data, ipHeader.MessageLength);
                    int destinationPort = Int32.Parse(udpHeader.DestinationPort);
                    int sourcePort = Int32.Parse(udpHeader.SourcePort);

                    if (destinationPort == 53 || sourcePort == 53) {
                        byte[] fillBuffer = new byte[ipHeader.MessageLength];
                        string result = string.Empty;
                        for (int i = 0, c=0; i < fillBuffer.Length; i++) {
                            int byteInt = ipHeader.Data[i];
                            if (byteInt < 32) {
                                if (c > 3 && result.Length > 0) result += ".";
                                else c++;
                            }
                            else if (byteInt > 32 && byteInt < 127) result += (char)byteInt;
                        }

                        char[] charArray = result.ToCharArray();
                        char oldChar = '.';
                        for (int i = 0; i < charArray.Length; i++) {
                            if (i > 3 && charArray[i] != oldChar && charArray[i-1] == oldChar && charArray[i-2] == oldChar) {
                                result = string.Empty;
                                for (int j = i; j < charArray.Length; j++) result += charArray[j];
                                break;
                            }
                            oldChar = charArray[i];
                        }
                        result = result.Replace(".....", "");
                        Console.WriteLine("DNS: " + result);
                    }
                    break;
                }
                default: {
                    break;
                }
            }
        }

        private void parseData(IPHeader ipHeader, TCPHeader tcpHeader, string content, bool isRequest) {
            string destionationIP = ipHeader.DestinationAddress.ToString();
            int destinationPort = Int32.Parse(tcpHeader.DestinationPort);
            string sourceIP = ipHeader.SourceAddress.ToString();
            int sourcePort = Int32.Parse(tcpHeader.SourcePort);

            if (!isRequest) {
                int index = content.IndexOf("\r\n\r\n");
                if (index != -1) {
                    content = content.Substring(0, index).Trim();
                }
            }

            string[] lines = content.Split(new char[] { '\r', '\n' });
            if (isRequest && lines != null && lines.Length>0) {
                Hashtable httpRequest = new Hashtable();
                targetIP = destionationIP;
                targetPort = destinationPort;
                httpRequest.Add("REQUEST_IP_SOURCE", sourceIP);
                httpRequest.Add("REQUEST_PORT_SOURCE", sourcePort);
                httpRequest.Add("REQUEST_IP_DESTINATION", destionationIP);
                httpRequest.Add("REQUEST_PORT_DESTINATION", destinationPort);

                for (int i = 0; i < lines.Length; i++) {
                    if (lines[i] != null && lines[i].Length > 0) {
                        if (lines[i].StartsWith("GET ")) {
                            string value = lines[i].Substring("GET ".Length);
                            int index = value.IndexOf(" ");
                            if (index != -1) value = value.Substring(0, index);
                            httpRequest.Add("REQUEST_URL", value);
                        }
                        else {
                            int index = lines[i].IndexOf(":");
                            if (index > 0) {
                                string key = lines[i].Substring(0, index).Trim();
                                string value = lines[i].Substring(index + 1).Trim();
                                if (!httpRequest.ContainsKey(key)) httpRequest.Add(key, value);
                            }
                        }
                    }
                }
                receiveRequest(httpRequest);
            }
            else if (!isRequest && lines != null && lines.Length > 0) {
                if (targetIP != null && targetIP.Equals(sourceIP) && targetPort == sourcePort) {
                    Hashtable httpResponse = new Hashtable();
                    httpResponse.Add("RESPONSE_IP_SOURCE", sourceIP);
                    httpResponse.Add("RESPONSE_PORT_SOURCE", sourcePort);
                    httpResponse.Add("RESPONSE_IP_DESTINATION", destionationIP);
                    httpResponse.Add("RESPONSE_PORT_DESTINATION", destinationPort);

                    for (int i = 0; i < lines.Length; i++) {
                        if (lines[i] != null && lines[i].Length > 0) {
                            if (lines[i].StartsWith("HTTP/1.1 ")) {
                                string value = lines[i].Substring("HTTP/1.1 ".Length);
                                httpResponse.Add("REQUEST_CODE", value);
                            }
                            else {
                                int index = lines[i].IndexOf(":");
                                if (index > 0) {
                                    string key = lines[i].Substring(0, index).Trim();
                                    string value = lines[i].Substring(index + 1).Trim();
                                    if (!httpResponse.ContainsKey(key)) httpResponse.Add(key, value);
                                }
                            }
                        }
                    }
                    receiveResponse(httpResponse);
                }
            }
        }

        private void receiveRequest(Hashtable httpRequest) {
            string text = "ReceiveRequest:\n\r\n\r";
            foreach (string key in httpRequest.Keys) text += key + " => " + httpRequest[key];
            Debug.WriteLine(text);
            Console.WriteLine(text);
        }

        private void receiveResponse(Hashtable httpResponse) {
            string text = "ReceiveResponse:\n\r\n\r";
            foreach (string key in httpResponse.Keys) text += key + " => " + httpResponse[key];
            Debug.WriteLine(text);
            Console.WriteLine(text);
        }

        [STAThread]
        static void Main()
        {
            Sniffer sniff = new Sniffer();
            List<string> ips = sniff.getIP();
            //sniff.create("192.168.0.10");
            //sniff.create("169.254.174.12");
            sniff.create("169.254.225.151");

            while (true)
            {
                Thread.Sleep(1000);
            }

        }

        

    }
}
