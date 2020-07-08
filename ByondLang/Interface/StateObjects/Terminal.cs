using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using ByondLang.ChakraCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ByondLang.Interface.StateObjects
{
    [JsObject]
    public class Terminal
    {
        [JsMapped]
        public int cursorX { get; set; } = 0;
        [JsMapped]
        public int cursorY { get; set; } = 0;
        [JsMapped]
        public int width { get; private set; } = 64;
        [JsMapped]
        public int height { get; private set; } = 20;
        string computer_ref;
        TerminalChar[][] char_array;

        public Color background = new Color(0, 0, 0);
        public Color foreground = new Color(255, 255, 255);

        private BaseProgram context;

        public Terminal(int width, int height, string computer_ref, BaseProgram context)
        {
            this.computer_ref = computer_ref;
            this.width = width;
            this.height = height;
            this.context = context;
            clear();
        }

        public Terminal(string computer_ref, BaseProgram context) : this(64, 20, computer_ref, context) { }

        [JsCallable]
        public void setForeground(float r, float g, float b) => foreground = new Color(r, g, b);
        [JsCallable]
        public JsValue getForeground(TypeMapper tm)
        {
            var array = JsValue.CreateArray(3);
            array.SetIndexedProperty(0, tm.MTS(foreground.r));
            array.SetIndexedProperty(1, tm.MTS(foreground.g));
            array.SetIndexedProperty(2, tm.MTS(foreground.b));
            return array;
        }
        [JsCallable]
        public void setBackground(float r, float g, float b) => background = new Color(r, g, b);
        [JsCallable]
        public JsValue getBackground(TypeMapper tm)
        {
            var array = JsValue.CreateArray(3);
            array.SetIndexedProperty(0, tm.MTS(background.r));
            array.SetIndexedProperty(1, tm.MTS(background.g));
            array.SetIndexedProperty(2, tm.MTS(background.b));
            return array;
        }
        [JsCallable]
        public void setCursor(int x, int y)
        {
            cursorX = x;
            cursorY = y;
        }
        [JsCallable]
        public JsValue getCursor(TypeMapper tm)
        {
            var array = JsValue.CreateArray(2);
            array.SetIndexedProperty(0, tm.MTS(cursorX));
            array.SetIndexedProperty(1, tm.MTS(cursorY));
            return array;
        }
        [JsCallable]
        public void clear()
        {
            cursorX = 0;
            cursorY = 0;
            char_array = new TerminalChar[height][];
            for (int y = 0; y < height; y++)
            {
                char_array[y] = new TerminalChar[width];
                for (int x = 0; x < width; x++)
                {
                    char_array[y][x] = new TerminalChar(' ', background, foreground, "");
                }
            }
        }
        [JsCallable]
        public void write(JsValue val)
        {
            if (val.ValueType != JsValueType.String)
                write(val.ConvertToString().ToString());
            else
                write(val.ToString());
        }

        [JsCallable]
        public void print(JsValue val)
        {
            write(val);
            cursorX = 0;
            MoveDown();
        }

        public void write(string str)
        {
            foreach (char c in str)
            {
                if (c == '\r')
                {
                    cursorX = 0;
                }
                else if (c == '\n')
                {
                    cursorX = 0;
                    MoveDown();
                }
                else if (c == '\t')
                {
                    MoveRight();
                    while (cursorX % 4 > 0)
                        MoveRight();
                }
                else
                {
                    lock (char_array)
                    {
                        char_array[cursorY][cursorX] = new TerminalChar(c, background, foreground, "");
                    }
                    MoveRight();
                }
            }
        }
        [JsCallable]
        public JsValue getSize(TypeMapper tm)
        {
            var array = JsValue.CreateArray(2);
            array.SetIndexedProperty(0, tm.MTS(width));
            array.SetIndexedProperty(1, tm.MTS(height));
            return array;
        }
        [JsCallable]
        public void setTopic(int x, int y, int w, int h, bool promt, JsValue callback)
        {
            var topic = context.RegisterCallback(callback);
            if (promt)
                topic = "?" + topic;
            for (int X = 0; X < w; X++)
            {
                for (int Y = 0; Y < h; Y++)
                {
                    SetTopic(x + X, y + Y, topic);
                }
            }
        }
        [JsCallable]
        public void setTopic(int x, int y, int w, int h, JsValue callback) => setTopic(x, y, w, h, false, callback);

        public string Stringify()
        {
            StringBuilder outp = new StringBuilder();
            lock (char_array)
            {
                for (int y = 0; y < height; y++)
                {
                    if (y != 0)
                        outp.Append("<br>");
                    for (int x = 0; x < width; x++)
                    {

                        TerminalChar toDraw = char_array[y][x];

                        // Is out color diffrent?
                        if (x == 0 || !toDraw.Like(char_array[y][x - 1]))
                            // We are not at start, so we close prevous color
                            if (x != 0)
                                outp.Append(toDraw.ColorClose());
                        outp.Append(toDraw.ColorOpen());

                        // Open topic 
                        if (toDraw.topic != "" && (x == 0 || toDraw.topic != char_array[y][x - 1].topic))
                        {
                            outp.Append($"<a style='text-decoration:none;' href='?src={computer_ref};PRG_topic={HttpUtility.UrlEncode(toDraw.topic)}'>");
                            outp.Append(toDraw.ColorOpen());
                        }

                        outp.Append(encode(toDraw.text));

                        if (x == width - 1)
                            outp.Append(toDraw.ColorClose());

                        if (toDraw.topic != "" && !(x < width - 1 && toDraw.topic == char_array[y][x + 1].topic))
                        {
                            outp.Append("</a>");

                        }
                    }
                }
            }
            return outp.ToString();
        }

        public string encode(char c)
        {
            if (c == ' ')
                return "&nbsp;";
            return HttpUtility.HtmlEncode(c.ToString());
        }


        public void MoveRight()
        {
            cursorX++;
            if (cursorX >= width)
            {
                cursorX = 0;
                MoveDown();
            }
        }

        public void MoveDown()
        {
            cursorY++;
            if (cursorY >= height)
            {
                cursorY--;
                for (int i = 0; i < height - 1; i++)
                {
                    char_array[i] = char_array[i + 1];
                }
                char_array[height - 1] = new TerminalChar[width];
                for (int x = 0; x < width; x++)
                {
                    char_array[height - 1][x] = new TerminalChar(' ', background, foreground, "");
                }
            }
        }
        

        private void SetTopic(int x, int y, string topic)
        {
            if (y >= 0 && x >= 0 && x < width && y < height)
            {
                char_array[y][x].topic = topic;
            }
        }

        internal void PrintException(Exception ex)
        {
            var lastfgcolor = foreground;
            var lastbgcolor = background;
            foreground = new Color(255, 0, 0);
            background = new Color(0, 0, 0);
            write(ex.Message);
            write("\r\n");
            foreground = lastfgcolor;
            background = lastbgcolor;
        }


        class TerminalChar
        {
            public char text = ' ';
            public Color background = new Color(0, 0, 0);
            public Color foreground = new Color(255, 255, 255);
            public string topic = "";


            public TerminalChar()
            {
            }

            public TerminalChar(char text, Color background, Color foreground, string topic) : base()
            {
                this.text = text;
                this.background = background.Copy();
                this.foreground = foreground.Copy();
                this.topic = topic;
            }

            public bool Like(TerminalChar other)
            {
                return background.r == other.background.r &&
                       background.g == other.background.g &&
                       background.b == other.background.b &&
                       foreground.r == other.foreground.r &&
                       foreground.g == other.foreground.g &&
                       foreground.b == other.foreground.b;
            }

            internal string ColorOpen() => $"<span style=\"color:{foreground.toHTML()};background-color:{background.toHTML()}\">";
            internal string ColorClose() => $"</span>";
        }

        public class Color
        {
            public float r = 0;
            public float g = 0;
            public float b = 0;

            public Color()
            {

            }

            public Color(float r, float g, float b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                Validate();
            }

            public Color(int r, int g, int b)
            {
                this.r = (r / 255.0f);
                this.g = (g / 255.0f);
                this.b = (b / 255.0f);
                Validate();
            }

            public void Validate()
            {
                r = Math.Clamp(r, 0.0f, 1.0f);
                g = Math.Clamp(g, 0.0f, 1.0f);
                b = Math.Clamp(b, 0.0f, 1.0f);
            }

            public Color Copy()
            {
                return new Color(r, g, b);
            }

            public string toHTML()
            {
                int R = (int)(r * 255);
                int G = (int)(g * 255);
                int B = (int)(b * 255);
                return $"#{R.ToString("X2")}{G.ToString("X2")}{B.ToString("X2")}";
            }
        }
    }
}
