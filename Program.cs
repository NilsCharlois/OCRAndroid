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
using System.Data;

namespace OCRAndroid
{   
    class Program
    {
        /*
         * Ideas : When the first letter of the word is found, create 8 potential directions:
         * delete the ones out of bound based on the word's length
         * delete the ones not followed by the second letter, and so on
         * if nothing found, forget that letter and move on the next occurence of the first letter in the matrix
         */
        private static Letter[][] matrix;
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
            // returns string[] where [0] = Words.png path and [1] = Letters.png path
            string[] imagesPath = SplitMainImage(@".\Images\Screenshot_20200802_171218_com.blackout.word.png");

            if(!System.IO.File.Exists(imagesPath[1].Replace(".png","-out.png"))){
                System.IO.File.Copy(imagesPath[1], imagesPath[1].Replace(".png","-out.png"));
            }
            
            Tuple<List<string>, Letter[][]> result = ProcessImages(imagesPath);
            matrix = result.Item2;
            List<string> WordsToFind = result.Item1.Where(s=>!string.IsNullOrEmpty(s)).ToList();

            Tuple<Letter, Letter> testResult = FindLettersForWord(matrix, "TASTEFUL");
                    
            WordsToFind.ForEach(word=>{
                System.Console.WriteLine($"Processing word: {word}");
                    Tuple<Letter, Letter> finalResult = FindLettersForWord(matrix, word);
                    DrawLine(finalResult, imagesPath[1].Replace(".png","-out.png"));
                    System.Threading.Thread.Sleep(200);
            });
            System.IO.File.Delete(imagesPath[1].Replace("-out.png","-out2.png"));

            Console.Read();
        }

        private static string[] SplitMainImage(string filePath)
        {
            Rectangle words = new Rectangle(){Y = 1360,X = 20,Height=500, Width=1150};
            Rectangle letters = new Rectangle(){Y = 375, X = 55, Height= 965, Width=1090 };
            CropAtRect((Bitmap)Bitmap.FromFile(filePath), words).Save(@".\Images\Words.png", System.Drawing.Imaging.ImageFormat.Png);
            CropAtRect((Bitmap)Bitmap.FromFile(filePath), letters).Save(@".\Images\Letters.png", System.Drawing.Imaging.ImageFormat.Png);
            return new string[]{@".\Images\Words.png",@".\Images\Letters.png"};
        }

        private static Bitmap CropAtRect(Bitmap b, Rectangle r)
        {
            Bitmap nb = new Bitmap(r.Width, r.Height);
            using (Graphics g = Graphics.FromImage(nb))
            {
                g.DrawImage(b, -r.X, -r.Y);
                return nb;
            }
        }

        private static void DrawLine(Tuple<Letter, Letter> result, string filePath)
        {
            // filepath should be like ".\Images\Letters-out.png"
            var img = Bitmap.FromFile(filePath);
            Pen pen = new Pen(Color.Red, 3);
            using(var g = Graphics.FromImage(img))
            {
                g.DrawLine(pen,
                new Point(result.Item1.Rectangle.X+result.Item1.Rectangle.Width/2, result.Item1.Rectangle.Y+result.Item1.Rectangle.Height/2),
                new Point(result.Item2.Rectangle.X+result.Item2.Rectangle.Width/2, result.Item2.Rectangle.Y+result.Item2.Rectangle.Height/2));
            }
            img.Save(filePath.Replace("-out.png","-out2.png"));
            img.Dispose();
            System.IO.File.Delete(filePath);
            System.IO.File.Copy(filePath.Replace("-out.png","-out2.png"), filePath);
        }

        private static Tuple<Letter, Letter> FindLettersForWord(Letter[][] matrix, string word)
        {
            char firstLetter = word[0];
            char secontLetter = word[1];

            List<Letter> firstLetterOccurences = new List<Letter>();

            // first find all occurences of this letter in the matrix
            int rowCount = matrix.GetLength(0);

            int columnCount = matrix[0].Length;

            for(int i = 0; i < rowCount; i++)
            {
                for(int j = 0; j < columnCount; j++)
                {
                    // found one
                    if (matrix[i][j].Value.ToString().ToUpper().Equals(firstLetter.ToString().ToUpper()))
                    {
                        firstLetterOccurences.Add(matrix[i][j]);
                    }
                }
            }

            // we now have the list of all occurences of the first letter of the word
            // for all of them, we need to have a look at the letters around it
            // if the second letter of the word is around it, we save the position and direction

            var allCandidates = FilterBySecondLetterAndGetDirections(firstLetterOccurences, word);

            // filter down the result by checking that the last letter, for a given start letter and a direction is within the matrix
            allCandidates = FilterByLastLetterPosition(matrix, allCandidates, word);

            // for each candidate, follow each direction until the word is found, if found, return the starting and ending letters
            Tuple<Letter, Letter> wordInGrid;

            foreach(KeyValuePair<Letter, List<Directions>> entry in allCandidates){
                foreach(Directions direction in entry.Value)
                {
                    wordInGrid = TestCandidate(matrix, entry.Key, direction, word);
                    if(wordInGrid != null)
                    {
                        return wordInGrid;
                    }
                }
            }
            return null;
        }
        
        private static Dictionary<Letter, List<Directions>> FilterByLastLetterPosition(Letter[][] matrix, Dictionary<Letter, List<Directions>> allCandidatesFilteredBySecondLetter, string word)
        {
            Dictionary<Letter, List<Directions>> output = new Dictionary<Letter, List<Directions>>();
            Tuple<Letter, List<Directions>> result;

            foreach(KeyValuePair<Letter, List<Directions>> entry in allCandidatesFilteredBySecondLetter){
                result = GetDirectionsWithinRange(matrix, entry.Key, entry.Value, word);
                if(result.Item2.Count>0) // we can go to at least one direction
                {
                    if(!output.ContainsKey(entry.Key)) // add new entry to dictionary 
                    {
                        output.Add(entry.Key, new List<Directions>());
                    }
                    // add direction to existing entry in dictionary
                    output[entry.Key].AddRange(result.Item2);                    
                }
            }
            return output;
        }

        private static Tuple<Letter, Letter> TestCandidate(Letter[][] matrix, Letter startLetter, Directions direction, string word){
            int currentX = startLetter.X;
            int currentY = startLetter.Y;
            string str = "";
            for(int i = 0; i<word.Length; i++){
                str += matrix[currentX][currentY].Value;

                if(str.ToLower().Equals(word.ToLower())){
                    return Tuple.Create(startLetter, matrix[currentX][currentY]);
                }

                switch(direction){
                    case Directions.T:
                        currentX--;
                    break;
                    case Directions.TR:
                        currentX--;
                        currentY++;
                    break;
                    case Directions.R:
                        currentY++;
                    break;
                    case Directions.BR:
                        currentY++;
                        currentX++;
                    break;
                    case Directions.B:
                        currentX++;
                    break;
                    case Directions.BL:
                        currentX++;
                        currentY--;
                    break;
                    case Directions.L:
                        currentY--;
                    break;
                    case Directions.TL:
                        currentY--;
                        currentX--;
                    break;
                }
            }
            return null;            
        }

        private static Tuple<Letter, List<Directions>> GetDirectionsWithinRange(Letter[][] matrix, Letter startLetter, List<Directions> directions, string word)
        {
            Tuple<Letter, List<Directions>> output = Tuple.Create(startLetter, new List<Directions>());
            directions.ForEach(item=>{
                if(IsLastLetterInRangeTest(matrix, word.Length, startLetter, item)){
                    output.Item2.Add(item);
                }
            });            
            return output;
        }

        private static bool IsLastLetterInRangeTest(Letter[][] matrix, int wordLength, Letter startLetter, Directions direction){
            try
            {
                switch(direction)
                {
                    case Directions.T: 
                        return (matrix[startLetter.X - (wordLength-1)][startLetter.Y] != null);
                    case Directions.TR:
                        return (matrix[startLetter.X - (wordLength-1)][startLetter.Y + (wordLength-1)] != null);
                    case Directions.R:
                        return (matrix[startLetter.X][startLetter.Y + (wordLength-1)] != null);
                    case Directions.BR:
                        return (matrix[startLetter.X + (wordLength-1)][startLetter.Y + (wordLength-1)] != null);
                    case Directions.B:
                        return (matrix[startLetter.X + (wordLength-1)][startLetter.Y] != null);
                    case Directions.BL:
                        return (matrix[startLetter.X + (wordLength-1)][startLetter.Y - (wordLength-1)] != null);
                    case Directions.L:
                        return (matrix[startLetter.X][startLetter.Y - (wordLength-1)] != null);
                    case Directions.TL:
                        return (matrix[startLetter.X - (wordLength-1)][startLetter.Y - (wordLength-1)] != null);
                    default:
                    return false;
                }
            }
            catch (System.IndexOutOfRangeException)
            {
                return false;
            }
        }
        
        private static Dictionary<Letter, List<Directions>> FilterBySecondLetterAndGetDirections(List<Letter> firstLetterOccurences, string word)
        {
            Dictionary<Letter, List<Directions>> output = new Dictionary<Letter, List<Directions>>();
            List<Directions> tmpDirections = new List<Directions>();
            firstLetterOccurences.ForEach(item=>{
                tmpDirections = GetDirectionsForSecondLetter(matrix, item.X, item.Y, word[1].ToString());
                if(tmpDirections.Count>0) // at least one direction where the second letter is found
                {
                    output.Add(item, tmpDirections);
                }
            });
            return output;
        }
        
        private static Tuple<List<string>, Letter[][]>  ProcessImages(string[] filePaths)
        {
            Pix pix = Pix.LoadFromFile(filePaths[0]);
            TesseractEngine engine = new TesseractEngine(@"C:\Users\Perso\Documents\tessdata", "eng", EngineMode.TesseractOnly);
            
            engine.SetVariable("tessedit_char_blacklist", "0123456789");
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            // get all words
            List<string> allWords = new List<string>();
            engine.DefaultPageSegMode = PageSegMode.Auto;
            string curText = "";

            var page = engine.Process(pix);
            curText = page.GetText();
            allWords.AddRange(curText.Replace("\n",",").Replace(" ",",").Split(",").ToList());

            page.Dispose();

            // get all letters
            pix = Pix.LoadFromFile(filePaths[1]);
            engine.DefaultPageSegMode = PageSegMode.SingleBlock;
            List<List<Letter>> allLetters = new List<List<Letter>>();

            int tmpYPos = 0;
            List<Letter> tmpList = new List<Letter>();
            Letter currentLetter;
            int currentX = 0;
            int currentY = 0;

            Tuple<char, char, float> lowConfidenceRerun;
            
            using (page = engine.Process(pix))
            using (var iter = page.GetIterator())
            {
                iter.Begin();
                do
                {
                    if (iter.TryGetBoundingBox(PageIteratorLevel.Symbol, out var rect))
                    {
                        curText = iter.GetText(PageIteratorLevel.Symbol);
                        
                        System.Console.WriteLine($"confidence {iter.GetConfidence(PageIteratorLevel.Symbol)} for value {curText}, at location {rect.X1}:{rect.Y1}, height:{rect.Height}, width:{rect.Width}, next value {iter.GetChoiceIterator().GetText()}");    

                        if(Math.Abs(rect.Y1-tmpYPos)>5) // letter may not have the sime size, plus if there are more than 10 pixels, we can assume this is a new line
                        //if(rect.Y1 != tmpYPos) // new line
                        {
                            tmpYPos = rect.Y1;
                            allLetters.Add(tmpList);
                            tmpList = new List<Letter>();
                            currentX ++ ;
                            currentY = 0;
                        }
                        currentLetter = new Letter() {
                            Rectangle = new Rectangle(rect.X1, rect.Y1, rect.Width, rect.Height),
                            Value = curText[0],
                            X = currentX-1,
                            Y = currentY++,
                            Confidence = iter.GetConfidence(PageIteratorLevel.Symbol),
                            SecondSuggestion = iter.GetChoiceIterator().GetText()[0]
                        };
                        // if low confidence or first suggestion does not match second suggestion, re-run the engine on the rectangle only
                        if(iter.GetConfidence(PageIteratorLevel.Symbol) < 80f || currentLetter.Value != currentLetter.SecondSuggestion)
                        {
                            lowConfidenceRerun = ReRunForLowConfidence(currentLetter, filePaths[1]);
                            currentLetter.Value = lowConfidenceRerun.Item1;
                            currentLetter.SecondSuggestion = lowConfidenceRerun.Item2;
                            currentLetter.Confidence = lowConfidenceRerun.Item3;
                            System.Console.WriteLine($"After ReRun: confidence {iter.GetConfidence(PageIteratorLevel.Symbol)} for value {curText}, next value {iter.GetChoiceIterator().GetText()}");    
                        }
                        tmpList.Add(currentLetter);
                    }
                } while (iter.Next(PageIteratorLevel.Symbol));
                allLetters.Add(tmpList);
            }
            return Tuple.Create(allWords, allLetters.Skip(1).Select(a=>a.ToArray()).ToArray());
        }

        private static Tuple<char,char,float> ReRunForLowConfidence(Letter letterWithLowConfidence, string letterFilePath)
        {
            CropAtRect((Bitmap)Bitmap.FromFile(letterFilePath), new Rectangle(){
                Height = letterWithLowConfidence.Rectangle.Height+5,
                Width = letterWithLowConfidence.Rectangle.Width+5,
                X = letterWithLowConfidence.Rectangle.X-5,
                Y = letterWithLowConfidence.Rectangle.Y-5,
            }).Save(@".\Images\TmpLetterReRun.png");

            TesseractEngine engine = new TesseractEngine(@"C:\Users\Perso\Documents\tessdata", "eng", EngineMode.TesseractOnly);
            
            engine.SetVariable("tessedit_char_blacklist", "0123456789");
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            Pix pix = Pix.LoadFromFile(@".\Images\Test.png");
            engine.DefaultPageSegMode = PageSegMode.SingleBlock;

            using (var page = engine.Process(pix))
            using (var iter = page.GetIterator())
            {
                iter.Begin();
                do
                {
                    if (iter.TryGetBoundingBox(PageIteratorLevel.Symbol, out var rect))
                    {
                        System.Console.WriteLine($"confidence {iter.GetConfidence(PageIteratorLevel.Symbol)} for value {iter.GetText(PageIteratorLevel.Symbol)}, next value {iter.GetChoiceIterator().GetText()}");    
                        return Tuple.Create(iter.GetText(PageIteratorLevel.Symbol)[0], iter.GetChoiceIterator().GetText()[0], iter.GetConfidence(PageIteratorLevel.Symbol));
                    }
                } while (iter.Next(PageIteratorLevel.Symbol));
            }
            
            System.IO.File.Delete(@".\Images\TmpLetterReRun.png");
            return null;            
        }

        private static List<Directions> GetDirectionsForSecondLetter(Letter[][] matrix, int i, int j, string letterToFind)
        {
            List<Directions> directions = new List<Directions>();

            // test top
            try
            {
                if (matrix[i - 1][j] != null && (matrix[i - 1][j].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.T);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            {}

            // test topRight
            try
            {
                if (matrix[i - 1][j + 1] != null && (matrix[i - 1][j + 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.TR);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            {}


            // test right
            try
            {
                if (matrix[i][j + 1] != null && (matrix[i][j + 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.R);                
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            {}


            // test bottom right
            try
            {
                if (matrix[i + 1][j + 1] != null && (matrix[i + 1][j + 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.BR);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            {}

            // test bottom
            try
            {
                if (matrix[i + 1][j] != null && (matrix[i + 1][j].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.B);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            {}

            // test bottom left
            try
            {
                if (matrix[i + 1][j - 1] != null && (matrix[i + 1][j - 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.BL);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            {}

            // test left
            try
            {
                if (matrix[i][j - 1] != null && (matrix[i][j - 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.L);
            }
            catch (Exception) // we may be out of bound but we don't want to raise it
            {}

            // test top left
            try
            {
                if (matrix[i - 1][j - 1] != null && (matrix[i - 1][j - 1].Value.ToString().ToUpper().Equals(letterToFind.ToUpper())))
                    directions.Add(Directions.TL);
            }
            catch (Exception)  // we may be out of bound but we don't want to raise it
            {}
            
            return directions;
        }
    }
}
