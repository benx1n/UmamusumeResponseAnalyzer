﻿using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.Net;
using System.Runtime.InteropServices;
using UmamusumeResponseAnalyzer.Localization;
using UmamusumeResponseAnalyzer.Handler;

namespace UmamusumeResponseAnalyzer
{
    internal static class Server
    {
        static readonly HttpListener httpListener = new();
        static readonly object _lock = new();
        public static void Start()
        {
            httpListener.Prefixes.Add("http://127.0.0.1:4693/");
            httpListener.Start();
            Task.Run(async () =>
            {
                while (httpListener.IsListening)
                {
                    var ctx = await httpListener.GetContextAsync();

                    var ms = new MemoryStream();
                    ctx.Request.InputStream.CopyTo(ms);
                    var buffer = ms.ToArray();

                    if (ctx.Request.RawUrl == "/notify/response")
                    {
#if DEBUG
                        Directory.CreateDirectory("packets");
                        File.WriteAllBytes($@"./packets/{DateTime.Now:yy-MM-dd HH-mm-ss}R.bin", buffer);
                        File.WriteAllText($@"./packets/{DateTime.Now:yy-MM-dd HH-mm-ss}R.json", JObject.Parse(MessagePackSerializer.ConvertToJson(buffer)).ToString());
#endif
                        if (Config.Get(Resource.ConfigSet_SaveResponseForDebug))
                        {
                            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", "packets");
                            if (Directory.Exists(directory))
                            {
                                foreach (var i in Directory.GetFiles(directory))
                                {
                                    var fileInfo = new FileInfo(i);
                                    if (fileInfo.CreationTime.AddDays(1) < DateTime.Now)
                                        fileInfo.Delete();
                                }
                            }
                            else
                            {
                                Directory.CreateDirectory(directory);
                            }
                            File.WriteAllBytes($"{directory}/{DateTime.Now:yy-MM-dd HH-mm-ss}R.msgpack", buffer);
                        }
                        _ = Task.Run(() => ParseResponse(buffer));
                    }
                    else if (ctx.Request.RawUrl == "/notify/request")
                    {
#if DEBUG
                        Directory.CreateDirectory("packets");
                        File.WriteAllText($@"./packets/{DateTime.Now:yy-MM-dd HH-mm-ss}Q.json", JObject.Parse(MessagePackSerializer.ConvertToJson(buffer.AsMemory()[170..])).ToString());
#endif
                        _ = Task.Run(() => ParseRequest(buffer[170..]));
                    }

                    await ctx.Response.OutputStream.WriteAsync(Array.Empty<byte>());
                    ctx.Response.Close();
                }
            });
        }
        static void ParseRequest(byte[] buffer)
        {
            try
            {
                lock (_lock)
                {
                    var str = MessagePackSerializer.ConvertToJson(buffer);
                    switch (str)
                    {
                        default:
                            return;
                    }
                }
            }
            catch (Exception e)
            {
                AnsiConsole.WriteException(e);
            }
        }
        static void ParseResponse(byte[] buffer)
        {
            lock (_lock)
            {
                try
                {
                    var dyn = JsonConvert.DeserializeObject<dynamic>(MessagePackSerializer.ConvertToJson(buffer));
                    if (dyn == default(dynamic)) return;
                    var data = dyn.data;
                    if (data.chara_info != null && data.home_info?.command_info_array != null && data.race_reward_info == null) //根据文本简单过滤防止重复、异常输出
                    {
                        if (Config.Get(Resource.ConfigSet_ShowCommandInfo))
                            Handlers.ParseCommandInfo(dyn.ToObject<Gallop.SingleModeCheckEventResponse>());
                    }
                    if (data.chara_info != null && data.unchecked_event_array?.Count > 0)
                    {
                        if (Config.Get(Resource.ConfigSet_ParseSingleModeCheckEventResponse))
                            Handlers.ParseSingleModeCheckEventResponse(dyn.ToObject<Gallop.SingleModeCheckEventResponse>());
                    }
                    if (data.chara_info != null && (data.chara_info.state == 2 || data.chara_info.state == 3) && data.unchecked_event_array?.Count == 0)
                    {
                        if (Config.Get(Resource.ConfigSet_MaximiumGradeSkillRecommendation) && data.chara_info.skill_tips_array != null)
                            Handlers.ParseSkillTipsResponse(dyn.ToObject<Gallop.SingleModeCheckEventResponse>());
                    }
                    if (data.trained_chara_array != null && data.trained_chara_favorite_array != null && data.room_match_entry_chara_id_array != null)
                    {
                        if (Config.Get(Resource.ConfigSet_ParseTrainedCharaLoadResponse))
                            Handlers.ParseTrainedCharaLoadResponse(dyn.ToObject<Gallop.TrainedCharaLoadResponse>());
                    }
                    if (data.user_info_summary != null && data.practice_partner_info != null && data.support_card_data != null && data.follower_num != null && data.own_follow_num != null)
                    {
                        if (Config.Get(Resource.ConfigSet_ParseFriendSearchResponse))
                            Handlers.ParseFriendSearchResponse(dyn.ToObject<Gallop.FriendSearchResponse>());
                    }
                    if (data.opponent_info_array?.Count == 3)
                    {
                        if (Config.Get(Resource.ConfigSet_ParseTeamStadiumOpponentListResponse))
                            Handlers.ParseTeamStadiumOpponentListResponse(dyn.ToObject<Gallop.TeamStadiumOpponentListResponse>()); //https://github.com/CNA-Bld/EXNOA-CarrotJuicer/issues/2
                    }
                    if (data.trained_chara_array != null && data.race_result_info != null && data.entry_info_array != null && data.practice_race_id != null && data.state != null && data.practice_partner_owner_info_array != null)
                    {
                        if (Config.Get(Resource.ConfigSet_ParsePracticeRaceRaceStartResponse))
                            Handlers.ParsePracticeRaceRaceStartResponse(buffer);
                    }
                    if (data.race_scenario != null && data.random_seed != null && data.race_horse_data_array != null && data.trained_chara_array != null && data.season != null && data.weather != null && data.ground_condition != null)
                    {
                        if (Config.Get(Resource.ConfigSet_ParseRoomMatchRaceStartResponse))
                            Handlers.ParseRoomMatchRaceStartResponse(dyn.ToObject<Gallop.RoomMatchRaceStartResponse>());
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
