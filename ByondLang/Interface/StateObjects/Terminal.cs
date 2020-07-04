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
    public class Terminal
    {
        public int cursor_x = 0;
        public int cursor_y = 0;
        public int width = 64;
        public int height = 20;
        string computer_ref;
        TerminalChar[][] char_array;

        public Color background = new Color(0, 0, 0);
        public Color foreground = new Color(255, 255, 255);

        public Terminal(int width, int height, string computer_ref)
        {
            this.computer_ref = computer_ref;
            this.width = width;
            this.height = height;
            Clear();
        }

        public JsValue GetRepresentation(ChakraCore.TypeMapper tm)
        {
            var term = JsValue.CreateObject();
            term.SetProperty("set_foreground", tm.MTS((Action<float, float, float>)SetForeground), true);
            term.SetProperty("get_foreground", tm.MTS((Func<ChakraCore.TypeMapper, JsValue>)GetForeground), true);
            term.SetProperty("set_background", tm.MTS((Action<float, float, float>)SetForeground), true);
            term.SetProperty("get_background", tm.MTS((Func<ChakraCore.TypeMapper, JsValue>)GetForeground), true);
            term.SetProperty("set_cursor", tm.MTS((Action<int, int>)SetCursor), true);
            term.SetProperty("set_cursor_x", tm.MTS((Action<int>)SetCursorX), true);
            term.SetProperty("set_cursor_y", tm.MTS((Action<int>)SetCursorY), true);
            term.SetProperty("get_cursor", tm.MTS((Func<ChakraCore.TypeMapper, JsValue>)GetCursor), true);
            term.SetProperty("get_cursor_x", tm.MTS((Func<int>)GetCursorX), true);
            term.SetProperty("get_cursor_y", tm.MTS((Func<int>)GetCursorY), true);
            term.SetProperty("clear", tm.MTS((Action)Clear), true);
            term.SetProperty("write", tm.MTS((Action<string>)Write), true);
            term.SetProperty("get_size", tm.MTS((Func<ChakraCore.TypeMapper, JsValue>)GetSize), true);
            term.SetProperty("get_width", tm.MTS((Func<int>)GetWidth), true);
            term.SetProperty("get_height", tm.MTS((Func<int>)GetHeight), true);
            term.SetProperty("set_topic", tm.MTS((Action<int, int, int, int, string>)SetTopic), true);
            return term;
        }

        // set_foreground
        public void SetForeground(float r, float g, float b) => foreground = new Color(r, g, b);
        // get_foreground
        public JsValue GetForeground(ChakraCore.TypeMapper tm)
        {
            var array = JsValue.CreateArray(3);
            array.SetIndexedProperty(0, tm.MTS(foreground.r));
            array.SetIndexedProperty(1, tm.MTS(foreground.g));
            array.SetIndexedProperty(2, tm.MTS(foreground.b));
            return array;
        }
        // set_background
        public void SetBackground(float r, float g, float b) => background = new Color(r, g, b);
        // get_foreground
        public JsValue GetBackground(ChakraCore.TypeMapper tm)
        {
            var array = JsValue.CreateArray(3);
            array.SetIndexedProperty(0, tm.MTS(background.r));
            array.SetIndexedProperty(1, tm.MTS(background.g));
            array.SetIndexedProperty(2, tm.MTS(background.b));
            return array;
        }
        // set_cursor
        public void SetCursor(int x, int y)
        {
            cursor_x = x;
            cursor_y = y;
        }
        // set_cursor_x
        public void SetCursorX(int x) => cursor_x = x;
        // set_cursor_y
        public void SetCursorY(int y) => cursor_y = y;
        // get_cursor
        public JsValue GetCursor(ChakraCore.TypeMapper tm)
        {
            var array = JsValue.CreateArray(2);
            array.SetIndexedProperty(0, tm.MTS(cursor_x));
            array.SetIndexedProperty(1, tm.MTS(cursor_y));
            return array;
        }
        // get_cursor_x
        public int GetCursorX() => cursor_x;
        // get_cursor_y
        public int GetCursorY() => cursor_y;
        // clear
        public void Clear()
        {
            cursor_x = 0;
            cursor_y = 0;
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
        // write
        public void Write(string str)
        {
            foreach (char c in str)
            {
                if (c == '\r')
                {
                    cursor_x = 0;
                }
                else if (c == '\n')
                {
                    MoveDown();
                }
                else if (c == '\t')
                {
                    MoveRight();
                    while (cursor_x % 4 > 0)
                        MoveRight();
                }
                else
                {
                    char_array[cursor_y][cursor_x] = new TerminalChar(c, background, foreground, "");
                    MoveRight();
                }
            }
        }
        // get_size
        public JsValue GetSize(ChakraCore.TypeMapper tm)
        {
            var array = JsValue.CreateArray(2);
            array.SetIndexedProperty(0, tm.MTS(width));
            array.SetIndexedProperty(1, tm.MTS(height));
            return array;
        }
        // get_width
        public int GetWidth() => width;
        // get_height
        public int GetHeight() => height;
        // set_topic
        public void SetTopic(int x, int y, int w, int h, string topic)
        {
            for (int X = 0; X < w; X++)
            {
                for (int Y = 0; Y < h; Y++)
                {
                    SetTopic(x + X, y + Y, topic);
                }
            }
        }



        public string Stringify()
        {
            string outp = "";
            string joiner = "";
            for (int y = 0; y < height; y++)
            {
                outp += joiner;
                joiner = "<br>";
                for (int x = 0; x < width; x++)
                {
                    StringBuilder to_Write = new StringBuilder();
                    TerminalChar toDraw = char_array[y][x];
                    if (toDraw.topic != "" && !(x > 0 && toDraw.topic == char_array[y][x - 1].topic))
                        to_Write.Append($"<a style='text-decoration:none;' href='?src={computer_ref};PRG_topic={HttpUtility.UrlEncode(toDraw.topic)}'>");
                    if (!(x > 0 && toDraw.Like(char_array[y][x - 1])))
                        to_Write.Append( $"<span style=\"color:{toDraw.foreground.toHTML()};background-color:{toDraw.background.toHTML()}\">");
                    to_Write.Append(encode(toDraw.text));
                    if (!(x < width - 1 && toDraw.Like(char_array[y][x + 1])))
                        to_Write.Append("</span>");
                    if (toDraw.topic != "" && !(x < width - 1 && toDraw.topic == char_array[y][x + 1].topic))
                        to_Write.Append("</a>");
                    outp += to_Write;
                }
            }
            return outp;
        }

        public string encode(char c)
        {
            if (c == ' ')
                return "&nbsp;";
            return HttpUtility.HtmlEncode(c.ToString());
        }

        public Terminal(string computer_ref) : this(64, 20, computer_ref) { }

        public void MoveRight()
        {
            cursor_x++;
            if (cursor_x >= width)
            {
                cursor_x = 0;
                MoveDown();
            }
        }

        public void MoveDown()
        {
            cursor_y++;
            if (cursor_y >= height)
            {
                cursor_y--;
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
