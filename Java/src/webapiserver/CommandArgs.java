/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.io.File;
import java.util.ArrayList;

/**
 *
 * @author xxx
 */
public class CommandArgs
{
    static KeyValue CmdArgs;
    static int PortNo = 0;
    static String[] PathMount = new String[26];
    static String RegCmd = "^$"; // "^.*$"
    static String RegNet = "^$"; // "^.*$"
    static String MountAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    static String PathDirSep = "/";
    static boolean PathDirNotSlash = false;

    static String PathCorrect(String PathName, int Sep)
    {
        switch (Sep)
        {
            case 0:
                if (PathName.endsWith("/"))
                {
                    return PathName.substring(0, PathName.length() - 1);
                }
                break;
            case 1:
                if (!PathName.endsWith("/"))
                {
                    return PathName + "/";
                }
                break;
            case 2:
                if (PathName.endsWith(PathDirSep))
                {
                    return PathName.substring(0, PathName.length() - 1);
                }
                break;
            case 3:
                if (!PathName.endsWith(PathDirSep))
                {
                    return PathName + PathDirSep;
                }
                break;
        }
        return PathName;
    }

    public static String PathMountToReal(String MountPath)
    {
        if (MountPath.startsWith("?/"))
        {
            MountPath = MountPath.substring(2);
        }
        if (MountPath.length() == 0)
        {
            return "";
        }
        MountPath = PathCorrect(MountPath, 0);
        if (PathDirNotSlash)
        {
            MountPath = MountPath.replace("/", PathDirSep);
        }
        int Idx = MountAlphabet.indexOf(MountPath.substring(0, 1).toUpperCase());
        if (Idx >= 0)
        {
            if (PathMount[Idx].length() > 0)
            {
                return PathMount[Idx] + MountPath.substring(1);
            }
        }
        return "";
    }

    public static String PathRealToMount(String RealPath, boolean Question)
    {
        RealPath = PathCorrect(RealPath, 2);
        if (RealPath == null)
        {
            return Question ? "?" : "";
        }
        if (RealPath.length() < 2)
        {
            return Question ? "?" : "";
        }
        for (int i = 0; i < PathMount.length; i++)
        {
            if ((PathMount[i].length() > 0) && RealPath.startsWith(PathMount[i]))
            {
                String MountPath = MountAlphabet.substring(i, i + 1) + RealPath.substring(PathMount[i].length());
                if (PathDirNotSlash)
                {
                    return MountPath.replace(PathDirSep, "/");
                }
                else
                {
                    return MountPath;
                }
            }
        }
        return Question ? "?" : "";
    }

    public static String[] PathMountList()
    {
        ArrayList<String> PathMountList_ = new ArrayList<String>();
        for (int i = 0; i < PathMount.length; i++)
        {
            if (PathMount[i].length() > 0)
            {
                PathMountList_.add(MountAlphabet.substring(i, i + 1));
            }
        }
        String[] PathMountList__ = new String[PathMountList_.size()];
        PathMountList_.toArray(PathMountList__);
        return PathMountList__;
    }

    public static void SetArgs(String[] args_)
    {
        String ArgsStr = String.join(" ", args_) + " ";
        
        ArrayList<String> Args = new ArrayList<String>();
        int QuoteState = 0;
        StringBuilder ArgsStrB = new StringBuilder();
        for (int I = 0; I < ArgsStr.length(); I++)
        {
            switch (((int)ArgsStr.charAt(I)) + QuoteState)
            {
                case ' ':
                    if (ArgsStrB.length() > 0)
                    {
                        Args.add(ArgsStrB.toString());
                    }
                    ArgsStrB.delete(0, ArgsStrB.length());
                    break;
                case '\'':
                    QuoteState = 1000;
                    break;
                case '\'' + 1000:
                    if (ArgsStr.charAt(I + 1) == '\'')
                    {
                        ArgsStrB.append('\'');
                        I++;
                    }
                    else
                    {
                        QuoteState = 0;
                    }
                    break;
                case '\"':
                    QuoteState = 2000;
                    break;
                case '\"' + 2000:
                    if (ArgsStr.charAt(I + 1) == '\"')
                    {
                        ArgsStrB.append('\"');
                        I++;
                    }
                    else
                    {
                        QuoteState = 0;
                    }
                    break;
                case '`':
                    QuoteState = 3000;
                    break;
                case '`' + 3000:
                    if (ArgsStr.charAt(I + 1) == '`')
                    {
                        ArgsStrB.append('`');
                        I++;
                    }
                    else
                    {
                        QuoteState = 0;
                    }
                    break;
                default:
                    ArgsStrB.append(ArgsStr.charAt(I));
                    break;
            }
        }

        CmdArgs = new KeyValue();
        for (int I = 0; I < Args.size(); I++)
        {
            CmdArgs.ParamSet(Args.get(I));
            System.out.println(Args.get(I));
        }

        PathDirSep = File.separator;
        PathDirNotSlash = !PathDirSep.equals("/");

        PortNo = CmdArgs.ParamGetI("PORT");
        if (CmdArgs.ParamExists("CMD"))
        {
            RegCmd = CmdArgs.ParamGetS("CMD");
        }
        if (CmdArgs.ParamExists("NET"))
        {
            RegNet = CmdArgs.ParamGetS("NET");
        }
        for (int I = 0; I < 26; I++)
        {
            PathMount[I] = PathCorrect(CmdArgs.ParamGetS(MountAlphabet.substring(I, I + 1)), 2);
        }

        System.out.println();
        System.out.println("Server port: " + String.valueOf(PortNo));
        System.out.println("Command pattern: " + RegCmd);
        System.out.println("Network pattern: " + RegNet);
        for (int I = 0; I < 26; I++)
        {
            if (PathMount[I].length() > 0)
            {
                System.out.print("Mount ");
                System.out.print(MountAlphabet.substring(I, I + 1));
                System.out.print(": ");
                System.out.println(PathMount[I]);
            }
        }
    }
}
