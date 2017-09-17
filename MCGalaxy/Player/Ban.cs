/*
 Copyright 2011 MCForge
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.IO;
using System.Text;

namespace MCGalaxy {
    
    /// <summary> Can check the info about someone's ban, find out if there's info about someone,
    /// and add / remove someone to the baninfo (NOT THE BANNED.TXT !) </summary>
    public static class Ban {
        
        static PlayerMetaList bans = new PlayerMetaList("text/bans.txt");
        static PlayerMetaList unbans = new PlayerMetaList("text/unbans.txt");
        
        public static void EnsureExists() {
            bans.EnsureExists();
            unbans.EnsureExists();
        }
        
        public static string PackTempBanData(string reason, string banner, DateTime expiry) {
            if (reason == null) reason = "-";
            return banner + " " + expiry.ToUnixTime() + " " + reason;
        }
        
        public static void UnpackTempBanData(string line, out string reason, out string banner, out DateTime expiry) {
            string[] parts = line.SplitSpaces(3);
            banner = parts[0];
            
            // Timestamp used to be raw DateTime ticks, is now UTC timestamp
            long timestamp = long.Parse(parts[1]);
            try {
                expiry = timestamp.FromUnixTime();
            } catch (ArgumentOutOfRangeException) {
                expiry = new DateTime(long.Parse(parts[1]), DateTimeKind.Utc);
            }
            reason = parts.Length > 2 ? parts[2] : "";
        }
        
        
        /// <summary> Adds a ban entry for the given user, and who banned them and why they were banned. </summary>
        public static void BanPlayer(Player banner, string target, string reason, bool stealth, string oldrank) {
            if (reason.Length == 0) reason = ServerConfig.DefaultBanMessage;
            reason = reason.Replace(" ", "%20");
            
            string player = banner == null ? "(console)" : banner.truename;
            AddBanEntry(player, target.ToLower(), reason, stealth.ToString(), oldrank);
        }
        
        /// <summary> Adds a ban entry for the given user, and who banned them and why they were banned. </summary>
        public static void UnbanPlayer(Player unbanner, string target, string reason) {
            if (reason.Length == 0) reason = "(none given)";
            reason = reason.Replace(" ", "%20");
            
            string player = unbanner == null ? "(console)" : unbanner.truename;
            AddUnbanEntry(player, target.ToLower(), reason);
        }
        
        static void AddBanEntry(string pl, string who, string reason, string stealth, string oldrank) {
            string datetime = DateTime.UtcNow.ToUnixTime().ToString();
            string data = pl + " " + who + " " + reason + " " + stealth + " " + datetime + " " + oldrank;
            bans.Append(data);
        }
        
        static void AddUnbanEntry(string pl, string who, string reason) {
            string datetime = DateTime.UtcNow.ToUnixTime().ToString();
            string data = pl + " " + who + " " + reason + " " + datetime;
            unbans.Append(data);
        }
        
        
        /// <summary> Returns info about the current or last ban of a user. </summary>
        public static void GetBanData(string who, out string banner, out string reason, 
                                      out DateTime time, out string prevRank) {
            who = who.ToLower();
            foreach (string line in File.ReadAllLines(bans.file)) {
                string[] parts = line.SplitSpaces();
                if (parts.Length <= 5 || parts[1] != who) continue;
                
                banner   = parts[0];
                reason   = parts[2].Replace("%20", " ");
                time     = GetDate(parts[4]);
                prevRank = parts[5];
                return;
            }
            banner = null; reason = null; time = DateTime.MinValue; prevRank = null;
        }
        
        /// <summary> Returns information about the last unban of a user. </summary>
        public static void GetUnbanData(string who, out string unbanner, out string reason, 
                                        out DateTime time) {
            who = who.ToLower();
            unbanner = null; reason = null;
            foreach (string line in File.ReadAllLines(unbans.file)) {
                string[] parts = line.SplitSpaces();
                if (parts.Length <= 3 || parts[1] != who) continue;
                
                unbanner = parts[0];
                reason   = parts[2].Replace("%20", " ");
                time     = GetDate(parts[3]);
                return;
            }
            unbanner = null; reason = null; time = DateTime.MinValue;
        }
        
        static DateTime GetDate(string raw) {
            raw = raw.Replace("%20", " ").Replace(",", "");
            long timestap;
            if (long.TryParse(raw, out timestap)) return timestap.FromUnixTime();
            
            /* Old form of timestamps in bans/unbans:
               DateTime now = DateTime.Now;
               return now.DayOfWeek + "%20" + now.Day + "%20" + now.Month + "%20" + now.Year + ",%20at%20" + now.Hour + ":" + now.Minute;
             */
            string[] date = raw.SplitSpaces();
            string[] minuteHour = date[5].Split(':');
            
            int hour = int.Parse(minuteHour[0]), minute = int.Parse(minuteHour[1]);
            int day = int.Parse(date[1]), month = int.Parse(date[2]), year = int.Parse(date[3]);
            return new DateTime(year, month, day, hour, minute, 0).ToUniversalTime();
        }
        
        
        /// <summary> Deletes the ban information about the user. </summary>
        public static bool DeleteBan(string name) { return DeleteInfo(name, bans); }
        
        /// <summary> Deletes the unban information about the user. </summary>
        public static bool DeleteUnban(string name) { return DeleteInfo(name, unbans); }
        
        static bool DeleteInfo(string name, PlayerMetaList list) {
            name = name.ToLower();
            bool success = false;
            StringBuilder sb = new StringBuilder();
            
            foreach (string line in File.ReadAllLines(list.file)) {
                string[] parts = line.SplitSpaces();
                if (parts.Length <= 1 || parts[1] != name)
                    sb.AppendLine(line);
                else
                    success = true;
            }
            File.WriteAllText(list.file, sb.ToString());
            return success;
        }
        
        
        /// <summary> Change the ban reason for the given user. </summary>
        public static bool ChangeBanReason(string who, string reason) {
            return ChangeReason(who, reason, bans);
        }
        
        /// <summary> Change the unban reason for the given user. </summary>
        public static bool ChangeUnbanReason(string who, string reason) {
            return ChangeReason(who, reason, unbans);
        }
        
        static bool ChangeReason(string who, string reason, PlayerMetaList list) {
            who = who.ToLower();
            reason = reason.Replace(" ", "%20");
            bool success = false;
            StringBuilder sb = new StringBuilder();
            
            foreach (string line in File.ReadAllLines(list.file)) {
                string[] parts = line.SplitSpaces();
                if (parts.Length > 2 && parts[1] == who) {
                    success = true;
                    sb.AppendLine(String.Join(" ", parts));
                } else {
                    sb.AppendLine(line);
                }
            }
            
            if (success)
                File.WriteAllText(list.file, sb.ToString());
            return success;
        }
    }
}