using System;
using OCRAndroid.Models;

using Tesseract;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;

namespace OCRAndroid
{   
    class Program
    {
        /*
         * Test Matrix is 
         * B N S S M T
         * T P A R A B
         * I I N B A U
         * 
         * 
         * Ideas : When the first letter of the word is found, create 8 potential directions:
         * delete the ones out of bound based on the word's length (ie: word's length > letter position + array bounds)
         * delete the ones not followed by the second letter, and so on
         * if nothing found, forget that letter and move on the next occurence of the first letter in the matrix
         */
        private static List<Letter> AllLetters;
        private static Letter[,] matrix;
        private static string testWord = "arab";
        public enum Directions
        {
            T, // -1,0
            TR, // -1,1
            R, // 0,1
            BR, // 1,1
            B, // 1,0
            BL, // 1,-1
            L, // 0,-1
            TL, // -1,-1
            None
        }
        static void Main(string[] args)
        {
            // get all the letters from recognition library
            AllLetters = GetAllLetters();
            AllLetters.ForEach(item => {
                Console.WriteLine($"Letter: {item.Value} Rectangle: TopLeft: {item.Rectangle.X} {item.Rectangle.Y}, BottomRight {item.Rectangle.X + item.Rectangle.Width} {item.Rectangle.Y + item.Rectangle.Height}");
            });
            // transform to matrix so we can search a letter surrounding another letter
            matrix = TransformListToMatrix(AllLetters);

            try
            {
                List<Directions> testNull = IsLetterAroundPosition(matrix, 0, 0, "a"); // should raise an exception
                
                List<Directions> testTop = IsLetterAroundPosition(matrix, 1, 1, "n"); // should return -1,0 : top
                List<Directions> testTopRight = IsLetterAroundPosition(matrix, 1, 1, "s"); // should return -1,1: top right
                List<Directions> testRight = IsLetterAroundPosition(matrix, 1, 1, "a"); // should return 0,1: right
                List<Directions> testBottomRight = IsLetterAroundPosition(matrix, 0, 2, "r"); // should return 1,1 bottom right
                List<Directions> testBottom = IsLetterAroundPosition(matrix, 1, 2, "n"); // should return 1,0: bottom
                List<Directions> testBottomLeft = IsLetterAroundPosition(matrix, 1, 2, "i"); // should return 1,-1: bottom left
                List<Directions> testLeft = IsLetterAroundPosition(matrix, 1, 1, "t"); // should return 0,-1: left
                List<Directions> testTopLeft = IsLetterAroundPosition(matrix, 1, 1, "b"); // should return -1,-1: top left

                List<Directions> DirectionsForA = IsLetterAroundPosition(matrix, 1, 3, "a"); // should return 3 directions    
            }
            catch (System.Exception)
            {
                
                throw;
            }
            SolveThisShit(testWord);


            Console.Read();
        }


        private static Tuple<Letter, Letter> SolveThisShit(string word)
        {
            char firstLetter = testWord[0];
            char secontLetter = testWord[1];

            List<Letter> firstLetterOccurences = new List<Letter>();

            // first find all occurences of this letter in the matrix
            int rowCount = matrix.GetLength(0);
            int columnCount = matrix.GetLength(1);

            for(int i = 0; i < rowCount; i++)
            {
                for(int j = 0; j < columnCount; j++)
                {
                    // found one
                    if (matrix[i, j].Value.ToString().ToUpper().Equals(firstLetter.ToString().ToUpper()))
                    {
                        firstLetterOccurences.Add(matrix[i,j]);
                    }
                }
            }

            // we now have the list of all occurences of the first letter of the word
            // for all of them, we need to have a look at the letters around it
            // if the second letter of the word is around it, we save the position and direction

            var allCandidates = FilterBySecondLetterAndFindDirections(firstLetterOccurences);

            // filter down the result by checking that the last letter, for a given start letter and a direction is within the matrix
            allCandidates = FilterByLastLetterPosition(allCandidates, word);

            // for each candidate, follow each direction until the word is found, if found, return the starting and ending letters
            Tuple<Letter, Letter> startToEnd = FindWord(allCandidates);


            return null;
        }

        private static Tuple<Letter,Letter> FindWord(Dictionary<Letter, List<Directions>> allCandidates)
        {
            Dictionary<Letter, List<Directions>> output = new Dictionary<Letter, List<Directions>>();

            
            foreach(KeyValuePair<Letter, List<Directions>> entry in allCandidates){

            }
            return null;
        }
        

        private static Dictionary<Letter, List<Directions>> FilterByLastLetterPosition(Dictionary<Letter, List<Directions>> allCandidates, string word)
        {
            Dictionary<Letter, List<Directions>> output = new Dictionary<Letter, List<Directions>>();
            Tuple<Letter, List<Directions>> result;

            foreach(KeyValuePair<Letter, List<Directions>> entry in allCandidates){
                result = GetDirectionsWithinRange(entry.Key, entry.Value, word);
            }

            return null;
        }

        private static Tuple<Letter, List<Directions>> GetDirectionsWithinRange(Letter startLetter, List<Directions> directions, string word)
        {
            Tuple<Letter, List<Directions>> output = Tuple.Create(startLetter, new List<Directions>());
            directions.ForEach(item=>{
                if(IsLastLetterInRange(word, startLetter, item)){
                    output.Item2.Add(item);
                }
            });
        }

        private static bool IsLastLetterInRange(string word, Letter startLetter, Directions direction){
            return false;
        }

        private static Dictionary<Letter, List<Directions>> FilterBySecondLetterAndFindDirections(List<Letter> firstLetterOccurences)
        {
            Dictionary<Letter, List<Directions>> output = new Dictionary<Letter, List<Directions>>();
            List<Directions> tmpDirections = new List<Directions>();
            firstLetterOccurences.ForEach(item=>{
                tmpDirections = IsLetterAroundPosition(matrix, item.X, item.Y, testWord[1].ToString());
                if(tmpDirections.Count>0) // at least one direction where the second letter is found
                {
                    output.Add(item, tmpDirections);
                }
            });
            return output;
        }

        private static List<Letter> GetAllLetters()
        {
            Pix pix = Pix.LoadFromFile(@".\Images\Letters - Copy.png");
            TesseractEngine engine = new TesseractEngine(@"C:\Users\nils.charlois\Downloads\tessdata", "eng", EngineMode.Default);
            engine.SetVariable("tessedit_char_blacklist", "0123456789");
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            engine.DefaultPageSegMode = PageSegMode.SingleBlock;
            
            List<Letter> AllLetters = new List<Letter>();

            using (var page = engine.Process(pix))
            using (var iter = page.GetIterator())
            {
                iter.Begin();
                do
                {
                    if (iter.TryGetBoundingBox(PageIteratorLevel.Symbol, out var rect))
                    {
                        var curText = iter.GetText(PageIteratorLevel.Symbol);
                        
                        AllLetters.Add(new Letter() {
                            Rectangle = new Rectangle(rect.X1, rect.Y1, rect.Width, rect.Height),
                            Value = curText[0],
                        });
                    }
                } while (iter.Next(PageIteratorLevel.Symbol));
            }
            return AllLetters;
        }


        private static Letter[,] TransformListToMatrix(List<Letter> toTransform)
        {
            // used to identify a line
            int currentTopY = toTransform.First().Rectangle.Y;

            List<Line> AllLines = new List<Line>();
            Line currentLine = new Line();
            toTransform.OrderBy(o=>o.Rectangle.Y).ThenBy(o=>o.Rectangle.X).ToList().ForEach(item => {
                if(currentTopY == item.Rectangle.Y)
                {
                    currentLine.AddLetter(item);
                }
                else
                {
                    AllLines.Add(currentLine);
                    currentTopY = item.Rectangle.Y;
                    currentLine = new Line();
                    currentLine.AddLetter(item);
                }
            });
            // add last line
            AllLines.Add(currentLine);
            Letter[,] allLetters = new Letter[AllLines.Count, AllLines[0].GetLetters().Count]; // assuming we have a square/rectangle, not weird shapes
            Letter currentLetter;
            for (int i = 0; i<AllLines.Count; i++)
            {
                for(int j = 0; j< AllLines[i].GetLetters().Count; j++)
                {
                    currentLetter = new Letter();
                    currentLetter = AllLines[i].GetLetters()[j];
                    currentLetter.X = i;
                    currentLetter.Y = j;
                    allLetters[i,j] = currentLetter;
                }
            }
            return allLetters;
        }
        
        private static List<Directions> IsLetterAroundPosition(Letter[,] matrix, int i, int j, string letterToFind)
        {
            List<Directions> directions = new List<Directions>();

            if(i > matrix.GetLength(0) || j > matrix.GetLength(1))
            {
                throw new Exception("Central position out of bound");
            }

            // test top
            try
            {
                if (matrix[i - 1, j] != null && (matrix[i - 1, j].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.T);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();
            }

            // test topRight
            try
            {
                if (matrix[i - 1, j + 1] != null && (matrix[i - 1, j + 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.TR);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();;
            }


            // test right
            try
            {
                if (matrix[i, j + 1] != null && (matrix[i, j + 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.R);                
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();;
            }


            // test bottom right
            try
            {
                if (matrix[i + 1, j + 1] != null && (matrix[i + 1, j + 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.BR);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();;
            }

            // test bottom
            try
            {
                if (matrix[i + 1, j] != null && (matrix[i + 1, j].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.B);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();;
            }

            // test bottom left
            try
            {
                if (matrix[i + 1, j - 1] != null && (matrix[i + 1, j - 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.BL);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();;
            }

            // test left
            try
            {
                if (matrix[i, j - 1] != null && (matrix[i, j - 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.L);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();;
            }

            // test top left
            try
            {
                if (matrix[i - 1, j - 1] != null && (matrix[i - 1, j - 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.TL);
            }
            catch (Exception)  // we may be out of bound but we don't want to raise it
            { 
                System.Console.WriteLine();;
            }
            
            return directions;
        }
    }
}
