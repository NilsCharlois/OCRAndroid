using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCRAndroid.Models
{
    public class Line
    {
        private List<Letter> _letters;

        public Line()
        {
            _letters = new List<Letter>();
        }

        public void AddLetter(Letter letter)
        {
            _letters.Add(letter);
        }

        public List<Letter> GetLetters()
        {
            return _letters;
        }

    }
}
