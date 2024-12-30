using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace WebApiServer
{
    public class ApiConnApp : ApiConn
    {
        Process App = null;
        Stream StrI;
        Stream StrO;
        Stream StrE;
        byte[] StreamBufO = new byte[1000000];
        byte[] StreamBufE = new byte[1000000];

        private void LoopOut()
        {
            while (true)
            {
                try
                {
                    int Avail = StrO.Read(StreamBufO, 0, StreamBufO.Length);
                    if (Avail > 0)
                    {
                        if (CommandArgs.Debug > 0)
                        {
                            Console.Write("> ");
                            for (int i = 0; i < Avail; i++)
                            {
                                if ((StreamBufO[i] >= 33) && (StreamBufO[i] <= 126))
                                {
                                    Console.Write((char)StreamBufO[i]);
                                }
                                else
                                {
                                    Console.Write("<");
                                    Console.Write((int)StreamBufO[i]);
                                    Console.Write(">");
                                }
                            }
                            Console.WriteLine();
                        }
                        RecvProcess(StreamBufO, Avail);
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
                    break;
                }
            }
            try
            {
                StrO.Close();
            }
            catch (Exception E)
            {

            }
        }

        private void LoopErr()
        {
            while (true)
            {
                try
                {
                    int Avail = StrE.Read(StreamBufE, 0, StreamBufE.Length);
                    if (Avail > 0)
                    {
                        if (CommandArgs.Debug > 0)
                        {
                            Console.Write("> ");
                            for (int i = 0; i < Avail; i++)
                            {
                                if ((StreamBufO[i] >= 33) && (StreamBufE[i] <= 126))
                                {
                                    Console.Write((char)StreamBufE[i]);
                                }
                                else
                                {
                                    Console.Write("<");
                                    Console.Write((int)StreamBufE[i]);
                                    Console.Write(">");
                                }
                            }
                            Console.WriteLine();
                        }
                        RecvProcess(StreamBufE, Avail);
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
                    break;
                }
            }
            try
            {
                StrE.Close();
            }
            catch (Exception E)
            {

            }
        }

        Thread LoopOutThr;
        Thread LoopErrThr;

        public override void Open(string Address, bool Push_)
        {
            if (!RegexTest.Match(Address, CommandArgs.RegCmd))
            {
                throw new Exception("Command pattern mismatch \"" + Address + "\"");
            }
            RecvBuf.Clear();
            RecvBufL = 0;
            Push = Push_;
            List<string> Cmd = new List<string>();
            bool InQuote1 = false;
            bool InQuote2 = false;
            char LastC = ' ';
            string CmdItem = "";
            for (int I = 0; I < Address.Length; I++)
            {
                switch (Address[I])
                {
                    case ' ':
                        if ((!InQuote1) && (!InQuote2))
                        {
                            Cmd.Add(CmdItem);
                            CmdItem = "";
                        }
                        else
                        {
                            CmdItem = CmdItem + ' ';
                        }
                        break;
                    case '\'':
                        if (InQuote1)
                        {
                            if (LastC != '\\')
                            {
                                InQuote1 = false;
                            }
                            else
                            {
                                CmdItem = CmdItem + '\'';
                            }
                        }
                        else
                        {
                            InQuote1 = true;
                        }
                        break;
                    case '\"':
                        if (InQuote2)
                        {
                            if (LastC != '\\')
                            {
                                InQuote2 = false;
                            }
                            else
                            {
                                CmdItem = CmdItem + '\"';
                            }
                        }
                        else
                        {
                            InQuote2 = true;
                        }
                        break;
                    default:
                        CmdItem = CmdItem + Address[I];
                        break;
                }
                LastC = Address[I];
            }
            if (!"".Equals(CmdItem))
            {
                Cmd.Add(CmdItem);
            }


            App = new Process();
            while (Cmd.Count > 2)
            {
                Cmd[1] = Cmd[1] + " " + Cmd[2];
                Cmd.RemoveAt(1);
            }
            if (Cmd.Count < 2)
            {
                Cmd.Add("");
            }
            App.StartInfo.FileName = Cmd[0];
            App.StartInfo.Arguments = Cmd[1];
            App.StartInfo.RedirectStandardInput = true;
            App.StartInfo.RedirectStandardOutput = true;
            App.StartInfo.RedirectStandardError = true;

            App.StartInfo.UseShellExecute = false;
            if (App.Start())
            {
                StrI = App.StandardInput.BaseStream;
                StrO = App.StandardOutput.BaseStream;
                StrE = App.StandardError.BaseStream;

                LoopOutThr = new Thread(LoopOut);
                LoopOutThr.Start();
                LoopErrThr = new Thread(LoopErr);
                LoopErrThr.Start();
            }
            else
            {
                App = null;
            }
        }

        public override int Status()
        {
            if (App == null)
            {
                return 0;
            }
            if (App.HasExited)
            {
                return 0;
            }
            return 1;
        }

        public override void Close()
        {
            try
            {
                App.Kill();
            }
            catch (Exception E)
            {
            }
            try
            {
                StrI.Close();
            }
            catch (Exception E)
            {

            }
            App = null;
        }

        public override void Send(byte[] Data)
        {
            if (CommandArgs.Debug > 0)
            {
                Console.Write("< ");
                for (int i = 0; i < Data.Length; i++)
                {
                    if ((Data[i] >= 33) && (Data[i] <= 126))
                    {
                        Console.Write((char)Data[i]);
                    }
                    else
                    {
                        Console.Write("<");
                        Console.Write((int)Data[i]);
                        Console.Write(">");
                    }
                }
                Console.WriteLine();
            }
            StrI.Write(Data, 0, Data.Length);
        }
    }
}