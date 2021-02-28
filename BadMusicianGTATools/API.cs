using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BadMusicianGTATools
{
    internal class API
    {
        internal static readonly HttpClient HttpClient = new HttpClient();
        internal static bool Busy { get; private set; } = false;

        internal static Task Send(string method, Action<string> onDone = null, params (string name, string val)[] args)
        {
            Busy = true;
            method = method.Trim('/');
            List<string> url_args = new List<string> { };
            for (int i = 0; i < args.Length; i++) url_args.Add(args[i].name + "=" + args[i].val);

            var t = HttpClient.GetAsync($"https://badmusician.ru/gta/api/{method}/?" + string.Join("&", url_args)).ContinueWith(x =>
            {
                x.Result.Content.ReadAsStringAsync().ContinueWith(s =>
                {
                    if (onDone != null) onDone(s.Result);
                    Busy = false;
                });
            });
            return t;
        }
        internal static void SendAndWait(string method, Action<string> onDone = null, params (string name, string val)[] args)
        {
            var t = Send(method, onDone, args);
            while (Busy)
            {
                Rage.GameFiber.Yield();
            }
        }
    }
}
