using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WebApiServer
{
    public class RegexTest
    {
        static string TestPattern;
        static List<string> TestCases;

        public static string ValidPattern(string Pattern_)
        {
            try
            {
                Regex.Match("", Pattern_);
                return "";
            }
            catch (Exception E)
            {
                return E.Message;
            }
        }

        public static bool Match(string Case, string Pattern_)
        {
            try
            {
                return Regex.Match(Case, Pattern_).Success;
            }
            catch (Exception E)
            {
                return false;
            }
        }

        static void PrintTest()
        {
            Console.WriteLine("Pattern: " + TestPattern);
            string PatternError = ValidPattern(TestPattern);
            if (PatternError.Equals(""))
            {
                for (int i = 0; i < TestCases.Count; i++)
                {
                    if (Match(TestCases[i], TestPattern))
                    {
                        Console.WriteLine("Pass: " + TestCases[i]);
                    }
                    else
                    {
                        Console.WriteLine("Fail: " + TestCases[i]);
                    }
                }
            }
            else
            {
                Console.WriteLine("Pattern error: " + PatternError);
            }
        }

        static void Info()
        {
            Console.WriteLine("00 - Exit");
            Console.WriteLine("01 - Clear test cases");
            Console.WriteLine("02 - Use CMD as pattern");
            Console.WriteLine("03 - Use NET as pattern");
            Console.WriteLine("1 - Set pattern");
            Console.WriteLine("2 - Add test case");
            Console.WriteLine("3 - Remove test case");
        }

        public static void Start()
        {
            TestPattern = "";
            TestCases = new List<string>();
            bool Work = true;
            Info();
            while (Work)
            {
                string Cmd = Console.ReadLine();
                switch (Cmd)
                {
                    case "":
                        Info();
                        break;
                    case "00":
                        Work = false;
                        break;
                    case "01":
                        TestCases.Clear();
                        PrintTest();
                        break;
                    case "02":
                        TestPattern = CommandArgs.RegCmd;
                        PrintTest();
                        break;
                    case "03":
                        TestPattern = CommandArgs.RegNet;
                        PrintTest();
                        break;
                }
                if (Cmd.Length >= 1)
                {
                    switch (Cmd[0])
                    {
                        case '1':
                            TestPattern = Cmd.Substring(1);
                            PrintTest();
                            break;
                        case '2':
                            if (!TestCases.Contains(Cmd.Substring(1)))
                            {
                                TestCases.Add(Cmd.Substring(1));
                                TestCases.Sort();
                            }
                            PrintTest();
                            break;
                        case '3':
                            if (TestCases.Contains(Cmd.Substring(1)))
                            {
                                TestCases.RemoveAt(TestCases.IndexOf(Cmd.Substring(1)));
                            }
                            PrintTest();
                            break;
                    }
                }
            }
        }
    }
}
