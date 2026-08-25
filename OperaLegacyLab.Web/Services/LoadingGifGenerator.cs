using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

namespace OperaLegacyLab.Web.Services;

public static class LoadingGifGenerator
{
    public static void Generate(string fileName)
    {
        const int width = 320;
        const int height = 100;

        const int target = 2781;

        // 50 frames × 100 ms = 5 seconds.
        const int frameCount = 50;
        const int frameDelay = 10;

        const int digitWidth = 55;
        const int digitHeight = 85;
        const int segment = 9;
        const int gap = 10;

        var directory = Path.GetDirectoryName(fileName);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var animation =
            new Image<Rgba32>(width, height);

        for (int frameNumber = 0;
             frameNumber < frameCount;
             frameNumber++)
        {
            double progress =
                (double)frameNumber /
                (frameCount - 1);

            // Ease-out cubic.
            double eased =
                1.0 -
                Math.Pow(1.0 - progress, 3.0);

            int number =
                (int)Math.Round(target * eased);

            if (frameNumber == frameCount - 1)
            {
                number = target;
            }

            using var frame =
                new Image<Rgba32>(width, height);

            Fill(frame, Color.White);

            DrawNumber(
                frame,
                number,
                digitWidth,
                digitHeight,
                segment,
                gap);

            if (frameNumber == 0)
            {
                CopyImage(frame, animation);
            }
            else
            {
                animation.Frames.AddFrame(
                    frame.Frames.RootFrame);
            }
        }

        animation.Metadata
            .GetGifMetadata()
            .RepeatCount = 0;

        foreach (var frame in animation.Frames)
        {
            frame.Metadata
                .GetGifMetadata()
                .FrameDelay = frameDelay;
        }

        animation.Save(
            fileName,
            new GifEncoder());
    }

    private static void DrawNumber(
        Image<Rgba32> image,
        int number,
        int digitWidth,
        int digitHeight,
        int segment,
        int gap)
    {
        string text = number.ToString();

        int totalWidth =
            text.Length * digitWidth +
            (text.Length - 1) * gap;

        int startX =
            (image.Width - totalWidth) / 2;

        int startY =
            (image.Height - digitHeight) / 2;

        for (int i = 0; i < text.Length; i++)
        {
            int digit =
                text[i] - '0';

            int x =
                startX +
                i * (digitWidth + gap);

            DrawDigit(
                image,
                digit,
                x,
                startY,
                digitWidth,
                digitHeight,
                segment);
        }
    }

    private static void DrawDigit(
        Image<Rgba32> image,
        int digit,
        int x,
        int y,
        int width,
        int height,
        int thickness)
    {
        bool[] segments =
            GetSegments(digit);

        /*
         *       A
         *     -----
         *   F |     | B
         *     - G -
         *   E |     | C
         *     -----
         *       D
         */

        if (segments[0])
        {
            Rectangle(
                image,
                x + thickness,
                y,
                width - 2 * thickness,
                thickness);
        }

        if (segments[1])
        {
            Rectangle(
                image,
                x + width - thickness,
                y + thickness,
                thickness,
                height / 2 - thickness);
        }

        if (segments[2])
        {
            Rectangle(
                image,
                x + width - thickness,
                y + height / 2,
                thickness,
                height / 2 - thickness);
        }

        if (segments[3])
        {
            Rectangle(
                image,
                x + thickness,
                y + height - thickness,
                width - 2 * thickness,
                thickness);
        }

        if (segments[4])
        {
            Rectangle(
                image,
                x,
                y + height / 2,
                thickness,
                height / 2 - thickness);
        }

        if (segments[5])
        {
            Rectangle(
                image,
                x,
                y + thickness,
                thickness,
                height / 2 - thickness);
        }

        if (segments[6])
        {
            Rectangle(
                image,
                x + thickness,
                y + height / 2 - thickness / 2,
                width - 2 * thickness,
                thickness);
        }
    }

    private static bool[] GetSegments(int digit)
    {
        switch (digit)
        {
            case 0:
                return new bool[]
                {
                    true, true, true,
                    true, true, true, false
                };

            case 1:
                return new bool[]
                {
                    false, true, true,
                    false, false, false, false
                };

            case 2:
                return new bool[]
                {
                    true, true, false,
                    true, true, false, true
                };

            case 3:
                return new bool[]
                {
                    true, true, true,
                    true, false, false, true
                };

            case 4:
                return new bool[]
                {
                    false, true, true,
                    false, false, true, true
                };

            case 5:
                return new bool[]
                {
                    true, false, true,
                    true, false, true, true
                };

            case 6:
                return new bool[]
                {
                    true, false, true,
                    true, true, true, true
                };

            case 7:
                return new bool[]
                {
                    true, true, true,
                    false, false, false, false
                };

            case 8:
                return new bool[]
                {
                    true, true, true,
                    true, true, true, true
                };

            case 9:
                return new bool[]
                {
                    true, true, true,
                    true, false, true, true
                };

            default:
                return new bool[]
                {
                    false, false, false,
                    false, false, false, false
                };
        }
    }

    private static void Rectangle(
        Image<Rgba32> image,
        int x,
        int y,
        int width,
        int height)
    {
        int right = x + width;
        int bottom = y + height;

        if (x < 0)
        {
            x = 0;
        }

        if (y < 0)
        {
            y = 0;
        }

        if (right > image.Width)
        {
            right = image.Width;
        }

        if (bottom > image.Height)
        {
            bottom = image.Height;
        }

        for (int yy = y; yy < bottom; yy++)
        {
            for (int xx = x; xx < right; xx++)
            {
                image[xx, yy] = Color.Black.ToPixel<Rgba32>();
            }
        }
    }

    private static void Fill(
        Image<Rgba32> image,
        Color color)
    {
        Rgba32 pixel = color.ToPixel<Rgba32>();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                image[x, y] = pixel;
            }
        }
    }

    private static void CopyImage(
        Image<Rgba32> source,
        Image<Rgba32> destination)
    {
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                destination[x, y] =
                    source[x, y];
            }
        }
    }
}