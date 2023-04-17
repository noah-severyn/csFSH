// See https://aka.ms/new-console-template for more information
using csFSH;

Console.WriteLine("Hello, World!");

string folder = "C:\\Users\\Administrator\\Desktop\\fsh";
Image<Rgba32> baseimg = Image.Load<Rgba32>(Path.Combine(folder, "7AB50E44-0986135E-E5040004-C0.bmp"));
Image<Rgba32> alphaimg = Image.Load<Rgba32>(Path.Combine(folder, "7AB50E44-0986135E-E5040004-A0.bmp"));

Image<Rgba32> output = FSHImage.Blend(alphaimg, baseimg);
output.SaveAsPng(Path.Combine(folder, "output.png"));