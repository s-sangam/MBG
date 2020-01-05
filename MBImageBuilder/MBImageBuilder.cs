using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MBGenerator.avro;
using Confluent.Kafka;

namespace MBImageBuilder
{

    public class MBImageBuilder
    {

        IGridFSBucket _bucket;
        imageRequest _request;
        IProducer<Null, imageResponse> _producer;

        bool toFile = false;

        public MBImageBuilder(imageRequest request, IProducer<Null, imageResponse> producer, IGridFSBucket bucket)
        {
            _request = request;
            _producer = producer;
            _bucket = bucket;
        }
        public void MBCreateImage()
        {
            int imageWidth = 100;
            int imageHeight = 100;

            Bitmap bmp = new Bitmap(imageWidth, imageHeight);

            double cx = 0;
            double cy = 0;
            double x_step = (_request.max_x - _request.min_x) / imageWidth;
            double y_step = (_request.max_y - _request.min_y) / imageHeight;
            double escapeCount = 0;
            double logMax = Math.Log(1000 * _request.depth);


            //run through each pixel in the bitmap, and calculate whether each pixel is in the mandelbrot set or not
            //based on the co-ordinates of the request.
            for (int x = 0; x <= imageWidth - 1; x++)
            {
                cx = _request.min_x + x * x_step;

                for (int y = 0; y <= imageHeight - 1; y++)
                {
                    cy = _request.min_y + y * y_step;

                    // calcuate the number of iterations to escape at this point in the set.

                    escapeCount = EscapeCount(cx, cy, _request.depth);

                    if (escapeCount < (1000 * _request.depth))
                    {

                        // use a log scale to get some nice grading in the colours
                        escapeCount = (Math.Log(escapeCount) / logMax);

                        // get a nice colour from looking up the hue of the escape count.
                        // note the test for zero puts a border around the image 
                        ColorRGB colorRGB;
                        ColorRGB colourRGB;

                        if (x == 0 | y == 0)
                        {
                            colorRGB = HSL2RGB(escapeCount, 0.5, 0.55);
                        }
                        else
                        {
                            colorRGB = HSL2RGB(escapeCount, 0.5, 0.5);
                        }

                        // switch the colours around to make it a bit more interesting.
                        switch(_request.depth % 4)
                        {
                            case 0:
                                { colourRGB.R = colorRGB.G; colourRGB.G = colorRGB.R; colourRGB.B = colorRGB.B; break; }
                            case 1:
                                { colourRGB.R = colorRGB.B; colourRGB.G = colorRGB.G; colourRGB.B = colorRGB.R; break; }
                            case 2:
                                { colourRGB.R = colorRGB.R; colourRGB.G = colorRGB.B; colourRGB.B = colorRGB.G; break; }
                            default:
                                { colourRGB.R = colorRGB.R; colourRGB.G = colorRGB.G; colourRGB.B = colorRGB.B; break; }
                        }

                        // set the pixel colour
                        bmp.SetPixel(x, y, Color.FromArgb(colourRGB.R, colourRGB.B, colourRGB.G));
                    }
                    else
                    {

                        if (x == 0 | y == 0)
                        {
                            // set it to grey.
                            bmp.SetPixel(x, y, Color.FromArgb(25, 25, 25));
                        }
                        else
                        {
                            // set it to black.
                            bmp.SetPixel(x, y, Color.FromArgb(0, 0, 0));
                        }

                    }
                }
            }

            // assuming everything kafka and mongo is threadsafe....

            if (toFile)
            {
                // create a new file and write the image to the file in jpeg format.
                // get rid of this when the upload to Mongo works!
                string filename = $"./Images/Image{_request.display_x.ToString()}-{_request.display_y.ToString()}.jpg";
                FileStream fileStream = File.Create(filename);
                bmp.Save(fileStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                fileStream.Close();
            }
            else
            {

                // upload the bmp to mongoDB as a Jpeg
                MemoryStream tmpStream = new MemoryStream();
                bmp.Save(tmpStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                tmpStream.Position = 0;
                var id = _bucket.UploadFromStream("filename", tmpStream);
                bmp.Dispose();


                var _response = new imageResponse();
                _response.display_x = _request.display_x;
                _response.display_y = _request.display_y;
                _response.url = id.ToString();
                _response.connectionId = _request.connectionId;

                // send the image Id back to the client
                _producer.ProduceAsync("imageResponse", new Message<Null, imageResponse> { Value = _response });
                _producer.Flush();
            }
        }

        // calculate the number of iterations needed to escape from the set (with a max of 10000 for now)
        public double EscapeCount(double cx, double cy, int depth)
        {
            double zx, zy, tempx;
            double count = 0;

            // z_real 
            zx = 0;

            // z_imaginary 
            zy = 0;

            // Calculate whether c(c_real + c_imaginary) belongs 
            // to the Mandelbrot set or not and draw a pixel 
            // at coordinates (x, y) accordingly 
            // If you reach the Maximum number of iterations 
            // and If the distance from the origin is 
            // greater than 2 exit the loop 
            while ((zx * zx + zy * zy < 4) && (count < (10000 * depth)))
            {
                // Calculate Mandelbrot function 
                // z = z*z + c where z is a complex number 

                // tempx = z_real*_real - z_imaginary*z_imaginary + c_real 
                tempx = zx * zx - zy * zy + cx;

                // 2*z_real*z_imaginary + c_imaginary 
                zy = 2 * zx * zy + cy;

                // Updating z_real = tempx 
                zx = tempx;

                // Increment count 
                count += 1;
            }
            return count;
        }

        public struct ColorRGB
        {
            public byte R;
            public byte G;
            public byte B;
            public ColorRGB(Color value)
            {
                this.R = value.R;
                this.G = value.G;
                this.B = value.B;
            }
            public static implicit operator Color(ColorRGB rgb)
            {
                Color c = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                return c;
            }
            public static explicit operator ColorRGB(Color c)
            {
                return new ColorRGB(c);
            }
        }

        // Given H,S,L in range of 0-1
        // Returns a Color (RGB struct) in range of 0-255
        public static ColorRGB HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;

            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            ColorRGB rgb;
            rgb.R = Convert.ToByte(r * 255.0f);
            rgb.G = Convert.ToByte(g * 255.0f);
            rgb.B = Convert.ToByte(b * 255.0f);
            return rgb;
        }
    }
}
