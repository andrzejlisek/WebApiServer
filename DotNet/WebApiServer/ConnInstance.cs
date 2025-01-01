using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace WebApiServer
{
    public class ConnInstance
    {
        static Mutex Mtx;

        string ClientId = "";

        int InstanceNo = 0;

        bool ConnectionWork = true;

        static ConnInstance()
        {
            Mtx = new Mutex();
        }

        static void LoopWait()
        {
            if (CommandArgs.LoopWait > 0)
            {
                try
                {
                    Thread.Sleep(CommandArgs.LoopWait);
                }
                catch (Exception e)
                {

                }
            }
        }

        public ConnInstance(int InstanceNo_)
        {
            InstanceNo = InstanceNo_;
        }

        static short BytesToInt16(byte B1, byte B2)
        {
            return BitConverter.ToInt16(new byte[] { B1, B2 }, 0);
        }

        static long BytesToInt64(byte B1, byte B2, byte B3, byte B4, byte B5, byte B6, byte B7, byte B8)
        {
            return BitConverter.ToInt64(new byte[] { B1, B2, B3, B4, B5, B6, B7, B8 }, 0);
        }

        static TcpListener server;

        public static bool StartListen(int PortNo)
        {
            try
            {
                Console.WriteLine(PortNo);
                server = new TcpListener(IPAddress.Any, PortNo);
                server.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        Thread Thr;

        public void Start()
        {
            Thr = new Thread(StartWork);
            Thr.Start();
        }

        NetworkStream stream;

        private void StartWork()
        {
            try
            {
                Console.WriteLine("Instance " + InstanceNo + " - Waiting for connection");
                TcpClient client = server.AcceptTcpClient();

                stream = client.GetStream();
                Console.WriteLine("Instance " + InstanceNo + " - Handshaking");

                Mtx.WaitOne();
                MainClass.NewInstance();
                Mtx.ReleaseMutex();

                // enter to an infinite cycle to be able to handle every change in stream
                string BufS = "";
                List<byte> BufB = new List<byte>();
                bool ServerHandshake = true;
                while (ServerHandshake)
                {
                    while (!stream.DataAvailable)
                    {
                        LoopWait();
                    }
                    byte[] bytes = new byte[client.Available];
                    stream.Read(bytes, 0, bytes.Length);
                    BufS = BufS + Encoding.UTF8.GetString(bytes);
                    if (BufS.StartsWith("GET"))
                    {
                        BufS = BufS.Replace("\r\n", "\n");
                        int BufEnd = BufS.IndexOf("\n\n") + 2;
                        if (BufEnd > 0)
                        {
                            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                            // 3. Compute SHA-1 and Base64 hash of the new value
                            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                            string swk = Regex.Match(BufS, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                            ClientId = swk;

                            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                            byte[] response = Encoding.UTF8.GetBytes(
                                "HTTP/1.1 101 Switching Protocols\r\n" +
                                "Connection: Upgrade\r\n" +
                                "Upgrade: websocket\r\n" +
                                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                            stream.Write(response, 0, response.Length);

                            ServerHandshake = false;
                            Console.WriteLine("Instance " + InstanceNo + " - " + ClientId + " - Connected");
                        }
                    }
                }
                while (ConnectionWork)
                {
                    while (!stream.DataAvailable)
                    {
                        LoopWait();
                    }
                    byte[] bytes = new byte[client.Available];
                    stream.Read(bytes, 0, bytes.Length);
                    BufB.AddRange(bytes);

                    string Resp = DataFrameToString(BufB);
                    while (Resp.Length > 0)
                    {
                        //Mtx.WaitOne();
                        KeyValue MessageI = KeyValue.PoolAssign();
                        KeyValue MessageO = KeyValue.PoolAssign();
                        MessageI.StringLoad(Resp);
                        MessageO.ParamClear();
                        MainClass.ApiAction(MessageI, MessageO, this, ClientId);
                        Resp = MessageO.StringSave();
                        KeyValue.PoolRelease(MessageI);
                        KeyValue.PoolRelease(MessageO);
                        //Mtx.ReleaseMutex();

                        if (Resp.Length > 0)
                        {
                            byte[] RespX = DataStringToFrame(Resp);
                            stream.Write(RespX, 0, RespX.Length);
                        }

                        Resp = DataFrameToString(BufB);
                    }
                }
                Console.WriteLine("Instance " + InstanceNo + " - " + ClientId + " - Disconnected");
            }
            catch (Exception E)
            {
                ConnectionWork = false;
                if ("".Equals(ClientId))
                {
                    Console.WriteLine("Instance " + InstanceNo + " - Connection error: " + E.Message);
                }
                else
                {
                    Console.WriteLine("Instance " + InstanceNo + " - " + ClientId + " - Connection error: " + E.Message);
                }
            }
        }

        /// <summary>
        /// Decodes the string from WebSocket frame
        /// </summary>
        /// <returns>The decoded data.</returns>
        /// <param name="buffer">Buffer.</param>
        private string DataFrameToString(List<byte> buffer)
        {
            if (buffer.Count == 0)
            {
                return "";
            }

            int b = buffer[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BytesToInt16(buffer[3], buffer[2]);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BytesToInt64(buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2]);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > buffer.Count)
            {
                return "";
            }

            byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
                count++;
            }

            int Opcode = buffer[0];
            if (Opcode < 0)
            {
                Opcode += 256;
            }
            switch (Opcode)
            {
                default:
                    buffer.RemoveRange(0, totalLength);
                    return "";
                case 136:
                    buffer.RemoveRange(0, totalLength);
                    ConnectionWork = false;
                    return "";
                case 129:
                    string RawStr = Encoding.UTF8.GetString(buffer.ToArray(), dataIndex, dataLength);
                    buffer.RemoveRange(0, totalLength);
                    return RawStr;
            }
        }

        /// <summary>
        /// Creates the WebSocket frame from string
        /// </summary>
        /// <returns>The frame from string.</returns>
        /// <param name="Message">Message.</param>
        private byte[] DataStringToFrame(string Message)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.UTF8.GetBytes(Message);
            byte[] frame = new byte[10];

            long indexStartRawData = -1;
            long length = (long)bytesRaw.Length;

            // 0 - Fragment
            // 1 - Text
            // 2 - Binary
            // 8 - ClosedConnection
            // 9 - Ping
            // 10 - Pong
            int Opcode = 1;

            frame[0] = (byte)(128 + Opcode);
            if (length <= 125)
            {
                frame[1] = (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (byte)127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }

        public void Push(KeyValue MessageO)
        {
            MessageO.ParamSet("Id", 0);
            string Resp = MessageO.StringSave();
            if (Resp.Length > 0)
            {
                byte[] RespX = DataStringToFrame(Resp);
                try
                {
                    stream.Write(RespX, 0, RespX.Length);
                }
                catch (Exception E)
                {
                    Console.WriteLine("Instance " + InstanceNo + " - " + ClientId + " - Push error: " + E.Message);
                }
            }
        }
    }
}
