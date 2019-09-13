using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Discord.Commands;
using ThunderED.Helpers;
using System.Collections.Concurrent;
using System.Collections.Generic;

using ThunderED.Classes;
using System.Text;

namespace ThunderED.Modules.Static
{
    internal class MoonsModule : AppModuleBase
    {
        public override LogCat Category => LogCat.About;

        private readonly static ConcurrentDictionary<long, string> _tags = new ConcurrentDictionary<long, string>();

        internal static async Task Moons(ICommandContext context)
        {

            var channel = context.Channel;

            var botid = APIHelper.DiscordAPI.GetCurrentUser().Id;
            var memoryUsed = ByteSize.FromBytes(Process.GetCurrentProcess().WorkingSet64);
            var runTime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var totalUsers = APIHelper.DiscordAPI.GetUsersCount();






            Dictionary<string, string> moons = new Dictionary<string, string>();
            List<string> output = new List<string>();
            try
            {


                var guildID = SettingsManager.Settings.Config.DiscordGuildId;

                foreach (var groupPair in SettingsManager.Settings.NotificationFeedModule.Groups)
                {
                    var group = groupPair.Value;
                    if (!group.CharacterID.Any() || group.CharacterID.All(a => a == 0))
                    {
                        await LogHelper.LogError($"[CONFIG] Notification group {groupPair.Key} has no characterID specified!");
                        continue;
                    }

                    if (group.DefaultDiscordChannelID == 0)
                    {
                        await LogHelper.LogError($"[CONFIG] Notification group {groupPair.Key} has no DefaultDiscordChannelID specified!");
                        continue;
                    }

                    //skip empty group
                    if (group.Filters.Values.All(a => a.Notifications.Count == 0)) continue;


                    foreach (var charId in group.CharacterID)
                    {
                        var rToken = await SQLHelper.GetRefreshTokenDefault(charId);
                        if (string.IsNullOrEmpty(rToken))
                        {
                            await LogHelper.LogWarning($"Failed to get notifications refresh token for character {charId}! User is not authenticated.");
                            continue;
                        }

                        var tq = await APIHelper.ESIAPI.RefreshToken(rToken, SettingsManager.Settings.WebServerModule.CcpAppClientId, SettingsManager.Settings.WebServerModule.CcpAppSecret);
                        var token = tq.Result;
                        if (tq.Data.IsNoConnection) return;
                        if (string.IsNullOrEmpty(token))
                        {
                            if (tq.Data.IsNotValid)
                                await LogHelper.LogWarning($"Notifications token for character {charId} is outdated or no more valid!");
                            else
                                await LogHelper.LogWarning($"Unable to get notifications token for character {charId}. Current check cycle will be skipped. {tq.Data.ErrorCode}({tq.Data.Message})");
                            continue;
                        }
                        //await LogHelper.LogInfo($"Checking characterID:{charId}", Category, LogToConsole, false);
                        Console.WriteLine($"Checking characterID:{charId}");


                        var etag = _tags.GetOrNull(charId);

                        var result = await APIHelper.ESIAPI.GetCalendar("", charId, token, etag);
                        var results = result.Result;

                        foreach (var r in results)
                        {
                            if (r.title.StartsWith("Moon extraction for "))
                            {
                                string key = r.event_date + "..." + r.title;
                                

                                if (!moons.ContainsKey(key))
                                {
                                    //sb.AppendLine(value);
                                    

                                    string dteve = r.event_date.Replace("T", " ").Replace("Z", " ");
                                    DateTime dt = DateTime.Parse(dteve);

                                    string value = dt.ToString() + " --- " + r.title + "...";
                                    moons.Add(key, value);
                                    output.Add(value);
                                }
                            }
                        }
                    }
                }
                output.Sort();

                StringBuilder sb = new StringBuilder();
                foreach (string o in output)
                    sb.AppendLine(o);


                await APIHelper.DiscordAPI.SendMessageAsync(channel, $"{context.User.Mention},{Environment.NewLine}{Environment.NewLine}" +
                               sb.ToString()).ConfigureAwait(false);


            }
            catch (Exception ex)
            {
                //await LogHelper.LogEx(ex.Message, ex, Category);

                await APIHelper.DiscordAPI.SendMessageAsync(channel, $"{context.User.Mention},{Environment.NewLine}{Environment.NewLine}" +
               "Errors reading the moons").ConfigureAwait(false);

            }
            finally
            {
                //_passNotifications.Clear();
            }





            await Task.CompletedTask;
        }
    }
}
