﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using HoTS_Service.Service;
using System.Net;
using System.IO;
using HoTS_Service.Entity;
using HoTS_Service.Entity.Enum;
using System.Globalization;
using HoTS_Service.Util.Extension;
using HoTS_Service.Util;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OnlineParser
{
    class Program
    {
        static class Selectors
        {
            public static string GetDataSelector(int i)
            {
                return $".wikitable > tbody:nth-child(1) > tr:nth-child({i}) > td:nth-child(3)";
            }

            public static string GetImageSelector()
            {
                return ".infobox2 > tbody:nth-child(1) > tr:nth-child(2) > td:nth-child(1) > a:nth-child(1) > img:nth-child(1)";
            }

            public static string GetIconSelector()
            {
                return ".hero-tile";
            }

            public static string GetIconContainer()
            {
                return "div.main-page:nth-child(3)";
            }
        }

        const bool USE_HERO_DETAILS = true;
        const bool USE_HERO_ICONS = true;


        static HeroService Heroes = new HeroService();
        const String HeroTemplateURL = "https://heroesofthestorm.gamepedia.com/Data:";
        const String IconsURL = "https://heroesofthestorm.gamepedia.com/Heroes_of_the_Storm_Wiki";
        const String ImageTemplateURL = "https://heroesofthestorm.gamepedia.com/";
        static HeroDetails[] Details;

        static AnimatedBar ABar = new AnimatedBar();

        static int temp = 0;

        public static object Bitmap { get; private set; }

        static void Main(string[] args)
        {
            string HTML ="";

            if (!Directory.Exists("Cache"))
                Directory.CreateDirectory("Cache");
            if (!Directory.Exists("Images"))
                Directory.CreateDirectory("Images");
            if (!Directory.Exists("Icons"))
                Directory.CreateDirectory("Icons");
            if (!Directory.Exists("Hero"))
                Directory.CreateDirectory("Hero");

            log("debug", "WEB Парсер запущен");
            if (!File.Exists("./Source/Hero/Hero.json"))
                log("error", "Не найден исходный файл схемы героев по пути ./Source/Hero/Hero.json");
            Heroes.Load("./Source/Hero/Hero.json");
            if (USE_HERO_DETAILS)
            {
                Details = new HeroDetails[Heroes.Count()];
                log("succes", "Схема героев успешно загружена");
                log("debug", "Парсинг деталей героев");

                for (int i = 0; i < Heroes.Count(); i++)
                {
                    temp = i;
                    var Hero = Heroes.Find(i);

                    Caching(HeroTemplateURL + Hero.Name, $"./Cache/{Hero.Name}.html");

                    log("debug", "Чтение Data:" + Hero.Name);

                    HTML = File.ReadAllText($"./Cache/{Hero.Name}.html", Encoding.Default);

                    log("succes", "Считан Data:" + Hero.Name);

                    log("debug", "Парсинг " + Hero.Name + " начат");
                    Details[i] = ParseDetails(HTML);
                    log("info", Details[i].ToString());
                    log("succes", "Парсинг " + Hero.Name + " завершен");

                }

                log("succes", "Парсинг деталей героев завершен");

                var outputDetails = JSonParser.Save(Details, typeof(HeroDetails[]));
                File.WriteAllText("Hero/HeroDetails.json", outputDetails);

            }

            if (USE_HERO_ICONS)
            {

                Caching(IconsURL, "./Cache/Icons.html");

                log("debug", "Чтение Icons");
                HTML = File.ReadAllText($"./Cache/Icons.html");
                log("succes", "Считан Icons");

                log("debug", "Парсинг Icons начат");
                ParseIcons(HTML);
                log("succes", "Парсинг Icons завершен");
            }

            log("debug", "Парсинг изображений");

            for(int i = 0; i < Heroes.Count(); i++)
            {
                var Hero = Heroes.Find(i);

                Caching(ImageTemplateURL + Hero.Name, $"./Cache/{Hero.Name}_Large.html");

                log("debug", $"Чтение {Hero.Name}_Large");
                HTML = File.ReadAllText($"./Cache/{Hero.Name}_Large.html", Encoding.Default);
                log("succes", $"Считан {Hero.Name}_Large");

                log("debug", $"Парсинг {Hero.Name}_Large");
                ParseImages(HTML,Hero.Name);
                log("succes", $"Парсинг {Hero.Name}_Large завершен");
            }

            log("succes", "Парсинг изображений завершен");


        }


        public static void Caching(string url,string resource)
        {
            if (!File.Exists(resource))
            {
                log("debug", $"Загрузка {resource}");

                Download(url, resource);

                log("succes", $"Загружен {resource}");
            }
        }

        public static void Download(Hero Hero)
        {
            Download(HeroTemplateURL + Hero.Name, $"./Cache/{Hero.Name}.html");
        }

        public static void Download(string url,string adress)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, adress);
            }
        }

        public static Image OvalImage(Bitmap srcImage, Color backGround)
        {
            Image dstImage = new Bitmap(srcImage.Width, srcImage.Height, srcImage.PixelFormat);
            Graphics g = Graphics.FromImage(dstImage);
            using (Brush br = new SolidBrush(backGround))
            {
                g.FillRectangle(br, 0, 0, dstImage.Width, dstImage.Height);
            }
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, dstImage.Width, dstImage.Height);
            g.SetClip(path);
            g.DrawImage(srcImage, 0, 0);

            return dstImage;
        }

        public static HeroDetails ParseDetails(string HTMLHero)
        {
            HeroDetails hd = new HeroDetails(temp);
            try
            {
                CQ cq = CQ.Create(HTMLHero);
                bool state;

                string buff = cq.Find(Selectors.GetDataSelector(4)).Elements.First().InnerText.Replace("\n", "");
                hd.Date = DateTime.Parse(buff);

                buff = cq.Find(Selectors.GetDataSelector(5)).Elements.First().InnerText.Replace("\n", "");
                hd.Price = Int32.Parse(buff);

                hd.Title = cq.Find(Selectors.GetDataSelector(6)).Elements.First().InnerText.Replace("\n", "");

                buff = cq.Find(Selectors.GetDataSelector(8)).Elements.First().InnerText.Replace("\n", "");
                state = Enum.TryParse(buff, out Franchise Fresult);
                if (state == false)
                    Fresult = Franchise.Unknown;
                hd.Franchise = Fresult;

                hd.Info = cq.Find(Selectors.GetDataSelector(9)).Elements.First().InnerHTML.Replace("\n", "");

                hd.Lore = cq.Find(Selectors.GetDataSelector(10)).Elements.First().InnerText.Replace("\n", "");

                buff = cq.Find(Selectors.GetDataSelector(11)).Elements.First().InnerText.Replace("\n", "");
                state = Enum.TryParse(buff, out Difficulty Dresult);
                if (state == false)
                    Dresult = Difficulty.Unknown;
                hd.Difficulty = Dresult;

                if (hd.Franchise == Franchise.Vikings)
                    return hd;
                if (Heroes.Find(temp).Name == "Greymane")
                {
                    Hero GreyCustom = new Hero(-1, "Greymane_(Human)", 0, 0);
                    Download(GreyCustom);
                    string HTMLCustom = File.ReadAllText($"./Cache/{GreyCustom.Name}.html", Encoding.Default);
                    cq = CQ.Create(HTMLCustom);
                }
                if (Heroes.Find(temp).Name == "D.Va")
                {
                    Hero GreyCustom = new Hero(-1, "D.Va_(Pilot)", 0, 0);
                    Download(GreyCustom);
                    string HTMLCustom = File.ReadAllText($"./Cache/{GreyCustom.Name}.html", Encoding.Default);
                    cq = CQ.Create(HTMLCustom);
                }

                buff = cq.Find(Selectors.GetDataSelector(12)).Elements.First().InnerText.Replace("\n", "");
                hd.Melee = Boolean.Parse(buff);

                buff = cq.Find(Selectors.GetDataSelector(13)).Elements.First().InnerText.Replace("\n", "").Replace(",","");
                hd.Health = Int32.Parse(buff);

                buff = cq.Find(Selectors.GetDataSelector(14)).Elements.First().InnerText.Replace("\n", "");
                hd.HealthRegen = buff.DoubleParseAdvanced();

                buff = cq.Find(Selectors.GetDataSelector(15)).Elements.First().InnerText.Replace("\n", "");
                hd.Resource = Int32.Parse(buff);

                buff = cq.Find(Selectors.GetDataSelector(16)).Elements.First().InnerText.Replace("\n", "");
                state = Enum.TryParse(buff, out ResourceType RTresult);
                if (state == false)
                    RTresult = ResourceType.Unknown;
                hd.ResourceType = RTresult;


                buff = cq.Find(Selectors.GetDataSelector(17)).Elements.First().InnerText.Replace("\n", "");
                if (buff.Contains("Spell"))
                {
                    var buff2 = buff.Split(new string[] { "Spell" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    hd.SpellArmor = Int32.Parse(buff2);
                }

                if (buff.Contains("Physical"))
                {
                    buff = buff.Split(new string[] { "Physical" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    hd.PhysicalArmor = Int32.Parse(buff);
                }


                buff = cq.Find(Selectors.GetDataSelector(18)).Elements.First().InnerText.Replace("\n", "");
                hd.AttackSpeed = buff.DoubleParseAdvanced();

                buff = cq.Find(Selectors.GetDataSelector(19)).Elements.First().InnerText.Replace("\n", "");
                hd.AttackRange = buff.DoubleParseAdvanced();

                buff = cq.Find(Selectors.GetDataSelector(20)).Elements.First().InnerText.Replace("\n", "");
                hd.AttackDamage = Int32.Parse(buff);

            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return hd;
        }

        public static void ParseIcons(string HTML)
        {
            CQ cq = CQ.Create(HTML).Find(Selectors.GetIconContainer());

            var cq2 = cq.Find(Selectors.GetIconSelector());
            foreach (var iconData in cq2)
            {

                var element = iconData.FirstChild.FirstChild;
                var alt = element.Attributes["alt"];
                var src = element.Attributes["src"];
                Caching(src, $"./Icons/{alt}.png");

                Bitmap img = new Bitmap(new Bitmap($"./Icons/{alt}.png"), new Size(40, 40));
                

                Bitmap imgWithFrame = new Bitmap(img);

                using (Graphics g = Graphics.FromImage(imgWithFrame))
                {
                    g.DrawRectangle(new Pen(Brushes.Gold, 2), new Rectangle(0, 0, img.Width, img.Height));
                }

                imgWithFrame.Save($"./Icons/{alt}_h.png");

                Image corner = OvalImage(img,Color.Transparent);

                /* using (Graphics g = Graphics.FromImage(corner))
                 {
                     var pen = new Pen(new SolidBrush(Color.Black), 1);
                     pen.Color = Color.FromArgb(150, Color.Black.R, Color.Black.G, Color.Black.B);
                     DrawCirlceFrame(corner, g, pen, 2);
                 }*/

                Bitmap rez = new Bitmap(50, 50);
                using (Graphics g = Graphics.FromImage(rez))
                {
                    g.DrawImage(corner, new Rectangle(5, 5, 40, 40), new Rectangle(0, 0, 40, 40), GraphicsUnit.Pixel);
                }

                rez.Save($"./Icons/{alt}_corner.png");

                using (Graphics g = Graphics.FromImage(rez))
                {
                    var pen = new Pen(new SolidBrush(Color.Gold), 1);

                    for(int i = 0; i < 10; i++)
                    {
                        pen.Color = Color.FromArgb((int)(20 + i*2), Color.Gold.R, Color.Gold.G, Color.Gold.B);
                        pen.Width = 10-i;
                        g.DrawEllipse(pen, 5, 5, corner.Width, corner.Height);
                    }

                }

                rez.Save($"./Icons/{alt}_corner_h.png");

            }
        }

        public static void ParseImages(string HTML,string heroName)
        {
            CQ cq = CQ.Create(HTML);

            var cq2 = cq.Find(Selectors.GetImageSelector());

            var src = cq2[0].Attributes["src"];

            Caching(src, $"./Images/{heroName}.png");

        }

        public static void log(string type, string message)
        {

            type = type.ToUpper();
            switch (type)
            {
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "WARNG":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "SUCCES":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "INFO":
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case "PRGRS":
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine("{1,6} {0} {2}", DateTime.Now.ToString("hh:mm:ss"), type, message);
        }

        /// <summary>
        /// Draw a progress bar at the current cursor position.
        /// Be careful not to Console.WriteLine or anything whilst using this to show progress!
        /// </summary>
        /// <param name="progress">The position of the bar</param>
        /// <param name="total">The amount it counts</param>
        private static void drawTextProgressBar(int progress, int total)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
        }

        class AnimatedBar
        {
            List<string> animation;
            int counter;
            public AnimatedBar()
            {
                this.animation = new List<string> { "/", "-", @"\", "|" };
                this.counter = 0;
            }

            /// <summary>
            /// prints the character found in the animation according to the current index
            /// </summary>
            public void Step(int value)
            {
                Console.Write(this.animation[this.counter] + " " + value + "\r");
                this.counter++;
                if (this.counter == this.animation.Count)
                    this.counter = 0;
            }
        }
    }
}