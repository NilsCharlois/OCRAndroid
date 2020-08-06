using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OCRAndroid.Models
{
    public class Letter
    {
        public Rectangle Rectangle { get; set; }
        public char Value { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public float Confidence { get; set; }
        public char SecondSuggestion { get; set; }

        public Letter()
        {

        }
    }
}
