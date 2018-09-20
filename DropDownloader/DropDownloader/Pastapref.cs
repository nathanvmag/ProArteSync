using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DropDownloader
{
    public class Pastapref
    {
        public string path;
        public bool check;
        public Pastapref(string path, bool check)
        {
            this.path = path;
            this.check = check;
        }
        public override string ToString()
        {
            return "Meu path " + path + " meu check " + check+" "+path.Split('\\').Length;
        }
        public static void addifNew(List<Pastapref> prefs, string pth)
        {
            bool test = false;
            foreach (Pastapref pf in prefs)
            {
                if(pf!=null)
                if (pf.path == pth)
                {
                    test = true;
                    break;
                }

            }
            if (!test)
            {
                prefs.Add(new Pastapref(pth, true));
            }
        }
        public static Pastapref getboolvalue(List<Pastapref> prefs, string pth)
        {
            Pastapref ppref;
            foreach (Pastapref pf in prefs)
            {
                if (pf != null)
                    if (pf.path == pth)
                {
                    ppref = pf;
                    return ppref;
                }
            }
            return null;
        }
        public static void checkifexist(List<Pastapref> pref, string pth, bool check)
        {
            foreach (Pastapref pf in pref)
            {
                if (pf != null)
                    if (pf.path == pth)
                {
                    pf.check = check;
                    break;
                }
            }
        }
        public static bool candonwload(List<Pastapref> pref, string pth)
        {
            foreach (Pastapref pf in pref)
            {
                if (pf != null)

                    if (!string.IsNullOrEmpty(pf.path) && pth.Contains(pf.path.Replace('\\', '/')))

                {
                    return pf.check;
                }
            }
            return false;
            
        }
    }
}
