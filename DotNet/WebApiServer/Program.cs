using System;
using System.Collections.Generic;
using System.Threading;

namespace WebApiServer
{
    public class MainClass
    {
        static List<ConnInstance> ConnInstance_;
        static int InstanceNo = 0;
        static Timer IdleTimer;

        public static void IdleTimerTick(object x)
        {
            ApiFile.IdleTimerTick();
            ApiConn.IdleTimerTick();
        }

        static void Main(string[] args)
        {
            CommandArgs.SetArgs(args);
            if (CommandArgs.PortNo == 0)
            {
                RegexTest.Start();
            }
            else
            {
                if (CommandArgs.Timeout > 0)
                {
                    IdleTimer = new Timer(new TimerCallback(IdleTimerTick), null, 60000, 60000);
                }
                ConnInstance.StartListen(CommandArgs.PortNo);
                ConnInstance_ = new List<ConnInstance>();
                NewInstance();
            }
        }

        public static void NewInstance()
        {
            InstanceNo++;
            ConnInstance CI = new ConnInstance(InstanceNo);
            ConnInstance_.Add(CI);
            CI.Start();
        }

        public static void ApiAction(KeyValue MessageI, KeyValue MessageO, ConnInstance Instance, string ClientId)
        {
            MessageO.ParamSet("Command", MessageI.ParamGetS("Command"));
            MessageO.ParamSet("Id", MessageI.ParamGetS("Id"));
            MessageO.ParamSet("Error", "");
            try
            {
                switch (MessageI.ParamGetS("Command"))
                {
                    case "Test":
                        {
                            int TimeOut = MessageI.ParamGetI("Time");
                            if (TimeOut > 0)
                            {
                                Thread.Sleep(TimeOut);
                            }
                            if (TimeOut < 0)
                            {
                                Thread.Sleep(0 - TimeOut);
                            }
                            MessageO.ParamSet("Error", MessageI.ParamGetS("Error"));
                        }
                        break;
                    case "DirectoryList":
                        {
                            ApiFile.GetDir(MessageI, MessageO);
                        }
                        break;
                    case "FileDelete":
                        {
                            ApiFile.FileDelete(MessageI, MessageO);
                        }
                        break;
                    case "FileOpen":
                        {
                            ApiFile.FileOpen(MessageI, MessageO);
                        }
                        break;
                    case "FileRead":
                        {
                            ApiFile.FileRead(MessageI, MessageO);
                        }
                        break;
                    case "FileWrite":
                        {
                            ApiFile.FileWrite(MessageI, MessageO);
                        }
                        break;
                    case "FileClose":
                        {
                            ApiFile.FileClose(MessageI, MessageO);
                        }
                        break;
                    case "FileInfo":
                        {
                            ApiFile.FileInfo(MessageI, MessageO);
                        }
                        break;
                    case "FileSeek":
                        {
                            ApiFile.FileSeek(MessageI, MessageO);
                            ApiFile.FileInfo(MessageI, MessageO);
                        }
                        break;
                    case "ConnOpen":
                        {
                            ApiConn.ConnOpen(MessageI, MessageO, Instance);
                        }
                        break;
                    case "ConnInfo":
                        {
                            ApiConn.ConnInfo(MessageI, MessageO);
                        }
                        break;
                    case "ConnClose":
                        {
                            ApiConn.ConnClose(MessageI, MessageO);
                        }
                        break;
                    case "ConnSend":
                        {
                            ApiConn.ConnSend(MessageI, MessageO);
                        }
                        break;
                    case "ConnRecv":
                        {
                            ApiConn.ConnRecv(MessageI, MessageO);
                        }
                        break;
                }
            }
            catch (Exception E)
            {
                CatchError(MessageO, E);
                Console.WriteLine("Instance " + InstanceNo + " - " + ClientId + " - Error msg: " + E.Message);
                Console.WriteLine("Instance " + InstanceNo + " - " + ClientId + " - Error type: " + E.GetType().FullName);
            }
        }

        public static void CatchError(KeyValue MessageO, Exception E)
        {
            MessageO.ParamSet("Error", E.GetType().FullName + ":" + E.Message);
        }
    }
}
