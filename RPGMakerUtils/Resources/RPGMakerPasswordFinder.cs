using Newtonsoft.Json.Linq;
using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace RPGMakerUtils.Resources
{
    internal class RPGMakerPasswordFinder
    {

        public static string GetMapName(JToken mapToken)
        {
            if (mapToken == null || mapToken.Type != JTokenType.Object)
                return string.Empty;
            JObject mapObj = mapToken.ToObject<JObject>();
            if (!mapObj.ContainsKey("displayName"))
                return string.Empty;
            return mapObj["displayName"].ToString();
        }

        private static void CollectPasswordsFromEventList(JArray list, List<(string, List<string>)> passwords)
        {
            for (int i = 0; i < list.Count; i++)
            {
                JObject command = list[i] as JObject;
                if (command == null || !command.ContainsKey("code"))
                    continue;

                if (command["code"].ToObject<int>() == 103 && i + 1 < list.Count())
                {
                    JObject nextCommand = list[i + 1] as JObject;
                    // Code 111 is "Input Password" (search next 5 commands for code 111)
                    for (int j = 1; j <= 5 && (i + j) < list.Count(); j++)
                    {
                        nextCommand = list[i + j] as JObject;
                        if (nextCommand != null && nextCommand.ContainsKey("code") && nextCommand["code"].ToObject<int>() == 111)
                        {
                            string password = nextCommand["parameters"][3].ToString();
                            passwords.Add((password, GetLastDialogs(list, i)));
                            break;
                        }
                        nextCommand = null;
                    }
                }
            }
        }

        public static List<(string, List<string>)> FindPasswordFromMapToken(JToken mapToken)
        {
            // MapXX.json Structure: .events[] -> .pages[] -> .list[] -> .code
            if (mapToken == null || mapToken.Type != JTokenType.Object)
                return null;

            JObject mapObj = mapToken as JObject;
            if (!mapObj.ContainsKey("events"))
                return null;

            JToken eventsToken = mapObj["events"];

            if (eventsToken == null || eventsToken.Type != JTokenType.Array)
                return null;

            JArray eventArray = eventsToken as JArray;
            List<(string, List<string>)> passwords = new List<(string, List<string>)>();

            foreach (JToken rpgEvent in eventArray)
            {
                if (rpgEvent == null || rpgEvent.Type != JTokenType.Object || !((JObject)rpgEvent).ContainsKey("pages"))
                    continue;

                JArray pages = ((JObject)rpgEvent)["pages"] as JArray;
                foreach (JObject page in pages)
                {
                    if (page == null || !page.ContainsKey("list"))
                        continue;
                    JArray list = page["list"] as JArray;
                    CollectPasswordsFromEventList(list, passwords);
                }
            }

            return passwords;
        }

        public static List<(string, List<string>)> FindPasswordFromCommonEventToken(JToken commonEventToken)
        {
            // CommonEvents.json Structure: .commonEvents[] -> .list[] -> .code
            if (commonEventToken == null || commonEventToken.Type != JTokenType.Array)
                return null;

            JArray eventArray = commonEventToken as JArray;
            List<(string, List<string>)> passwords = new List<(string, List<string>)>();

            foreach (JToken eventToken in eventArray)
            {
                if (eventToken.Type != JTokenType.Object)
                    continue;
                JObject eventObject = eventToken as JObject;
                if (!eventObject.ContainsKey("list"))
                    continue;
                JArray list = eventObject["list"] as JArray;
                CollectPasswordsFromEventList(list, passwords);
            }

            return passwords;
        }

        public static List<string> GetLastDialogs(JArray listToken, int currentIndex, int count = 3)
        {
            if (listToken == null)
                return null;

            List<string> lastDialogs = new List<string>();

            for (int i = currentIndex - 1; i >= 0 && lastDialogs.Count < count; i--)
            {
                JObject command = listToken[i].ToObject<JObject>();
                if (command == null || !command.ContainsKey("code"))
                    continue;
                if (command["code"].ToObject<int>() == 401) // code 401 is the dialog
                {
                    string dialog = command["parameters"][0].ToString();
                    lastDialogs.Add(dialog);
                    if (lastDialogs.Count == count)
                    {
                        lastDialogs.Reverse();
                        return lastDialogs;
                    }
                }
            }

            if (lastDialogs.Count > 0)
            {
                lastDialogs.Reverse();
                return lastDialogs;
            }
            else
            {
                return null;
            }
        }

        public static List<PasswordDialog> GetPasswordsFromGameData(IEnumerable<GameDataFile> gameDataFiles)
        {
            List<PasswordDialog> passwords = new List<PasswordDialog>();

            foreach (var gameDataFile in gameDataFiles)
            {
                // MapXX.json
                if (gameDataFile.FileName.StartsWith("Map") && gameDataFile.FileName.EndsWith(".json"))
                {
                    string json = File.ReadAllText(gameDataFile.FilePath);
                    JToken token = JToken.Parse(json);
                    var mapEvents = FindPasswordFromMapToken(token);
                    if (mapEvents != null)
                    {
                        foreach (var mapEvent in mapEvents)
                        {
                            if (string.IsNullOrWhiteSpace(mapEvent.Item1))
                                continue;

                            passwords.Add(new PasswordDialog
                            {
                                Password = mapEvent.Item1,
                                LastDialog = $"【{gameDataFile.FileName}】 {GetMapName(token)}\n" + (mapEvent.Item2 != null ? string.Join("\n", mapEvent.Item2) : "无前置对话")
                            });
                        }
                    }
                }
                else if (gameDataFile.FileName == "CommonEvents.json")
                {
                    string json = File.ReadAllText(gameDataFile.FilePath);
                    JToken token = JToken.Parse(json);
                    var mapEvents = FindPasswordFromCommonEventToken(token);
                    if (mapEvents != null)
                    {
                        foreach (var mapEvent in mapEvents)
                        {
                            if (string.IsNullOrWhiteSpace(mapEvent.Item1))
                                continue;

                            passwords.Add(new PasswordDialog
                            {
                                Password = mapEvent.Item1,
                                LastDialog = $"【{gameDataFile.FileName}】 {GetMapName(token)}\n" + (mapEvent.Item2 != null ? string.Join("\n", mapEvent.Item2) : "无前置对话")
                            });
                        }
                    }
                }
            }

            return passwords;
        }
    }
}
