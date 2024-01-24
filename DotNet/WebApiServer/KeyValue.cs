using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace WebApiServer
{
    public class KeyValue
    {
        static List<KeyValue> PoolObj = new List<KeyValue>();
        static Mutex PoolMtx = new Mutex();

        public static KeyValue PoolAssign()
        {
            PoolMtx.WaitOne();
            KeyValue KV;
            for (int i = 0; i < PoolObj.Count; i++)
            {
                if (PoolObj[i].Free)
                {
                    KV = PoolObj[i];
                    KV.Free = false;
                    PoolMtx.ReleaseMutex();
                    return KV;
                }
            }
            KV = new KeyValue();
            PoolObj.Add(KV);
            PoolMtx.ReleaseMutex();
            return KV;
        }

        public static void PoolRelease(KeyValue KV)
        {
            PoolMtx.WaitOne();
            int KVI = PoolObj.IndexOf(KV);
            if (KVI >= 0)
            {
                PoolObj[KVI].Free = true;
            }
            PoolMtx.ReleaseMutex();
        }

        private bool Free = false;

        private Dictionary<string, string> Raw = new Dictionary<string, string>();

        public int CaseMode = 0;

        private string Case(string S)
        {
            if (CaseMode > 0)
            {
                return S.ToUpperInvariant();
            }
            if (CaseMode < 0)
            {
                return S.ToLowerInvariant();
            }
            return S;
        }

        public static string BinaryEncode(byte[] Data)
        {
            return Convert.ToBase64String(Data);
        }

        public static byte[] BinaryDecode(string Data)
        {
            return Convert.FromBase64String(Data);
        }

        public static string StringEncode(string Data)
        {
            return BinaryEncode(Encoding.UTF8.GetBytes(Data));
        }

        public static string StringDecode(string Data)
        {
            return Encoding.UTF8.GetString(BinaryDecode(Data));
        }

        public void FileLoad(string FileName)
        {
            ParamClear();
            try
            {
                FileStream F_ = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                StreamReader F = new StreamReader(F_);
                while (!F.EndOfStream)
                {
                    string S = F.ReadLine();
                    int I = S.IndexOf("=");
                    if (I >= 0)
                    {
                        string RawK = Case(S.Substring(0, I));
                        if (!Raw.ContainsKey(RawK))
                        {
                            if (S.Length > (I + 1))
                            {
                                Raw.Add(RawK, S.Substring(I + 1));
                            }
                            else
                            {
                                Raw.Add(RawK, "");
                            }
                        }
                    }
                }
                F.Close();
                F_.Close();
            }
            catch
            {

            }
        }

        public void StringLoad(string Data)
        {
            ParamClear();
            string[] Data_ = Data.Split('\n');
            for (int II = 0; II < Data_.Length; II++)
            {
                string S = Data_[II];
                int I = S.IndexOf("=");
                if (I >= 0)
                {
                    string RawK = Case(S.Substring(0, I));
                    if (!Raw.ContainsKey(RawK))
                    {
                        if (S.Length > (I + 1))
                        {
                            Raw.Add(RawK, S.Substring(I + 1));
                        }
                        else
                        {
                            Raw.Add(RawK, "");
                        }
                    }
                }
            }
        }

        public void FileSave(string FileName)
        {
            try
            {
                FileStream F_ = new FileStream(FileName, FileMode.Create, FileAccess.Write);
                StreamWriter F = new StreamWriter(F_);
                foreach (KeyValuePair<string, string> item in Raw)
                {
                    F.Write(Case(item.Key));
                    F.Write("=");
                    F.Write(item.Value);
                    F.WriteLine();
                }
                F.Close();
                F_.Close();
            }
            catch
            {

            }
        }

        public string StringSave()
        {
            StringBuilder SB = new StringBuilder();
            foreach (KeyValuePair<string, string> item in Raw)
            {
                SB.Append(Case(item.Key));
                SB.Append("=");
                SB.Append(item.Value);
                SB.Append("\n");
            }
            return SB.ToString();
        }

        public string Print()
        {
            string S = "";
            foreach (KeyValuePair<string, string> item in Raw)
            {
                S = S + Case(item.Key);
                S = S + "=";
                S = S + item.Value;
                S = S + Environment.NewLine;
            }
            return S;
        }

        public void ParamClear()
        {
            Raw.Clear();
        }

        public void ParamRemove(string Name)
        {
            if (Raw.ContainsKey(Case(Name)))
            {
                Raw.Remove(Case(Name));
            }
        }

        public void ParamSet(string NameValue)
        {
            int I = NameValue.IndexOf("=");
            if (I >= 0)
            {
                string RawK = Case(NameValue.Substring(0, I));
                if (!Raw.ContainsKey(RawK))
                {
                    if (NameValue.Length > (I + 1))
                    {
                        Raw.Add(RawK, NameValue.Substring(I + 1));
                    }
                    else
                    {
                        Raw.Add(RawK, "");
                    }
                }
            }
        }

        public void ParamSet(string Name, string Value)
        {
            if (Raw.ContainsKey(Case(Name)))
            {
                Raw[Case(Name)] = Value;
            }
            else
            {
                Raw.Add(Case(Name), Value);
            }
        }

        public void ParamSet(string Name, int Value)
        {
            ParamSet(Name, Value.ToString());
        }

        public void ParamSet(string Name, long Value)
        {
            ParamSet(Name, Value.ToString());
        }

        public void ParamSet(string Name, bool Value)
        {
            ParamSet(Name, Value ? "1" : "0");
        }

        public void ParamSet(string Name, byte[] Value)
        {
            ParamSet(Name, BinaryEncode(Value));
        }

        public bool ParamGet(string Name, ref string Value)
        {
            if (Raw.ContainsKey(Case(Name)))
            {
                Value = Raw[Case(Name)];
                return true;
            }
            return false;
        }

        public bool ParamGet(string Name, ref int Value)
        {
            if (Raw.ContainsKey(Case(Name)))
            {
                try
                {
                    Value = int.Parse(Raw[Case(Name)]);
                    return true;
                }
                catch
                {

                }
            }
            return false;
        }

        public bool ParamGet(string Name, ref long Value)
        {
            if (Raw.ContainsKey(Case(Name)))
            {
                try
                {
                    Value = long.Parse(Raw[Case(Name)]);
                    return true;
                }
                catch
                {

                }
            }
            return false;
        }

        public bool ParamGet(string Name, ref bool Value)
        {
            if (Raw.ContainsKey(Case(Name)))
            {
                switch (Raw[Case(Name)].ToUpperInvariant())
                {
                    case "1":
                    case "TRUE":
                    case "YES":
                    case "T":
                    case "Y":
                        Value = true;
                        return true;
                    case "0":
                    case "FALSE":
                    case "NO":
                    case "F":
                    case "N":
                        Value = false;
                        return true;
                }
            }
            return false;
        }

        public string ParamGetS(string Name, string X)
        {
            ParamGet(Name, ref X);
            return X;
        }

        public int ParamGetI(string Name, int X)
        {
            ParamGet(Name, ref X);
            return X;
        }

        public long ParamGetL(string Name, long X)
        {
            ParamGet(Name, ref X);
            return X;
        }

        public bool ParamGetB(string Name, bool X)
        {
            ParamGet(Name, ref X);
            return X;
        }

        public byte[] ParamGetD(string Name, byte[] X)
        {
            return BinaryDecode(ParamGetS(Name, BinaryEncode(X)));
        }

        public string ParamGetS(string Name)
        {
            string X = "";
            ParamGet(Name, ref X);
            return X;
        }

        public int ParamGetI(string Name)
        {
            int X = 0;
            ParamGet(Name, ref X);
            return X;
        }

        public long ParamGetL(string Name)
        {
            long X = 0;
            ParamGet(Name, ref X);
            return X;
        }

        public bool ParamGetB(string Name)
        {
            bool X = false;
            ParamGet(Name, ref X);
            return X;
        }

        public byte[] ParamGetD(string Name)
        {
            return BinaryDecode(ParamGetS(Name));
        }

        public bool ParamExists(string Name)
        {
            return Raw.ContainsKey(Case(Name));
        }
    }
}
