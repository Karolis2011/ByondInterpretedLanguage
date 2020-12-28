using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using ByondLang.ChakraCore.Reflection;
using Newtonsoft.Json.Linq;
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
        TerminalChar[][] char_array;

        public Color background = new Color(0, 0, 0);
        public Color foreground = new Color(255, 255, 255);

        private BaseProgram context;

        public Terminal(int width, int height, BaseProgram context)
        {
            this.width = width;
            this.height = height;
            this.context = context;
            clear();
        }

        public Terminal(BaseProgram context) : this(64, 20, context) { }

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
                    char_array[y][x] = new TerminalChar(' ', background, foreground);
                }
            }
        }
        [JsCallable]
        public void write(JsValue val)
        {
            if (val.ValueType != JsValueType.String)
                realWrite(val.ConvertToString().ToString());
            else
                realWrite(val.ToString());
        }

        [JsCallable]
        public void write(JsValue val, bool promt, JsValue callback)
        {
            var callbackObject = context.RegisterCallback(callback);
            if (val.ValueType != JsValueType.String)
                realWrite(val.ConvertToString().ToString(), callbackObject, promt);
            else
                realWrite(val.ToString(), callbackObject, promt);
        }

        [JsCallable]
        public void write(JsValue val, JsValue callback) => write(val, false, callback);

        [JsCallable]
        public void print(JsValue val)
        {
            write(val);
            cursorX = 0;
            MoveDown();
        }

        private void realWrite(string str, JsCallback topic = null, bool prompt = false)
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
                        char_array[cursorY][cursorX] = new TerminalChar(c, background, foreground, topic, prompt);
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
            var callbackObject = context.RegisterCallback(callback);
            for (int X = 0; X < w; X++)
            {
                for (int Y = 0; Y < h; Y++)
                {
                    SetTopic(x + X, y + Y, callbackObject, promt);
                }
            }
        }
        [JsCallable]
        public void setTopic(int x, int y, int w, int h, JsValue callback) => setTopic(x, y, w, h, false, callback);

        [JsCallable]
        public void clearTopic(int x, int y, int w, int h)
        {
            for (int X = 0; X < w; X++)
            {
                for (int Y = 0; Y < h; Y++)
                {
                    SetTopic(x + X, y + Y, null, false);
                }
            }
        }

        public string Stringify()
        {
            StringBuilder o = new StringBuilder();
            lock (char_array)
            {
                for (int y = 0; y < char_array.Length; y++)
                {
                    var row = char_array[y];
                    Color lastfg = null;
                    Color lastbg = null;
                    string lastTopic = null;
                    for (int x = 0; x < row.Length; x++)
                    {
                        var termChar = row[x];
                        if(x == 0)
                        {
                            // Jut clean line init
                            if(termChar.topic != null)
                            {
                                o.Append(formatLinkOpening(termChar.topic));
                                lastTopic = termChar.topic;
                            }
                            o.Append(formatColorOpening(termChar.foreground, termChar.background));
                            lastfg = termChar.foreground;
                            lastbg = termChar.background;
                        } else
                        {
                            if(lastTopic != termChar.topic)
                            {
                                // Topic is diffrent, let's change that
                                o.Append(formatColorClosing(lastfg, lastbg)); // before doing anything close colors
                                if (lastTopic != null) // If topic was opened
                                {
                                    o.Append(formatLinkClosing(lastTopic)); // Close topic
                                }
                                if (termChar.topic != null)
                                {
                                    o.Append(formatLinkOpening(termChar.topic)); // Open new topic
                                }
                                o.Append(formatColorOpening(termChar.foreground, termChar.background)); // Open new colors
                                lastTopic = termChar.topic;
                                lastfg = termChar.foreground;
                                lastbg = termChar.background;
                            }
                            if(lastfg != termChar.foreground || lastbg != termChar.background)
                            {
                                // Colors diffrent, close and reopen
                                o.Append(formatColorClosing(lastfg, lastbg)); // close colors
                                o.Append(formatColorOpening(termChar.foreground, termChar.background)); // Open new colors
                                lastfg = termChar.foreground;
                                lastbg = termChar.background;
                            }
                        }

                        // Ourput char
                        o.Append(encode(termChar.text));
                    }

                    o.Append(formatColorClosing(lastfg, lastbg)); // close colors
                    if (lastTopic != null) // If topic was opened
                    {
                        o.Append(formatLinkClosing(lastTopic)); // Close topic
                    }
                    o.Append("<br/>"); // Add line break
                }
            }
            return o.ToString();
        }

        internal string formatLinkOpening(string topic) => $"<to to=\"{topic}\">";
        internal string formatLinkClosing(string topic) => $"</to>";


        internal string formatColorOpening(Color fg, Color bg) => $"<co fg=\"{fg.toHTML()}\" bg=\"{bg.toHTML()}\">";
        internal string formatColorClosing(Color fg, Color bg) => $"</co>";

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
                    char_array[height - 1][x] = new TerminalChar(' ', background, foreground);
                }
            }
        }
        

        private void SetTopic(int x, int y, JsCallback callback, bool prompt)
        {
            if (y >= 0 && x >= 0 && x < width && y < height)
            {
                char_array[y][x].callback = callback;
                char_array[y][x].prompt = prompt;

            }
        }

        internal void PrintException(Exception ex)
        {
            var lastfgcolor = foreground;
            var lastbgcolor = background;
            foreground = new Color(255, 0, 0);
            background = new Color(0, 0, 0);
            realWrite(ex.Message);
            realWrite("\r\n");
            foreground = lastfgcolor;
            background = lastbgcolor;
        }


        class TerminalChar
        {
            public char text = ' ';
            public Color background = new Color(0, 0, 0);
            public Color foreground = new Color(255, 255, 255);
            public JsCallback callback = null;
            public bool prompt = false;

            public string topic
            {
                get
                {
                    if(callback != null)
                    {
                        if (prompt) return '?' + callback.Id;
                        return callback.Id;
                    }
                    return null;
                }
            }


            public TerminalChar()
            {
            }

            public TerminalChar(char text, Color background, Color foreground, JsCallback callback = null, bool prompt = false) : base()
            {
                this.text = text;
                this.background = background.Copy();
                this.foreground = foreground.Copy();
                this.callback = callback;
                this.prompt = prompt;
            }



        }

        public class Color
        {
            public byte r = 0;
            public byte g = 0;
            public byte b = 0;

            public Color()
            {
            }

            public Color(float r, float g, float b) : this((int)(r * 255), (int)(g * 255), (int)(b * 255))
            {
            }

            public Color(int r, int g, int b)
            {
                this.r = (byte)Math.Clamp(r, 0, 255);
                this.g = (byte)Math.Clamp(g, 0, 255);
                this.b = (byte)Math.Clamp(b, 0, 255);
            }

            public Color(byte r, byte g, byte b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }

            public Color Copy()
            {
                return new Color(r, g, b);
            }

            public string toHTML()
            {
                return $"{r:X2}{g:X2}{b:X2}";
            }

            // override object.Equals
            public bool Equals(Color c)
            {
                if (ReferenceEquals(c, null))
                    return false;

                // Optimization for a common success case.
                if (ReferenceEquals(this, c))
                    return true;

                return r == c.r && g == c.g && b == c.b;
            }

            public override bool Equals(object c)
            {
                return Equals(c as Color);
            }

            public override int GetHashCode()
            {
                int hash = r.GetHashCode();
                unchecked
                {
                    hash += g.GetHashCode();
                    hash += b.GetHashCode();
                }
                return hash;
            }

            public static bool operator ==(Color lhs, Color rhs)
            {
                if (ReferenceEquals(lhs, null))
                {
                    if (ReferenceEquals(rhs, null))
                    {
                        // null == null = true.
                        return true;
                    }

                    // Only the left side is null.
                    return false;
                }
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Color lhs, Color rhs)
            {
                return !(lhs == rhs);
            }
        }
    }
}
