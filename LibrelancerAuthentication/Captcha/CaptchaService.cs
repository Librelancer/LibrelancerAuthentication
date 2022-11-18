using SkiaSharp;

namespace LibrelancerAuthentication.Captcha;

public class CaptchaService
{
    private static readonly SKBitmap PuzzleBitmap;
    private static readonly SKBitmap PuzzleStrokeBitmap;

    public const int MaxAttempts = 5;

    private TimeSpan captchaExpiry = TimeSpan.FromMinutes(5);

    private ObjectEncryption encryption;

    record CaptchaData(int solution, int id, DateTime timestamp);

    record CaptchaAttempt(DateTime Expiry, int Attempts) : IExpiringItem;

    record CodeData(DateTime expiry, string code);

    record UsedCode(DateTime Expiry) : IExpiringItem;
    

    private ExpiringDictionary<int, CaptchaAttempt> attempts = new ExpiringDictionary<int, CaptchaAttempt>();
    private ExpiringDictionary<string, UsedCode> usedCodes = new ExpiringDictionary<string, UsedCode>();
    private int currentId = 0;

    static byte[] ExtractResource(String filename)
    {
        using (var stream = typeof(CaptchaService).Assembly.GetManifestResourceStream(filename))
        {
            if (stream == null) return null;
            byte[] ba = new byte[stream.Length];
            stream.Read(ba, 0, ba.Length);
            return ba;
        }
    }
    
    static CaptchaService()
    {
        using var p = SKBitmap.Decode(ExtractResource("LibrelancerAuthentication.Captcha.puzzle.png"));
        PuzzleBitmap = p.Copy(SKColorType.Alpha8);
        PuzzleStrokeBitmap = SKBitmap.Decode(ExtractResource("LibrelancerAuthentication.Captcha.puzzle-stroke.png"));
    }


    private CaptchaBackground backgroundProvider;

    public CaptchaService(CaptchaBackground backgroundProvider)
    {
        this.backgroundProvider = backgroundProvider;
        encryption = new ObjectEncryption();
    }

    public bool CheckToken(string token)
    {
        if (!encryption.TryDecrypt(token, out CodeData data))
            return false;
        if (DateTime.UtcNow > data.expiry) return false;
        if (usedCodes.TryGetValue(data.code, out _)) return false;
        usedCodes.Set(data.code, new UsedCode(data.expiry));
        return true;
    }

    public CaptchaResult CheckCaptcha(string id, int x, out string token)
    {
        token = null;
        
        if (!encryption.TryDecrypt(id, out CaptchaData data)) {
            return CaptchaResult.Invalid;
        }
        if (data.timestamp + captchaExpiry < DateTime.UtcNow) {
            return CaptchaResult.Expired;
        }
        int attemptCount = 0;
        if (attempts.TryGetValue(data.id, out var currentAttempt))
        {
            attemptCount = currentAttempt.Attempts;
        }
        if (attemptCount >= MaxAttempts)
        {
            return CaptchaResult.Expired;
        }
        if (x >= (data.solution - 4) && x <= (data.solution + 4))
        {
            //Set to max attempts, expires the captcha
            attempts.Set(data.id, new CaptchaAttempt(data.timestamp + captchaExpiry, MaxAttempts));
            var code = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            token = encryption.Encrypt(new CodeData(expiry, code));
            return CaptchaResult.Ok;
        }
        attempts.Set(data.id, new CaptchaAttempt(data.timestamp + captchaExpiry, attemptCount + 1));
        return CaptchaResult.IncorrectSolution;
    }

    public RenderedCaptcha Create()
    {
        using var background = this.backgroundProvider.CreateBackground();
        
        var rand = new Random();
        
        var puzzleY = rand.Next(16, (int)(background.Height - 1.5 * PuzzleBitmap.Height));
        var puzzleX = rand.Next(PuzzleBitmap.Width + 4, background.Width - PuzzleBitmap.Width - 4);

        var result = new RenderedCaptcha();
        
        using (var piece = new SKBitmap(PuzzleBitmap.Width, PuzzleBitmap.Height))
        {
            using var canvas = new SKCanvas(piece);
            using var piecePaint = new SKPaint();
            using var shader = SKShader.CreateBitmap(background, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat,
                SKMatrix.CreateTranslation(-puzzleX, -puzzleY));
            piecePaint.Shader = shader;
            canvas.DrawBitmap(PuzzleBitmap, 0, 0, piecePaint);
            canvas.DrawBitmap(PuzzleStrokeBitmap, 0, 0);
            canvas.Flush();
            result.Piece = "data:image/png;base64," + Convert.ToBase64String(piece.Encode(SKEncodedImageFormat.Png, 100).AsSpan());
        }

        using var bCanvas = new SKCanvas(background);
        using var bPaint = new SKPaint();
        bPaint.Color = new SKColor(0, 0, 0, 128);
        bCanvas.DrawBitmap(PuzzleBitmap, puzzleX, puzzleY, bPaint);
        bCanvas.Flush();
        
        result.Background = "data:image/png;base64," + Convert.ToBase64String(background.Encode(SKEncodedImageFormat.Png, 100).AsSpan());

        result.Y = puzzleY;
        result.Id = encryption.Encrypt(new CaptchaData(puzzleX, Interlocked.Increment(ref currentId), DateTime.UtcNow));
        return result;
    }
}