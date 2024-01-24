/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.util.ArrayList;

public class WebApiServer
{
    static ArrayList<ConnInstance> ConnInstance_;
    static int InstanceNo = 0;
    
    public static void main(String[] args)
    {
        CommandArgs.SetArgs(args);
        if (CommandArgs.PortNo == 0)
        {
            RegexTest.Start();
        }
        else
        {
            ConnInstance.StartListen(CommandArgs.PortNo);
            ConnInstance_ = new ArrayList<ConnInstance>();
            NewInstance();
        }
    }
    
    public static void NewInstance()
    {
        InstanceNo++;
        ConnInstance CI = new ConnInstance(InstanceNo);
        ConnInstance_.add(CI);
        CI.Start();
    }

    public static void ApiAction(KeyValue MessageI, KeyValue MessageO, ConnInstance Instance, String ClientId)
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
                            Thread.sleep(TimeOut);
                        }
                        if (TimeOut < 0)
                        {
                            Thread.sleep(0 - TimeOut);
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
            System.out.println("Instance " + InstanceNo + " - " + ClientId + " - Error msg: " + E.getMessage());
            System.out.println("Instance " + InstanceNo + " - " + ClientId + " - Error type: " + E.getClass().getName());
        }
    }

    public static void CatchError(KeyValue MessageO, Exception E)
    {
        MessageO.ParamSet("Error", E.getClass().getName() + ":" + E.getMessage());
    }
}
