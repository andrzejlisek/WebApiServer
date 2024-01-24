using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace WebApiServer
{
    public class ApiConnNet : ApiConn
    {
        Thread LoopThr;
        TcpClient TCPC;
        NetworkStream NSX;
        byte[] StreamBuf = new byte[1000000];

        public override void Open(string Address, bool Push_)
        {
            if (!RegexTest.Match(Address, CommandArgs.RegNet))
            {
                throw new Exception("Network pattern mismatch \"" + Address + "\"");
            }
            RecvBuf.Clear();
            RecvBufL = 0;
            Push = Push_;
            int Idx = Address.IndexOf(':');
            TCPC = new TcpClient();
            TCPC.Connect(Address.Substring(0, Idx), int.Parse(Address.Substring(Idx + 1)));
            NSX = TCPC.GetStream();

            LoopThr = new Thread(NetLoop);
            LoopThr.Start();
        }

        public void NetLoop()
        {
            while (true)
            {
                try
                {
                    while (NSX.DataAvailable)
                    {
                        int Avail = 0;
                        try
                        {
                            Avail = NSX.Read(StreamBuf, 0, StreamBuf.Length);
                            if (Avail > 0)
                            {
                                RecvProcess(StreamBuf, Avail);
                            }
                            else
                            {
                                if (Status() == 0)
                                {
                                    break;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        public override int Status()
        {
            if (NSX == null)
            {
                return 0;
            }
            if (TCPC == null)
            {
                return 0;
            }
            if (TCPC.Client == null)
            {
                return 0;
            }
            try
            {
                IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();
                foreach (TcpConnectionInformation c in tcpConnections)
                {
                    TcpState stateOfConnection = c.State;

                    if (c.LocalEndPoint.Equals(TCPC.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(TCPC.Client.RemoteEndPoint))
                    {
                        if (stateOfConnection == TcpState.Established)
                        {
                            return 1;
                        }
                        else
                        {
                            TCPC = null;
                            return 0;
                        }
                    }
                }
            }
            catch (ObjectDisposedException e)
            {
                TCPC = null;
            }
            return 1;
        }

        public override void Close()
        {
            try
            {
                TCPC.Close();
            }
            catch (Exception E)
            {
            }
            TCPC = null;
        }

        public override void Send(byte[] Data)
        {
            try
            {
                NSX.Write(Data, 0, Data.Length);
                NSX.Flush();
            }
            catch
            {

            }
        }
    }
}
