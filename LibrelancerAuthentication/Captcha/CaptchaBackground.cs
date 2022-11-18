using SkiaSharp;

namespace LibrelancerAuthentication.Captcha;

public class CaptchaBackground : IDisposable
{
    private static SKColor[] SourcePalette = new SKColor[]
    {
        new SKColor(240, 232, 205),
        new SKColor(219, 213, 185),
        new SKColor(192, 186, 153),
        new SKColor(254, 235, 201),
        new SKColor(253, 202, 162),
        new SKColor(252, 169, 133),
        new SKColor(255, 255, 176),
        new SKColor(255, 250, 129),
        new SKColor(255, 237, 81),
        new SKColor(224, 243, 176),
        new SKColor(191, 228, 118),
        new SKColor(133, 202, 93),
        new SKColor(207, 236, 207),
        new SKColor(181, 255, 174),
        new SKColor(145, 210, 144),
        new SKColor(179, 226, 221),
        new SKColor(134, 207, 190),
        new SKColor(72, 171, 163),
        new SKColor(204, 236, 239),
        new SKColor(154, 206, 223),
        new SKColor(111, 183, 214),
        new SKColor(191, 213, 232),
        new SKColor(148, 168, 208),
        new SKColor(117, 137, 191),
        new SKColor(221, 212, 232),
        new SKColor(193, 179, 215),
        new SKColor(165, 137, 193),
        new SKColor(253, 222, 238),
        new SKColor(251, 182, 209),
        new SKColor(249, 140, 182)
    };
    
    private byte[][] backgrounds = new byte[20][];
    
    public SKBitmap CreateBackground()
    {
        var rand = new Random();
        return SKBitmap.Decode(backgrounds[rand.Next(0, backgrounds.Length)]);
    }

    private PeriodicTimer timer;
    public CaptchaBackground()
    {
        for (int i = 0; i < backgrounds.Length; i++)
            backgrounds[i] = GenerateBackground();
        timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        Task.Run(BackgroundRefresh);
    }

    async void BackgroundRefresh()
    {
        int i = 0;
        while (await timer.WaitForNextTickAsync())
        {
            backgrounds[i++] = GenerateBackground();
            if (i >= backgrounds.Length) i = 0;
        }
    }
    
    static void Shuffle<T>(IList<T> list)
    {
        Random rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    byte[] GenerateBackground()
    {
        using var bitmap = new SKBitmap(400, 240);
        var rand = new Random();
        using (var canvas = new SKCanvas(bitmap))
        {
            List<SKColor> colors = new List<SKColor>(SourcePalette);
            Shuffle(colors);
            canvas.Clear(colors[0]);

            int circleCount = rand.Next(25, 50);
            int j = 1;
            for (int i = 0; i < circleCount; i++)
            {
                int circleSize = rand.Next(25, 40);
                int c = circleSize / 2;
                int circleX = rand.Next(0, 400 - circleSize);
                int circleY = rand.Next(0, 240 - circleSize);
                using var paint = new SKPaint()
                {
                    Style = SKPaintStyle.Fill,
                    Color = colors[j++]
                };
                if (j >= colors.Count) j = 1;
                canvas.DrawCircle(new SKPoint(circleX + c, circleY + c), circleSize, paint);
            }
            using var noise = SKShader.CreatePerlinNoiseTurbulence(0.3f, 0.7f, 3, rand.Next());
            using var noisePaint = new SKPaint()
            {
                Shader = noise,
                Color = new SKColor(255,255,255, 190)
            };
            canvas.DrawRect(0,0, 400, 240, noisePaint);
            canvas.Flush();
        }

        using var enc = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return enc.ToArray();
    }

    public void Dispose()
    {
        timer.Dispose();
    }
    
}