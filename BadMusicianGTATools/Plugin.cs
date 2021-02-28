using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Rage;
using N = Rage.Native.NativeFunction;
using Graphics = Rage.Graphics;

[assembly: Rage.Attributes.Plugin("BadMusician's GTA V Tools",
    Description = "Integration with https://badmusician.ru/gta/",
    Author = "BadMusician",
    EntryPoint = "BadMusicianGTATools.Plugin.OnLoad",
    ExitPoint = "BadMusicianGTATools.Plugin.OnUnload",
    PrefersSingleInstance = true,
    SupportUrl = "https://badmusician.ru/gta/",
    ShouldTickInPauseMenu = false)]

namespace BadMusicianGTATools
{
    public static class Plugin
    {
        internal static string Token { get; set; } = "";
        internal static bool Connected { get; set; } = false;

        internal static InitializationFile INI = new InitializationFile("Plugins//BadMusicianGTATools.ini");

        const string fontName = "Arial";
        const float fontSize = 18;
        const int updateTimeout = 500;

        static SizeF padding = new SizeF(10, 5);
        static SizeF s_1, s_2;
        static PointF root = new PointF(30, Game.Resolution.Height / 2);
        static int soundID = -1;
        static string lastAPI = "";
        static string taskType = "";
        static AnimationTask anim = null;
        static bool taskStarted = false;
        static DateTime taskStart = DateTime.Now;
        static DateTime animStart = DateTime.Now;
        static Action action = null;

        public static void OnLoad()
        {
            Token = SettingsGet("GENERAL", "Token", "");
            if (Token != "") Connect(Token);
            Game.RawFrameRender += Game_RawFrameRender;
            s_1 = Graphics.MeasureText("BadMusician", fontName, fontSize);
            s_2 = Graphics.MeasureText("'s GTA V Dev Tools: ", fontName, fontSize);
            Game.AddConsoleCommands();
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    while (API.Busy) GameFiber.Yield();
                    GameFiber.Sleep(updateTimeout);
                    if (!Connected) return;
                    API.Send("process", x =>
                    {
                        lastAPI = x;
                        if (x.Contains(":"))
                        {
                            string[] request = x.Split('|')[2].Split(':');
                            string[] content = request[1].Split(',');
                            taskType = request[0];
                            if (taskType == "audio")
                            {
                                var run = API.Send("run", y =>
                                {
                                    taskStarted = true;
                                    taskStart = DateTime.Now;
                                    action = delegate { PlaySound(content[0], content[1]); };
                                }, ("token", Token));
                                while (!run.IsCompleted)
                                {
                                    GameFiber.Sleep(1);
                                }
                            }
                            else
                            if (taskType == "anim")
                            {
                                var run = API.Send("run", y =>
                                {
                                    taskStarted = true;
                                    taskStart = DateTime.Now;
                                    action = delegate { PlayAnim(content[0], content[1], content[2].Split('+')); };
                                }, ("token", Token));
                                while (!run.IsCompleted)
                                {
                                    GameFiber.Sleep(1);
                                }
                            }
                            if (taskType == "speech")
                            {
                                var run = API.Send("run", y =>
                                {
                                    taskStarted = true;
                                    taskStart = DateTime.Now;
                                    action = delegate { PlaySpeech(content[0], content[1], content[2]); };
                                }, ("token", Token));
                                while (!run.IsCompleted)
                                {
                                    GameFiber.Sleep(1);
                                }
                            }
                            else
                            {

                            }
                        }
                        else
                        {
                            if (x.Split('|')[2] == "clear")
                            {
                                var run = API.Send("run", y =>
                                {
                                    taskType = "clear";
                                    taskStarted = true;
                                    taskStart = DateTime.Now;
                                    action = delegate { Clear(); };
                                }, ("token", Token));
                                while (!run.IsCompleted)
                                {
                                    GameFiber.Sleep(1);
                                }
                            }
                        }
                    }, ("token", Token));
                    if (action != null)
                    {
                        Game.LogTrivial("Running");
                        action();
                        action = null;
                    }
                    if (taskStarted)
                    {
                        if (taskType == "audio" && soundID != -1 && N.Natives.xFCBDCE714A7C88E5<bool>(soundID))
                        {
                            Finish();
                        }
                        else
                        if (taskType == "anim" && anim != null)
                        {
                            if ((DateTime.Now - animStart).TotalMilliseconds > 10000 || (!anim.IsPlaying && anim.Status != Rage.TaskStatus.InProgress && anim.Status != Rage.TaskStatus.Preparing && anim.Status != Rage.TaskStatus.Interrupted))
                            {
                                anim = null;
                                Finish();
                            }
                        }
                        else
                        if (taskType == "speech" && (DateTime.Now - animStart).TotalMilliseconds > 1000 && !Game.LocalPlayer.Character.IsAnySpeechPlaying)
                        {
                            Finish();
                        }
                        else
                        if (taskType == "clear")
                        {
                            Finish();
                        }
                    }
                }
            });
            GameFiber.Hibernate();
        }

        private static void Game_RawFrameRender(object sender, GraphicsEventArgs e)
        {
            string c = Connected ? "Connected" : "Not Connected";
            SizeF s_4 = Graphics.MeasureText(c, fontName, fontSize);
            if (API.Busy)
            {
                e.Graphics.DrawRectangle(new RectangleF(new PointF(root.X - 6, root.Y), new SizeF(6, s_1.Height + padding.Height * 4)), Color.Yellow);
            }
            e.Graphics.DrawRectangle(new RectangleF(root, new SizeF(s_1.Width + s_2.Width + s_4.Width + padding.Width * 2, s_1.Height + padding.Height * 4)), Color.FromArgb(150, Color.Black));
            e.Graphics.DrawText("BadMusician", fontName, fontSize, new PointF(root.X + padding.Width, root.Y + padding.Height), Color.Gold);
            e.Graphics.DrawText("'s GTA V Dev Tools: ", fontName, fontSize, new PointF(root.X + padding.Width + s_1.Width, root.Y + padding.Height), Color.White);
            e.Graphics.DrawText(c, fontName, fontSize, new PointF(root.X + padding.Width + s_1.Width + s_2.Width, root.Y + padding.Height), Connected ? Color.LightGreen : Color.Red);
        }

        public static void OnUnload(bool isTerminating)
        {
            API.HttpClient.Dispose();
            if (soundID != -1)
                N.Natives.x353FC880830B88FA(soundID);
        }

        internal static void Connect(string token)
        {
            Game.LogTrivial($"Connecting");
            Connected = false;
            Token = "";
            if (token.Length != 256)
            {
                Rage.Game.LogTrivial($"Error: Token must be 256 characters long");
                return;
            }
            API.SendAndWait("connect", x =>
            {
                if (x == "ok")
                {
                    Token = token;
                    Connected = true;
                    SettingsSet("GENERAL", "Token", Token);
                    Rage.Game.LogTrivial($"Connected");
                }
                else
                {
                    Rage.Game.LogTrivial($"Error: {x}");
                    return;
                }
            }, ("token", token));
        }
        internal static void Disconnect()
        {
            Game.LogTrivial($"Disconnecting");
            API.SendAndWait("disconnect", x =>
            {
                if (x == "ok")
                {
                    Rage.Game.LogTrivial($"Disconnected");
                }
                else
                {
                    Rage.Game.LogTrivial($"Error: {x}");
                    return;
                }
            }, ("token", Token));
            Connected = false;
            Token = "";
        }

        internal static string SettingsGet(string section, string key, string defaultValue)
        {
            if (!INI.Exists()) INI.Create();
            if (!INI.DoesKeyExist(section, key))
            {
                INI.Write(section, key, defaultValue);
                return defaultValue;
            }
            return INI.ReadString(section, key, defaultValue);
        }
        internal static void SettingsSet(string section, string key, string value)
        {
            if (!INI.Exists()) INI.Create();
            INI.Write(section, key, value);
        }


        internal static void PlaySound(string dict, string name)
        {
            StopSound();
            if (soundID == -1) soundID = N.Natives.x430386FE9BF80B45<int>();
            N.Natives.x67C540AA08E4A6F5(soundID, name, dict, false);
        }
        internal static void StopSound()
        {
            if (soundID == -1 || N.Natives.xFCBDCE714A7C88E5<bool>(soundID)) return;
            N.Natives.xA3B0C41BA5CC0BB5(soundID);
        }

        internal static void PlayAnim(string dict, string name, string[] flags)
        {
            Game.LocalPlayer.Character.Tasks.ClearSecondary();
            Game.LocalPlayer.Character.Tasks.ClearImmediately();
            if (flags.Length < 2)
            {
                anim = Game.LocalPlayer.Character.Tasks.PlayAnimation(dict, name, -1, 3, 3, 0, AnimationFlags.None);
            }
            else
            {
                AnimationFlags fAll = AnimationFlags.None;
                for (int i = 0; i < flags.Length; i++)
                {
                    Game.LogTrivial(flags[i]);
                    if (Enum.TryParse(flags[i], out AnimationFlags f))
                    {
                        fAll |= f;
                    }
                }
                anim = Game.LocalPlayer.Character.Tasks.PlayAnimation(dict, name, -1, 3, 3, 0, fAll);
            }
            animStart = DateTime.Now;
        }

        internal static void PlaySpeech(string voice, string name, string index)
        {
            N.Natives.x7A73D05A607734C7(Game.LocalPlayer.Character); //STOP_CURRENT_PLAYING_SPEECH
            N.Natives.xB8BEC0CA6F0EDB0F(Game.LocalPlayer.Character); //STOP_CURRENT_PLAYING_AMBIENT_SPEECH
            Game.LocalPlayer.Character.PlayAmbientSpeech(voice, name, int.Parse(index), SpeechModifier.ForceFrontend);
        }

        internal static void Clear()
        {
            StopSound();
            if (Game.LocalPlayer.Character)
            {
                Game.LocalPlayer.Character.Tasks.Clear();
                Game.LocalPlayer.Character.Tasks.ClearSecondary();
                N.Natives.x7A73D05A607734C7(Game.LocalPlayer.Character); //STOP_CURRENT_PLAYING_SPEECH
                N.Natives.xB8BEC0CA6F0EDB0F(Game.LocalPlayer.Character); //STOP_CURRENT_PLAYING_AMBIENT_SPEECH
            }
        }
        internal static void Finish()
        {
            Game.LogTrivial("Finishing");
            taskType = "";
            taskStarted = false;
            var finish = API.Send("finish", y =>
            {
            }, ("token", Token));
            while (!finish.IsCompleted)
            {
                GameFiber.Yield();
            }
        }
    }
}
