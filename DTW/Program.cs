using System.Threading;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDtw;
using OxyPlot;
using OxyPlot.WindowsForms;

namespace DTW
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("1 = raw\n2 = csv");
            if (Console.ReadLine() == "1")
            {
                DTWRaw(Directory.GetDirectories(Directory.GetCurrentDirectory()));
            }
            else
            {
                foreach (string dir in Directory.GetDirectories(Directory.GetCurrentDirectory()))
                {
                    DTWCsv(Directory.GetFiles(dir).Where(x => x.EndsWith(".csv")).ToArray());
                }
            }


            Console.ReadLine();
        }

        static void DTWRaw(string[] folders)
        {

            for (int folderId = 0; folderId < folders.Length; folderId++)
            {
                Console.WriteLine("Performing DTW on " + folderId + " of " + folders.Length + " subjects..");

                string[] testData = File.ReadAllLines(folders[folderId] + "/test.dat");
                string[] recallData = File.ReadAllLines(folders[folderId] + "/recall.dat");

                List<Tuple<int, int>> testDataPoints = new List<Tuple<int, int>>();
                List<Tuple<int, int>> recallDataPoints = new List<Tuple<int, int>>();

                Console.WriteLine("Parsing files..");
                foreach (var line in testData)
                {
                    int x, y;

                    var split = line.Split('#');

                    if (int.TryParse(split[0], out x) && int.TryParse(split[1], out y))
                    {
                        testDataPoints.Add(Tuple.Create(x, y));
                    }
                }

                foreach (var line in recallData)
                {
                    int x, y;

                    var split = line.Split('#');

                    if (int.TryParse(split[0], out x) && int.TryParse(split[1], out y))
                    {
                        recallDataPoints.Add(Tuple.Create(x, y));
                    }
                }

                Console.WriteLine("Doing Median filter");
                testDataPoints = testDataPoints.MedianFilter(25, (x) => x.Item2).OrderBy(x => x.Item1).ToList();
                recallDataPoints = recallDataPoints.MedianFilter(25, (x) => x.Item2).OrderBy(x => x.Item1).ToList();

                Console.WriteLine("Computing DTW (" + testData.Length + " x " + recallData.Length + "), this will take a while");
                Dtw dtw = new Dtw(testDataPoints.Select(p => (double)p.Item2).ToArray(), recallDataPoints.Select(p => (double)p.Item2).ToArray(), DistanceMeasure.Euclidean, true, true, 1, 1);

                var path = dtw.GetPath();

                Console.WriteLine("Done DTW'ing, generating img");

                PngExporter pngify = new PngExporter();
                pngify.Width = 36000;
                pngify.Height = 4000;

                var model = new PlotModel() { Title = "Red = test, blue = recall" };

                var aSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Blue, MarkerSize = 10 };
                var bSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Red, MarkerSize = 10 };

                foreach (var item in testDataPoints)
                {
                    aSeries.Points.Add(new DataPoint(item.Item1, item.Item2));
                }

                foreach (var item in recallDataPoints)
                {
                    bSeries.Points.Add(new DataPoint(item.Item1, item.Item2));
                }

                List<string> pearsonData = new List<string>();
                foreach (var pairing in path)
                {
                    var lineSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Gray, MarkerSize = 0.05 };

                    lineSeries.Points.Add(new DataPoint(testDataPoints[pairing.Item1].Item1, (testDataPoints[pairing.Item1].Item2)));
                    lineSeries.Points.Add(new DataPoint(recallDataPoints[pairing.Item2].Item1, (recallDataPoints[pairing.Item2].Item2)));

                    model.Series.Add(lineSeries);

                    pearsonData.Add(testDataPoints[pairing.Item1].Item2 + ";" + recallDataPoints[pairing.Item2].Item2);
                }

                File.WriteAllLines(folders[folderId] + "/pearsonDataRaw.txt", pearsonData);

                model.Series.Add(aSeries);
                model.Series.Add(bSeries);

                pngify.ExportToFile(model, "raw.png");
            }

            Console.WriteLine("DonnoDK");
        }

        static void DTWCsv(string[] files)
        {

            for (int fileId = 0; fileId < files.Length; fileId++)
            {
                if (File.Exists(files[fileId].Split('\\').Last() + "_pearsonDataCsv.txt"))
                {
                    continue;
                }
                Console.WriteLine("GarbageCollecting - memory: " + GC.GetTotalMemory(false));
                GC.Collect();
                Console.WriteLine("Garbage Collection completed - memory:" + GC.GetTotalMemory(false));
                Console.WriteLine("Performing DTW on csv data " + fileId + " of " + files.Length + "..");

                string[] data = File.ReadAllLines(files[fileId]);

                List<double> testDataPoints = new List<double>();
                List<double> recallDataPoints = new List<double>();

                Console.WriteLine("Parsing files");
                foreach (var line in data)
                {
                    var split = line.Replace(',', '.').Split(';');
                    double test = double.Parse(split[0], System.Globalization.CultureInfo.InvariantCulture);
                    double recall = double.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture);

                    testDataPoints.Add(test);
                    recallDataPoints.Add(recall);

                }

                Console.WriteLine("Computing DTW, this will take a while");
                //                Dtw dtw = new Dtw(testDataPoints.ToArray(), recallDataPoints.ToArray(), DistanceMeasure.Euclidean, true, true, null, null, 700);
                Dtw dtw = new Dtw(testDataPoints.ToArray(), recallDataPoints.ToArray(), DistanceMeasure.Euclidean, true, true, slopeStepSizeDiagonal: 2, slopeStepSizeAside: 1);

                var path = dtw.GetPath();

                Console.WriteLine("Done DTW'ing, generating img");

                PngExporter pngify = new PngExporter();
                pngify.Width = 36000;
                pngify.Height = 4000;

                var model = new PlotModel() { Title = "Red = test, blue = recall" };

                var aSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Blue, MarkerSize = 10 };
                var bSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Red, MarkerSize = 10 };

                for (int i = 0; i < testDataPoints.Count; i++)
                {
                    aSeries.Points.Add(new DataPoint(i, testDataPoints[i]));
                }

                for (int i = 0; i < recallDataPoints.Count; i++)
                {
                    bSeries.Points.Add(new DataPoint(i, recallDataPoints[i]));
                }

                List<string> pearsonData = new List<string>();
                foreach (var pairing in path)
                {
                    var lineSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Gray, MarkerSize = 0.05 };

                    lineSeries.Points.Add(new DataPoint(pairing.Item1, testDataPoints[pairing.Item1]));
                    lineSeries.Points.Add(new DataPoint(pairing.Item2, recallDataPoints[pairing.Item2]));

                    model.Series.Add(lineSeries);

                    pearsonData.Add(testDataPoints[pairing.Item1].ToString().Replace(',', '.') + ";" + recallDataPoints[pairing.Item2].ToString().Replace(',', '.'));
                }

                var pears = MathNet.Numerics.Statistics.Correlation.Pearson(path.Select(x => testDataPoints[x.Item1]).ToList(), path.Select(x => recallDataPoints[x.Item2]).ToList());
                Console.WriteLine("Pearson for " + files[fileId] + ":");
                Console.WriteLine(pears.ToString());

                File.WriteAllLines(files[fileId].Split('\\').Last() + "_pearsonDataCsv.txt", pearsonData);

                model.Series.Add(aSeries);
                model.Series.Add(bSeries);

                pngify.ExportToFile(model, files[fileId].Split('\\').Last() + "_csv.png");
            }

            Console.WriteLine("DonnoDK");
        }

        static void SavePngPairing(string path, string name, List<double> A, List<double> B)
        {
            PngExporter pngify = new PngExporter();
            pngify.Width = 3200;
            pngify.Height = 900;

            var model = new PlotModel() { Title = name };

            var aSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Blue };
            var bSeries = new OxyPlot.Series.LineSeries() { Color = OxyColors.Red };

            for (int i = 0; i < A.Count; i++)
            {
                aSeries.Points.Add(new OxyPlot.DataPoint(i, A[i]));
            }

            for (int i = 0; i < B.Count; i++)
            {
                bSeries.Points.Add(new OxyPlot.DataPoint(i, B[i]));
            }

            model.Series.Add(aSeries);
            model.Series.Add(bSeries);



            model.Axes.Add(new OxyPlot.Axes.LinearAxis() { Minimum = 0, Maximum = 1, Position = OxyPlot.Axes.AxisPosition.Left });
            //model.Axes.Add(new OxyPlot.Axes.LinearAxis() { Minimum = 0, Maximum = 1, Position = OxyPlot.Axes.AxisPosition.Bottom });


            pngify.ExportToFile(model, path);
        }




    }

    static class ext
    {
        public static List<T> MedianFilter<T>(this List<T> inList, int windowSize)
        {
            List<T> newValues = new List<T>();
            for (int i = 0; i < inList.Count - windowSize; i++)
            {
                List<T> tempValues = new List<T>();

                for (int j = 0; j < windowSize; j++)
                {
                    tempValues.Add(inList[i + j]);
                }

                tempValues.Sort();

                newValues.Add(tempValues.ElementAt((int)Math.Round((double)windowSize / 2)));
            }
            return newValues;
        }

        public static List<T> MedianFilter<T, TKey>(this List<T> inList, int windowSize, Func<T, TKey> comparer)
        {
            List<T> newValues = new List<T>();
            for (int i = 0; i < inList.Count - windowSize; i++)
            {
                List<T> tempValues = new List<T>();

                for (int j = 0; j < windowSize; j++)
                {
                    tempValues.Add(inList[i + j]);
                }

                tempValues.OrderBy(comparer);

                newValues.Add(tempValues.ElementAt((int)Math.Round((double)windowSize / 2)));
            }
            return newValues;
        }
    }
}
