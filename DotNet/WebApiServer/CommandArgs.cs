using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebApiServer
{
    public class CommandArgs
    {
        static KeyValue CmdArgs;
        public static int PortNo = 0;
        public static string[] PathMount = new string[26];
        public static string RegCmd = "^$"; // "^.*$"
        public static string RegNet = "^$"; // "^.*$"
        static string MountAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        static string PathDirSep = "/";
        static bool PathDirNotSlash = false;

        static string PathCorrect(string PathName, int Sep)
        {
            switch (Sep)
            {
                case 0:
                    if (PathName.EndsWith("/"))
                    {
                        return PathName.Substring(0, PathName.Length - 1);
                    }
                    break;
                case 1:
                    if (!PathName.EndsWith("/"))
                    {
                        return PathName + "/";
                    }
                    break;
                case 2:
                    if (PathName.EndsWith(PathDirSep))
                    {
                        return PathName.Substring(0, PathName.Length - 1);
                    }
                    break;
                case 3:
                    if (!PathName.EndsWith(PathDirSep))
                    {
                        return PathName + PathDirSep;
                    }
                    break;
            }
            return PathName;
        }

        public static string PathMountToReal(string MountPath)
        {
            if (MountPath.StartsWith("?/"))
            {
                MountPath = MountPath.Substring(2);
            }
            if (MountPath.Length == 0)
            {
                return "";
            }
            MountPath = PathCorrect(MountPath, 0);
            if (PathDirNotSlash)
            {
                MountPath = MountPath.Replace("/", PathDirSep);
            }
            int Idx = MountAlphabet.IndexOf(MountPath.Substring(0, 1).ToUpperInvariant());
            if (Idx >= 0)
            {
                if (PathMount[Idx].Length > 0)
                {
                    return PathMount[Idx] + MountPath.Substring(1);
                }
            }
            return "";
        }

        public static string PathRealToMount(string RealPath, bool Question)
        {
            RealPath = PathCorrect(RealPath, 2);
            if (RealPath == null)
            {
                return Question ? "?" : "";
            }
            if (RealPath.Length < 2)
            {
                return Question ? "?" : "";
            }
            for (int i = 0; i < PathMount.Length; i++)
            {
                if ((PathMount[i].Length > 0) && RealPath.StartsWith(PathMount[i]))
                {
                    string MountPath = MountAlphabet.Substring(i, 1) + RealPath.Substring(PathMount[i].Length);
                    if (PathDirNotSlash)
                    {
                        return MountPath.Replace(PathDirSep, "/");
                    }
                    else
                    {
                        return MountPath;
                    }
                }
            }
            return Question ? "?" : "";
        }

        public static string[] PathMountList()
        {
            List<string> PathMountList_ = new List<string>();
            for (int i = 0; i < PathMount.Length; i++)
            {
                if (PathMount[i].Length > 0)
                {
                    PathMountList_.Add(MountAlphabet.Substring(i, 1));
                }
            }
            return PathMountList_.ToArray();
        }

        public static void SetArgs(string[] args_)
        {
            string ArgsStr = string.Join(" ", args_) + " ";

            List<string> Args = new List<string>();
            int QuoteState = 0;
            StringBuilder ArgsStrB = new StringBuilder();
            for (int I = 0; I < ArgsStr.Length; I++)
            {
                switch (((int)ArgsStr[I]) + QuoteState)
                {
                    case ' ':
                        if (ArgsStrB.Length > 0)
                        {
                            Args.Add(ArgsStrB.ToString());
                        }
                        ArgsStrB.Clear();
                        break;
                    case '\'':
                        QuoteState = 1000;
                        break;
                    case '\'' + 1000:
                        if (ArgsStr[I + 1] == '\'')
                        {
                            ArgsStrB.Append('\'');
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
                        if (ArgsStr[I + 1] == '\"')
                        {
                            ArgsStrB.Append('\"');
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
                        if (ArgsStr[I + 1] == '`')
                        {
                            ArgsStrB.Append('`');
                            I++;
                        }
                        else
                        {
                            QuoteState = 0;
                        }
                        break;
                    default:
                        ArgsStrB.Append(ArgsStr[I]);
                        break;
                }
            }

            CmdArgs = new KeyValue();
            for (int I = 0; I < Args.Count; I++)
            {
                CmdArgs.ParamSet(Args[I]);
                Console.WriteLine(Args[I]);
            }

            PathDirSep = Path.DirectorySeparatorChar.ToString();
            PathDirNotSlash = !PathDirSep.Equals("/");

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
                PathMount[I] = PathCorrect(CmdArgs.ParamGetS(MountAlphabet.Substring(I, 1)), 2);
            }

            Console.WriteLine();
            Console.WriteLine("Server port: " + PortNo.ToString());
            Console.WriteLine("Command pattern: " + RegCmd);
            Console.WriteLine("Network pattern: " + RegNet);
            for (int I = 0; I < 26; I++)
            {
                if (PathMount[I].Length > 0)
                {
                    Console.Write("Mount ");
                    Console.Write(MountAlphabet.Substring(I, 1));
                    Console.Write(": ");
                    Console.WriteLine(PathMount[I]);
                }
            }
        }
    }
}
