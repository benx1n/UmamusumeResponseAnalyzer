﻿using MessagePack;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UmamusumeResponseAnalyzer.Localization;

namespace UmamusumeResponseAnalyzer
{
    internal static class Config
    {
        internal static string CONFIG_FILEPATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer", ".config");
        internal static Dictionary<string, string[]> ConfigSet { get; set; } = new();
        internal static Dictionary<string, object> Configuration { get; private set; } = new();
        internal static void Initialize()
        {
            ConfigSet.Add(Resource.ConfigSet_Events, new[]
            {
                Resource.ConfigSet_ParseSingleModeCheckEventResponse,
                Resource.ConfigSet_ParseTrainedCharaLoadResponse,
                Resource.ConfigSet_ParseFriendSearchResponse,
                Resource.ConfigSet_ParseTeamStadiumOpponentListResponse,
                Resource.ConfigSet_ParsePracticeRaceRaceStartResponse,
                Resource.ConfigSet_ParseRoomMatchRaceStartResponse,
                Resource.ConfigSet_MaximiumGradeSkillRecommendation,
                Resource.ConfigSet_ShowCommandInfo
            });
            ConfigSet.Add(Resource.ConfigSet_AutoUpdate, Array.Empty<string>());
            ConfigSet.Add(Resource.ConfigSet_SaveResponseForDebug, Array.Empty<string>());
            ConfigSet.Add(Resource.ConfigSet_ForceUseGithubToUpdate, Array.Empty<string>());
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UmamusumeResponseAnalyzer"));
            if (File.Exists(CONFIG_FILEPATH))
            {
                try
                {
                    var configuration = MessagePackSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllBytes(CONFIG_FILEPATH));
                    Configuration = configuration;
                }
                catch (Exception)
                {
                    File.Delete(CONFIG_FILEPATH);
                    Generate();
                    AnsiConsole.MarkupLine($"[red]读取配置文件时发生错误,已重新生成,请再次更改设置[/]");
                }
                foreach (var i in ConfigSet)
                {
                    if (i.Value == Array.Empty<string>())
                    {
                        if (!Configuration.ContainsKey(i.Key))
                        {
                            if (i.Key == Resource.ConfigSet_ForceUseGithubToUpdate) //但是这个不默认开
                                Configuration.Add(i.Key, false);
                            else
                                Configuration.Add(i.Key, true); //对于新添加的功能 默认开启
                        }
                    }
                    else
                    {
                        foreach (var j in i.Value)
                        {
                            if (!Configuration.ContainsKey(j))
                                Configuration.Add(j, true);
                        }
                    }
                }
            }
            else
            {
                Generate();
            }
        }
        public static void Save() =>
                File.WriteAllBytes(CONFIG_FILEPATH, MessagePackSerializer.Serialize(Configuration));
        private static void Generate()
        {
            foreach (var i in ConfigSet)
            {
                if (i.Value == Array.Empty<string>())
                {
                    if (i.Key == Resource.ConfigSet_ForceUseGithubToUpdate) //不默认开
                        Configuration.Add(i.Key, false);
                    else
                        Configuration.Add(i.Key, true);
                }
                else
                {
                    foreach (var j in i.Value)
                    {
                        Configuration.Add(j, true);
                    }
                }
            }
            Save();
        }
        public static T Get<T>(string key) => (T)Configuration[key];
        public static bool Get(string key) => Get<bool>(key);
        public static void Set(string key, object value)
        {
            Configuration[key] = value;
        }
    }
}
