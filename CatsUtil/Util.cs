using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatsUtil;

public class Util
{
    private static readonly Dictionary<char, string> _translate = new() {
            {'а', "a"},{'б', "b"},{'в', "v"},{'г', "g"},{'д', "d"},{'е', "je"},{'ё', "jo"},{'ж', "zh"},{'з', "z"},{'и', "i"},{'й', "j"},{'к', "k"},{'л', "l"},{'м', "m"},{'н', "n"},
            {'о', "o"},{'п', "p"},{'р', "r"},{'с', "s"},{'т', "t"},{'у', "u"},{'ф', "f"},{'х', "kh"},{'ц', "ts"},{'ч', "ch"},{'ш', "sh"},{'щ', "sch"},{'ъ', "``"},{'ы', "y"},{'ь', "`"},
            {'э', "e"},{'ю', "ju"},{'я', "ja"}
        };

    public static string Transliterate(string str)
    {
        StringBuilder sb = new();
        for(int i = 0; i < str.Length; ++i)
        {
            char ch;
            bool upper = Char.IsUpper(str[i]);
            if (upper)
            {
                ch = Char.ToLower(str[i]);
            } else
            {
                ch = str[i];
            }
            string res = _translate.ContainsKey(ch) ? _translate[ch] : new string(new char[] { ch });
            for(int j = 0; j < res.Length; ++j)
            {
                if(j == 0 && upper)
                {
                    sb.Append(res.Substring(j, 1).ToUpper());
                } else
                {
                    sb.Append(res.Substring(j, 1));
                }
            }
        }
        return sb.ToString();
    }
}
