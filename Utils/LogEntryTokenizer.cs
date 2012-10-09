using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Assimilated.Alfac.Utils
{
    public class LogEntryTokenizer
    {
        private readonly Regex tokenizer;

        public LogEntryTokenizer(string regexPattern)
        {
            tokenizer = new Regex(regexPattern);
        }

        public GroupCollection Tokenize(string entry)
        {
            var res = tokenizer.Match(entry);
            return res.Groups;
        }
    }
}
