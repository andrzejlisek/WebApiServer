/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package webapiserver;

import java.util.ArrayList;
import java.util.Collections;
import java.util.regex.Pattern;

/**
 *
 * @author xxx
 */
public class RegexTest
{
    static String TestPattern;
    static ArrayList<String> TestCases;

    public static String ValidPattern(String Pattern_)
    {
        try
        {
            Pattern RegExP = Pattern.compile(Pattern_);
            return "";
        }
        catch (Exception E)
        {
            return E.getMessage();
        }
    }

    public static boolean Match(String Case, String Pattern_)
    {
        try
        {
            Pattern RegExP = Pattern.compile(Pattern_);
            return RegExP.matcher(Case).find();
        }
        catch (Exception E)
        {
            return false;
        }
    }

    static void PrintTest()
    {
        System.out.println("Pattern: " + TestPattern);
        String PatternError = ValidPattern(TestPattern);
        if (PatternError.equals(""))
        {
            for (int i = 0; i < TestCases.size(); i++)
            {
                if (Match(TestCases.get(i), TestPattern))
                {
                    System.out.println("Pass: " + TestCases.get(i));
                }
                else
                {
                    System.out.println("Fail: " + TestCases.get(i));
                }
            }
        }
        else
        {
            System.out.println("Pattern error: " + PatternError);
        }
    }

    static void Info()
    {
        System.out.println("00 - Exit");
        System.out.println("01 - Clear test cases");
        System.out.println("02 - Use CMD as pattern");
        System.out.println("03 - Use NET as pattern");
        System.out.println("1 - Set pattern");
        System.out.println("2 - Add test case");
        System.out.println("3 - Remove test case");
    }

    public static void Start()
    {
        TestPattern = "";
        TestCases = new ArrayList<String>();
        boolean Work = true;
        Info();
        while (Work)
        {
            String Cmd = System.console().readLine();
            switch (Cmd)
            {
                case "":
                    Info();
                    break;
                case "00":
                    Work = false;
                    break;
                case "01":
                    TestCases.clear();
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
            if (Cmd.length() >= 1)
            {
                switch (Cmd.charAt(0))
                {
                    case '1':
                        TestPattern = Cmd.substring(1);
                        PrintTest();
                        break;
                    case '2':
                        if (!TestCases.contains(Cmd.substring(1)))
                        {
                            TestCases.add(Cmd.substring(1));
                            Collections.sort(TestCases);
                        }
                        PrintTest();
                        break;
                    case '3':
                        if (TestCases.contains(Cmd.substring(1)))
                        {
                            TestCases.remove(TestCases.indexOf(Cmd.substring(1)));
                        }
                        PrintTest();
                        break;
                }
            }
        }
    }
}
